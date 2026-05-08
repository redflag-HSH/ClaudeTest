using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum BodyPart { Head, LeftArm, RightArm, LeftLeg, RightLeg, Torso }

[System.Serializable]
public struct ComboHit
{
    public float damage;
    public float range;
    public float knockback;
    public float startup;    // seconds before weak hit fires
    public float chainGap;   // seconds between weak and strong hit
    public float recovery;   // seconds after strong hit (next combo window opens)
}

[System.Serializable]
public struct BodyPartDebuff
{
    [Range(0f, 1f)] public float speedMultiplier;       // movement speed scale
    [Range(0f, 1f)] public float jumpMultiplier;        // jump force scale (0 = unable to jump)
    [Range(0f, 1f)] public float attackDamageMultiplier;// light/heavy damage scale
    [Range(0f, 1f)] public float staminaCostMultiplier; // stamina cost scale (higher = costs more)
    [Range(0f, 1f)] public float hpMaxMultiplier;       // max HP scale
    [Range(0f, 3f)] public float hpDrainMultiplier;     // HP drain rate scale
    [Range(1f, 5f)] public float attackDelayMultiplier; // attack animation time scale
    [Range(1f, 5f)] public float dodgeDelayMultiplier;  // dodge duration scale
    [Range(1f, 5f)] public float stunMultiplier;        // stun duration scale when this part is slayed
}

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControl : MonoBehaviour, IDamageable
{
    public static PlayerControl Instance { get; private set; }

    // ���� Movement ����������������������������������������������������������������������������������������������������������������������������

    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 10f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    // ���� Health ��������������������������������������������������������������������������������������������������������������������������������

    [Header("Health")]
    public float maxHp = 100f;
    public float CurrentHp { get; private set; }
    public bool IsDead { get; set; }

    // ���� Stamina ������������������������������������������������������������������������������������������������������������������������������

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegen = 20f;
    public float staminaRegenDelay = 1.2f;
    public float CurrentStamina { get; private set; }

    // ���� Blood ��������������������������������������������������������������������������������������������������������������������������������

    [Header("BloodGage")]
    public float maxBloodGage = 100f;
    public float CurrentBloodGage { get; private set; }

    [Header("BloodMoney")]
    public int CurrentBloodMoney { get; private set; }

    // ── Body Part Debuffs ─────────────────────────────────────────────────────

    [Header("Body Part Debuffs")]
    [Tooltip("One entry per BodyPart enum value (Head, LeftArm, RightArm, LeftLeg, RightLeg, Torso).")]
    public BodyPartDebuff[] bodyPartDebuffs = new BodyPartDebuff[6];

    [Header("Head — Sight / Sound")]
    public float headFadeDuration = 0.5f;

    // tracks which parts have been slayed this run
    readonly bool[] _slayedParts = new bool[6];

    // priority order used when blood gauge heals a part
    static readonly BodyPart[] HealPriority =
    {
        BodyPart.Torso,
        BodyPart.Head,
        BodyPart.LeftArm,
        BodyPart.RightArm,
        BodyPart.LeftLeg,
        BodyPart.RightLeg,
    };

    // ── Leg Debuff Computed ───────────────────────────────────────────────────
    int SlayedLegCount =>
        (_slayedParts[(int)BodyPart.LeftLeg] ? 1 : 0) +
        (_slayedParts[(int)BodyPart.RightLeg] ? 1 : 0);

    float EffectiveSpeed => speed * (_isRunning ? runSpeedMultiplier : 1f) * (SlayedLegCount == 2 ? 0.05f : SlayedLegCount == 1 ? 0.50f : 1f);
    float EffectiveJumpForce => jumpForce * (SlayedLegCount == 2 ? 0f : SlayedLegCount == 1 ? 0.50f : 1f);
    bool CanJump => SlayedLegCount < 2;
    bool AllLimbsCut => SlayedLegCount == 2 && !CanWeakAttack && !CanStrongAttack;
    float AttackDelayMultiplier => SlayedLegCount == 2 ? 2.2f : SlayedLegCount == 1 ? 1.5f : 1f;
    float DodgeDelayMultiplier => SlayedLegCount == 2 ? 2.0f : 1f;
    float EffectiveDodgeForce => dodgeForce * (SlayedLegCount == 2 ? 0.5f : 1f);

    // ── Arm Debuff Computed ───────────────────────────────────────────────────
    bool CanWeakAttack => !_slayedParts[(int)BodyPart.LeftArm];
    bool CanStrongAttack => !_slayedParts[(int)BodyPart.RightArm];

    float ArmStunMultiplier
    {
        get
        {
            float m = 1f;
            if (_slayedParts[(int)BodyPart.LeftArm]) m *= bodyPartDebuffs[(int)BodyPart.LeftArm].stunMultiplier;
            if (_slayedParts[(int)BodyPart.RightArm]) m *= bodyPartDebuffs[(int)BodyPart.RightArm].stunMultiplier;
            return m;
        }
    }

    // ── Stun ──────────────────────────────────────────────────────────────────

    [Header("Stun")]
    [Tooltip("Base stun duration in seconds. Scaled up by each slayed arm's stunMultiplier.")]
    public float stunDuration = 0.6f;
    [Tooltip("Accumulated stun needed to enter downed state.")]
    public float downThreshold = 2.5f;
    [Tooltip("How fast the stun accumulator decays per second while upright.")]
    public float stunDecayRate = 1f;

    // ── HP Drain ──────────────────────────────────────────────────────────────

    [Header("HP Drain")]
    [Tooltip("HP lost per second passively. Set to 0 to disable.")]
    public float hpDrainPerSecond = 2f;
    [Tooltip("HP drain cannot reduce HP below this value.")]
    public float hpDrainFloor = 1f;

    // ���� Light Attack ��������������������������������������������������������������������������������������������������������������������

    [Header("Combo")]
    public float lightStaminaCost = 20f;
    public float comboWindowDuration = 0.5f;
    [Tooltip("Strong hit damage = weak hit damage × this multiplier.")]
    public float comboStrongMultiplier = 1.5f;
    [Tooltip("Each entry is one combo step — each step fires weak then strong.")]
    public ComboHit[] comboHits = new ComboHit[3];

    // ── Run ───────────────────────────────────────────────────────────────────

    [Header("Run")]
    [Tooltip("Speed multiplier applied while running.")]
    public float runSpeedMultiplier = 1.6f;
    [Tooltip("Seconds of holding Shift while moving before running starts.")]
    public float runHoldTime = 0.3f;

    bool _isRunning;
    float _runHoldTimer;
    bool _shiftDown;

    // ───── Heavy Attack ──────────────────────────────────────────────────────

    [Header("Heavy Attack")]
    public float heavyDamage = 40f;
    public float heavyStaminaCost = 45f;
    public float heavyRange = 1.3f;
    public float heavyKnockback = 7f;
    public float heavyStartupDuration = 0.25f;
    public float heavySliceForcePower = 9f;

    // ── Bloodloss ─────────────────────────────────────────────────────────────

    [Header("Bloodloss")]
    public float bleedDps = 5f;
    public float bleedDuration = 4f;

    // ── Deathblow ─────────────────────────────────────────────────────────────

    [Header("Deathblow")]
    public float deathblowRange = 2f;
    public float deathblowBloodGain = 50f;
    public float deathblowSliceForce = 10f;

    // ── Quickdraw ─────────────────────────────────────────────────────────────

    [Header("Quickdraw")]
    public float quickdrawRange = 6f;
    public float quickdrawDamage = 30f;
    public float quickdrawStaminaCost = 30f;
    public float quickdrawCooldown = 1.0f;
    public float quickdrawBleedDps = 5f;
    public float quickdrawBleedDuration = 3f;

    // ── Smashdown ─────────────────────────────────────────────────────────────

    [Header("Smashdown")]
    public float smashdownDamage = 25f;
    public float smashdownRange = 0.9f;
    public float smashdownKnockback = 6f;
    public float smashdownStaminaCost = 20f;
    public float smashdownStartup = 0.1f;
    public float smashdownRecovery = 0.35f;
    public float smashdownCooldown = 1.0f;

    // ── Body Slam ─────────────────────────────────────────────────────────────

    [Header("Body Slam")]
    public float bodySlamStaminaCost = 30f;
    public float bodySlamSlipSpeed = 10f;
    public float bodySlamDuration = 0.45f;
    public float bodySlamHitRadius = 0.55f;
    public float bodySlamDamage = 20f;
    public float bodySlamKnockback = 8f;
    public float bodySlamSelfDamage = 10f;
    public float bodySlamSelfKnockback = 12f;
    public float bodySlamCooldown = 1.5f;

    // ── Grab Throw ────────────────────────────────────────────────────────────

    [Header("Grab Throw")]
    public float grabThrowStaminaCost = 30f;
    public float grabThrowCooldown = 1.0f;
    public float grabThrowStartup = 0.3f;
    public float grabRange = 1.5f;
    public float throwForce = 14f;
    public float throwCollisionDamage = 25f;
    public float throwDuration = 0.8f;

    // ── Berserker Mode ────────────────────────────────────────────────────────

    [Header("Berserker Mode")]
    public float berserkerDuration = 10f;
    [Tooltip("Seconds of holding Gather button to activate manually.")]
    public float berserkerHoldTime = 1.0f;

    // ── Rising Attack ─────────────────────────────────────────────────────────

    [Header("Rising Attack")]
    public float risingDamage = 20f;
    public float risingRange = 1.0f;
    public float risingKnockback = 12f;
    public float risingStaminaCost = 20f;
    public float risingStartup = 0.08f;
    public float risingRecovery = 0.3f;

    // ���� Ladder ��������������������������������������������������������������������������������������������������������������������������������

    [Header("Ladder")]
    public float climbSpeed = 3f;

    // ���� Dodge ����������������������������������������������������������������������������������������������������������������������������������

    [Header("Dodge")]
    public float dodgeForce = 10f;
    public float dodgeStaminaCost = 25f;
    public float dodgeDuration = 0.3f;
    public float iFrameDuration = 0.25f;

    // ���� Guard / Parry ������������������������������������������������������������������������������������������������������������������

    [Header("Guard")]
    public float guardDamageReduction = 0.6f;
    public float parryWindowDuration = 0.2f;
    public float parryStaminaCost = 15f;

    // ���� Slice ����������������������������������������������������������������������������������������������������������������������������������

    [Header("Slice")]
    public float sliceRange = 5f;
    public float sliceDamage = 999f;
    public float sliceCooldown = 0.6f;
    public float sliceStaminaCost = 30f;
    public float sliceForce;  // multiplied by damage to get actual slice force

    [Header("Slash Visual")]
    public Color slashColor = new Color(1f, 1f, 0.3f, 0.85f);
    public float slashDuration = 0.07f;
    public float slashWidth = 0.05f;

    // ���� Layers / References ������������������������������������������������������������������������������������������������������

    [Header("Layers")]
    public LayerMask enemyLayer;

    [Header("References")]
    public Transform hitPoint;
    [SerializeField] private GameObject bloodSpherePrefab;

    // ���� State ����������������������������������������������������������������������������������������������������������������������������������

    public bool IsInvincible { get; private set; }
    public bool IsGuarding { get; private set; }
    public bool IsStunned { get; private set; }
    public bool IsDown { get; private set; }
    float _stunAccumulator;

    bool isParryActive;
    bool isAttacking;
    bool isDodging;
    bool isSlamming;

    public bool IsBerserker { get; private set; }
    float _berserkerTimer;
    bool _berserkerUsed;
    bool _gatherHeld;
    float _gatherHoldTimer;

    float _quickdrawCooldownTimer;
    float _smashdownCooldownTimer;
    float _bodySlamCooldownTimer;
    float _grabThrowCooldownTimer;

    int comboStep;
    float comboTimer;
    bool comboInputQueued;

    float staminaRegenTimer;
    float moveInput;
    float climbInput;
    bool jumpQueued;
    int lastFacingDir = 1;

    bool isOnSlope;
    Vector2 slopeNormal;

    bool isNearLadder;
    bool isOnLadder;

    LineRenderer slashLine;

    Rigidbody2D rb;
    SpriteRenderer sr;
    _2DActions actions;
    EffectGenerator effects;
    public BloodPuddleMaker bloodPuddleMaker;
    PlayerMagicSkill playerMagicSkill;

    // ���� Unity ����������������������������������������������������������������������������������������������������������������������������������

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        sr = GetComponent<SpriteRenderer>();
        CurrentHp = maxHp;
        CurrentStamina = maxStamina;

        actions = new _2DActions();
        slashLine = BuildSlashLine();
        effects = GetComponent<EffectGenerator>();
        bloodPuddleMaker = GetComponent<BloodPuddleMaker>();
        playerMagicSkill = GetComponent<PlayerMagicSkill>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        actions.Player2D.Move.performed += OnMove;
        actions.Player2D.Move.canceled += OnMove;
        actions.Player2D.Jump.performed += OnJump;
        actions.Player2D.LightAttack.performed += OnAttack;
        actions.Player2D.Dodge.performed += OnDodge;
        actions.Player2D.Dodge.canceled += OnDodgeRelease;
        actions.Player2D.Guard.performed += OnGuardStart;
        actions.Player2D.Guard.canceled += OnGuardEnd;
        actions.Player2D.Gather.started += OnGatherDown;
        actions.Player2D.Gather.performed += OnGather;
        actions.Player2D.Gather.canceled += OnGatherUp;
        actions.Player2D.Skill.started += OnSkillPress;
        actions.Player2D.Skill.canceled += OnSkillRelease;
        actions.Player2D.GrabThrow.performed += OnGrabThrow;
        actions.Player2D.Enable();
    }

    void OnDisable()
    {
        actions.Player2D.Move.performed -= OnMove;
        actions.Player2D.Move.canceled -= OnMove;
        actions.Player2D.Jump.performed -= OnJump;
        actions.Player2D.LightAttack.performed -= OnAttack;
        actions.Player2D.Dodge.performed -= OnDodge;
        actions.Player2D.Dodge.canceled -= OnDodgeRelease;
        actions.Player2D.Guard.performed -= OnGuardStart;
        actions.Player2D.Guard.canceled -= OnGuardEnd;
        actions.Player2D.Gather.started -= OnGatherDown;
        actions.Player2D.Gather.performed -= OnGather;
        actions.Player2D.Gather.canceled -= OnGatherUp;
        actions.Player2D.Skill.started -= OnSkillPress;
        actions.Player2D.Skill.canceled -= OnSkillRelease;
        actions.Player2D.GrabThrow.performed -= OnGrabThrow;
        actions.Player2D.Disable();
    }

    void Start()
    {
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        TickComboWindow();
        TickStaminaRegen();
        TickHpDrain();
        TickRun();
        TickBerserker();
        TickStunAccumulator();
        TickSkillCooldowns();
        CheckSlope();
    }

    void FixedUpdate()
    {
        if (isDodging || isSlamming) return;

        if (isNearLadder && !isOnLadder && climbInput != 0f && !AllLimbsCut)
            EnterLadder();

        if (isOnLadder)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(moveInput * EffectiveSpeed, climbInput * climbSpeed);
            return;
        }

        if (jumpQueued)
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = new Vector2(moveInput * EffectiveSpeed, EffectiveJumpForce);
            jumpQueued = false;
            return;
        }

        if (isOnSlope && moveInput != 0)
        {
            Vector2 slopeDir = new(slopeNormal.y, -slopeNormal.x);
            rb.gravityScale = 0f;
            rb.linearVelocity = moveInput * EffectiveSpeed * slopeDir;
        }
        else if (isOnSlope && moveInput == 0)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = new Vector2(moveInput * EffectiveSpeed, rb.linearVelocity.y);
        }
    }

    // ���� Input Callbacks ��������������������������������������������������������������������������������������������������������������
    void CheckSlope()
    {
        if (IsGrounded())
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.3f, groundLayer);
            if (hit.collider != null)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                isOnSlope = slopeAngle > 0f && slopeAngle <= 45f;
                slopeNormal = hit.normal;
                return;
            }
        }
        isOnSlope = false;
        slopeNormal = Vector2.up;
    }

    void OnMove(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        float prevClimbInput = climbInput;
        moveInput = input.x;
        climbInput = input.y;
        if (moveInput != 0f)
            lastFacingDir = (int)Mathf.Sign(moveInput);

        if (_isRunning && climbInput > 0.5f && prevClimbInput <= 0.5f && !isDodging && !IsGuarding && !IsStunned)
            BodySlamSkill();
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (IsDown)
        {
            RecoverFromDown();
            return;
        }
        if (isOnLadder)
        {
            ExitLadder();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, EffectiveJumpForce * 0.6f);
            return;
        }
        if (!CanJump) return;
        if (IsGrounded())
            jumpQueued = true;
    }

    void OnAttack(InputAction.CallbackContext ctx)
    {
        if (isDodging || IsGuarding || IsStunned || IsDown) return;

        if (_isRunning) { QuickdrawSkill(); return; }

        if (climbInput > 0.5f && CanWeakAttack) { RisingAttackSkill(); return; }
        if (climbInput < -0.5f && CanWeakAttack && !IsGrounded()) { SmashdownSkill(); return; }

        // Queue next combo hit while an attack is already running
        if (isAttacking) { comboInputQueued = true; return; }

        bool doWeak = CanWeakAttack;
        bool doStrong = CanStrongAttack;
        if (!doWeak && !doStrong) return;

        // Both arms — combo (each step = weak → strong)
        if (doWeak && doStrong)
        {
            if (!SpendStamina(lightStaminaCost + heavyStaminaCost)) return;
            comboStep = 0;
            StartCoroutine(ComboCoroutine());
        }
        else
        {
            // One arm cut — fall back to single chain
            float cost = (doWeak ? lightStaminaCost : 0f) + (doStrong ? heavyStaminaCost : 0f);
            if (!SpendStamina(cost)) return;
            StartCoroutine(ChainAttackCoroutine(doWeak, doStrong));
        }
    }



    void OnDodge(InputAction.CallbackContext ctx)
    {
        _shiftDown = true;
        _runHoldTimer = 0f;
        if (moveInput == 0f)
            TryDodge();
    }

    void OnDodgeRelease(InputAction.CallbackContext ctx)
    {
        if (_shiftDown && !_isRunning)
            TryDodge();
        _shiftDown = false;
        _isRunning = false;
        _runHoldTimer = 0f;
    }

    void TryDodge()
    {
        if (isDodging || AllLimbsCut || !SpendStamina(dodgeStaminaCost)) return;
        StartCoroutine(DodgeCoroutine());
    }

    void OnGuardStart(InputAction.CallbackContext ctx)
    {
        if (isDodging || isAttacking || (!CanWeakAttack && !CanStrongAttack)) return;
        IsGuarding = true;
        StartCoroutine(ParryWindowCoroutine());
    }

    void OnGuardEnd(InputAction.CallbackContext ctx)
    {
        IsGuarding = false;
        isParryActive = false;
    }

    void OnGather(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        if (!TryDeathblow())
            SphereSummon();
    }

    void SphereSummon()
    {
        Instantiate(bloodSpherePrefab, transform.position, Quaternion.identity);
    }

    bool TryDeathblow()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, deathblowRange, enemyLayer);

        Collider2D target = null;
        float bestDist = float.MaxValue;
        foreach (var col in hits)
        {
            bool bleeding = col.TryGetComponent<MeleeMonster>(out var mm) && mm.IsBleeding;
            if (!bleeding) continue;
            float d = Vector2.Distance(transform.position, col.bounds.center);
            if (d < bestDist) { bestDist = d; target = col; }
        }

        if (target == null) return false;

        if (target.TryGetComponent<IDamageable>(out var damageable))
            damageable.TakeDamage(99999f);

        if (target.TryGetComponent<EnemySliceable>(out var sliceable))
        {
            Vector2 toEnemy = ((Vector2)target.bounds.center - (Vector2)transform.position).normalized;
            Vector2 sliceNormal = new Vector2(-toEnemy.y, toEnemy.x);
            sliceable.Slice(sliceNormal, (Vector2)target.bounds.center, deathblowSliceForce, transform.position);
            SpawnBlood((Vector2)target.bounds.center, -toEnemy, sliceable.Money, sliceable.HpHeal);
            if (bloodPuddleMaker != null)
                bloodPuddleMaker.SpawnStrongPuddle((Vector2)target.bounds.center, sliceable.Money, sliceable.HpHeal);
        }

        AddBloodGage(deathblowBloodGain);
        return true;
    }

    void OnSkillPress(InputAction.CallbackContext ctx) => playerMagicSkill?.OnMagicSkillPress();
    void OnSkillRelease(InputAction.CallbackContext ctx) => playerMagicSkill?.OnMagicSkillRelease();

    void OnGatherDown(InputAction.CallbackContext ctx) => _gatherHeld = true;
    void OnGatherUp(InputAction.CallbackContext ctx) { _gatherHeld = false; _gatherHoldTimer = 0f; }

    // ── Berserker Mode ────────────────────────────────────────────────────────

    void TickBerserker()
    {
        if (IsDead) return;

        // HP condition: triggers automatically when HP hits 1
        if (!IsBerserker && !_berserkerUsed && CurrentHp <= 1f)
        {
            ActivateBerserker();
            return;
        }

        // Hold condition: hold Gather button for berserkerHoldTime
        if (!IsBerserker && !_berserkerUsed && _gatherHeld)
        {
            _gatherHoldTimer += Time.deltaTime;
            if (_gatherHoldTimer >= berserkerHoldTime)
            {
                _gatherHoldTimer = 0f;
                ActivateBerserker();
            }
        }

        // Countdown while active
        if (IsBerserker)
        {
            CurrentStamina = maxStamina;
            _berserkerTimer -= Time.deltaTime;
            if (_berserkerTimer <= 0f)
                DeactivateBerserker();
        }
    }

    void ActivateBerserker()
    {
        Debug.Log("Berserker Mode Activated!");
        IsBerserker = true;
        _berserkerUsed = true;
        _berserkerTimer = berserkerDuration;
        RestoreAllLimbs();
    }

    public void RestoreBerserker()
    {
        _berserkerUsed = false;
    }

    void DeactivateBerserker()
    {
        IsBerserker = false;
        CurrentHp = 1f;
        CurrentStamina = 0f;
        CurrentBloodGage = 0f;
        // effects added later
    }

    //���� Slice ����������������������������������������������������������������������������������������������������������������������������������

    void DoSlice(float forcePower = 0f)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.3f;

        Vector2 mouseWorld = CameraFollow2D.GameCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 sliceDir = (mouseWorld - origin);
        if (sliceDir.sqrMagnitude < 0.001f) sliceDir = Vector2.right * FacingDir();
        sliceDir.Normalize();

        Vector2 endpoint = origin + sliceDir * sliceRange;

        // Penetrating ray — hits every enemy along the line
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, sliceDir, sliceRange, enemyLayer);

        // Cut-plane normal: 90° CCW of the slice direction
        Vector2 sliceNormal = new Vector2(-sliceDir.y, sliceDir.x);

        foreach (var hit in hits)
        {
            if (hit.collider.TryGetComponent<EnemySliceable>(out var sliceable))
            {
                SpawnBlood(hit.point, hit.normal, sliceable.Money / 10, sliceable.HpHeal / 10f);

                bool alreadyDead = hit.collider.TryGetComponent<IDamageable>(out var preCheck) && preCheck.IsDead;
                if (!alreadyDead && hit.collider.TryGetComponent<IDamageable>(out var d))
                    d.TakeDamage(sliceDamage);

                bool isDead = alreadyDead || !hit.collider.TryGetComponent<IDamageable>(out var m) || m.IsDead;
                if (isDead)
                {
                    sliceable.Slice(sliceNormal, hit.point, forcePower, transform.position);
                    if (bloodPuddleMaker != null) bloodPuddleMaker.SpawnStrongPuddle(hit.point, sliceable.Money, sliceable.HpHeal);
                }
            }
            else
            {
                SpawnBlood(hit.point, hit.normal);
                if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
                    damageable.TakeDamage(sliceDamage);
            }
        }

        StartCoroutine(ShowSlashLine(origin, endpoint));
    }

    IEnumerator ShowSlashLine(Vector2 start, Vector2 end)
    {
        slashLine.enabled = true;
        slashLine.SetPosition(0, start);
        slashLine.SetPosition(1, end);

        yield return new WaitForSeconds(slashDuration);

        slashLine.enabled = false;
    }

    LineRenderer BuildSlashLine()
    {
        var lr = gameObject.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth = slashWidth;
        lr.endWidth = slashWidth * 0.4f;
        lr.useWorldSpace = true;
        lr.enabled = false;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = slashColor;
        lr.endColor = new Color(slashColor.r, slashColor.g, slashColor.b, 0f);
        lr.sortingOrder = 10;
        return lr;
    }

    // ���� Light Attack Combo ��������������������������������������������������������������������������������������������������������

    IEnumerator ComboCoroutine()
    {
        isAttacking = true;
        comboInputQueued = false;
        float ad = AttackDelayMultiplier;

        var hit = comboHits[comboStep];

        // Weak hit
        yield return new WaitForSeconds(hit.startup * ad);
        HitEnemies(HitOrigin(hit.range), hit.range * 0.6f, hit.damage, hit.knockback, FacingDir(), bleedDps, bleedDuration);
        DoSlice(hit.damage * sliceForce);

        // Strong hit
        float strongDamage = hit.damage * comboStrongMultiplier;
        yield return new WaitForSeconds(hit.chainGap * ad);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
        HitEnemies(HitOrigin(heavyRange), heavyRange, strongDamage, heavyKnockback, FacingDir(), bleedDps, bleedDuration);
        DoSlice(strongDamage * sliceForce);

        yield return new WaitForSeconds(hit.recovery * ad);

        isAttacking = false;

        if (comboInputQueued && comboStep < comboHits.Length - 1)
        {
            comboStep++;
            comboTimer = comboWindowDuration;
            comboInputQueued = false;
            if (SpendStamina(lightStaminaCost + heavyStaminaCost))
                StartCoroutine(ComboCoroutine());
        }
        else
        {
            comboStep = 0;
            comboTimer = 0f;
            comboInputQueued = false;
        }
    }

    void TickComboWindow()
    {
        if (comboStep == 0) return;

        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f)
        {
            comboStep = 0;
            comboInputQueued = false;
        }
    }

    // ── Chain Attack ──────────────────────────────────────────────────────────

    IEnumerator ChainAttackCoroutine(bool doWeak, bool doStrong)
    {
        isAttacking = true;
        float ad = AttackDelayMultiplier;

        if (doWeak)
        {
            var h = comboHits.Length > 0 ? comboHits[0] : default;
            yield return new WaitForSeconds(h.startup * ad);
            HitEnemies(HitOrigin(h.range), h.range * 0.6f, h.damage, h.knockback, FacingDir(), bleedDps, bleedDuration);
            DoSlice(h.damage * sliceForce);
            yield return new WaitForSeconds(h.recovery * ad);
        }

        if (doStrong)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
            yield return new WaitForSeconds(heavyStartupDuration * ad);
            HitEnemies(HitOrigin(heavyRange), heavyRange, heavyDamage, heavyKnockback, FacingDir(), bleedDps, bleedDuration);
            DoSlice(heavyDamage * sliceForce);
            yield return new WaitForSeconds(0.2f * ad);
        }

        isAttacking = false;
    }


    static readonly WaitForSeconds s_quickdrawRecovery = new(0.15f);

    void TickSkillCooldowns()
    {
        float dt = Time.deltaTime;
        if (_quickdrawCooldownTimer > 0f) _quickdrawCooldownTimer -= dt;
        if (_smashdownCooldownTimer > 0f) _smashdownCooldownTimer -= dt;
        if (_bodySlamCooldownTimer > 0f) _bodySlamCooldownTimer -= dt;
        if (_grabThrowCooldownTimer > 0f) _grabThrowCooldownTimer -= dt;
    }

    // ── Quickdraw Skill ───────────────────────────────────────────────────────
    void QuickdrawSkill()
    {
        if (_quickdrawCooldownTimer > 0f && !IsBerserker) return;
        if (!SpendStamina(quickdrawStaminaCost)) return;
        if (!IsBerserker) _quickdrawCooldownTimer = quickdrawCooldown;
        StartCoroutine(QuickdrawCoroutine());
    }

    IEnumerator QuickdrawCoroutine()
    {
        isAttacking = true;
        IsInvincible = true;

        Vector2 origin = transform.position;
        Vector2 dir = Vector2.right * FacingDir();

        // Stop at ground-layer walls
        RaycastHit2D wallHit = Physics2D.Raycast(origin, dir, quickdrawRange, groundLayer);
        Vector2 destination = wallHit.collider != null
            ? wallHit.point - dir * 0.15f
            : origin + dir * quickdrawRange;

        float dist = Vector2.Distance(origin, destination);

        // Slice every enemy along the path
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, dir, dist, enemyLayer);
        Vector2 sliceNormal = new(-dir.y, dir.x);
        foreach (var hit in hits)
        {
            SpawnBlood(hit.point, hit.normal);
            if (hit.collider.TryGetComponent<EnemySliceable>(out var sliceable))
            {
                bool alreadyDead = hit.collider.TryGetComponent<IDamageable>(out var preCheck) && preCheck.IsDead;
                if (!alreadyDead && hit.collider.TryGetComponent<IDamageable>(out var d)) d.TakeDamage(quickdrawDamage);
                if (!alreadyDead) ApplyBleedToCollider(hit.collider);
                bool isDead = alreadyDead || !hit.collider.TryGetComponent<IDamageable>(out var m) || m.IsDead;
                if (isDead)
                {
                    sliceable.Slice(sliceNormal, hit.point, deathblowSliceForce, transform.position);
                    if (bloodPuddleMaker != null) bloodPuddleMaker.SpawnStrongPuddle(hit.point, sliceable.Money, sliceable.HpHeal);
                }
            }
            else if (hit.collider.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(quickdrawDamage);
                ApplyBleedToCollider(hit.collider);
            }
        }

        // Draw slash line along the full travel path then teleport
        StartCoroutine(ShowSlashLine(origin, destination));
        transform.position = destination;

        yield return s_quickdrawRecovery;

        IsInvincible = false;
        isAttacking = false;
    }

    void ApplyBleedToCollider(Collider2D col)
    {
        if (col.TryGetComponent<MeleeMonster>(out var mm)) mm.ApplyBloodloss(quickdrawBleedDps, quickdrawBleedDuration);
    }

    // ── Rising Attack Skill ───────────────────────────────────────────────────

    void RisingAttackSkill()
    {
        if (!SpendStamina(risingStaminaCost)) return;
        StartCoroutine(RisingAttackCoroutine());
    }

    IEnumerator RisingAttackCoroutine()
    {
        isAttacking = true;
        float ad = AttackDelayMultiplier;

        yield return new WaitForSeconds(risingStartup * ad);

        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.4f;
        HitEnemies(origin, risingRange, risingDamage, new Vector2(FacingDir() * 2f, risingKnockback), bleedDps, bleedDuration);

        yield return new WaitForSeconds(risingRecovery * ad);
        isAttacking = false;
    }

    // ── Smashdown Skill ───────────────────────────────────────────────────────

    void SmashdownSkill()
    {
        if (_smashdownCooldownTimer > 0f && !IsBerserker) return;
        if (!SpendStamina(smashdownStaminaCost)) return;
        if (!IsBerserker) _smashdownCooldownTimer = smashdownCooldown;
        StartCoroutine(SmashdownCoroutine());
    }

    IEnumerator SmashdownCoroutine()
    {
        isAttacking = true;
        float ad = AttackDelayMultiplier;

        yield return new WaitForSeconds(smashdownStartup * ad);

        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.4f;
        HitEnemies(origin, smashdownRange, smashdownDamage, smashdownKnockback, FacingDir(), bleedDps, bleedDuration);

        yield return new WaitForSeconds(smashdownRecovery * ad);
        isAttacking = false;
    }

    // ── Grab Throw Skill ──────────────────────────────────────────────────────

    void OnGrabThrow(InputAction.CallbackContext ctx)
    {
        if (isDodging || IsGuarding || IsStunned || IsDown) return;
        GrabThrowSkill();
    }

    void GrabThrowSkill()
    {
        if (_grabThrowCooldownTimer > 0f && !IsBerserker) return;

        Collider2D target = FindNearestInRange((Vector2)transform.position, grabRange, enemyLayer);
        if (target == null) return;

        if (!SpendStamina(grabThrowStaminaCost)) return;
        if (!IsBerserker) _grabThrowCooldownTimer = grabThrowCooldown;
        StartCoroutine(GrabThrowCoroutine(target));
    }

    IEnumerator GrabThrowCoroutine(Collider2D targetCol)
    {
        isAttacking = true;

        targetCol.TryGetComponent<MeleeMonster>(out var mm);

        if (mm == null) { isAttacking = false; yield break; }

        mm.StartGrab();

        yield return new WaitForSeconds(grabThrowStartup);

        int dir = FacingDir();
        Vector2 throwVel = new Vector2(dir * throwForce, throwForce * 0.35f);

        mm.Throw(throwVel, throwCollisionDamage, throwDuration, enemyLayer);

        yield return new WaitForSeconds(0.2f);
        isAttacking = false;
    }

    Collider2D FindNearestInRange(Vector2 origin, float range, LayerMask layer)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range, layer);
        Collider2D best = null;
        float bestDist = float.MaxValue;
        foreach (var col in hits)
        {
            float d = Vector2.Distance(origin, col.bounds.center);
            if (d < bestDist) { bestDist = d; best = col; }
        }
        return best;
    }

    // ── Body Slam Skill ───────────────────────────────────────────────────────

    void BodySlamSkill()
    {
        if (isSlamming) return;
        if (_bodySlamCooldownTimer > 0f && !IsBerserker) return;
        if (!SpendStamina(bodySlamStaminaCost)) return;
        if (!IsBerserker) _bodySlamCooldownTimer = bodySlamCooldown;
        StartCoroutine(BodySlamCoroutine());
    }

    IEnumerator BodySlamCoroutine()
    {
        _isRunning = false;
        isAttacking = true;
        isSlamming = true;

        int dir = FacingDir();
        float elapsed = 0f;
        bool collided = false;

        while (elapsed < bodySlamDuration && !collided)
        {
            rb.linearVelocity = new Vector2(dir * bodySlamSlipSpeed, rb.linearVelocity.y);

            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, bodySlamHitRadius, enemyLayer);
            if (hits.Length > 0)
            {
                collided = true;
                foreach (var col in hits)
                {
                    if (col.TryGetComponent<IDamageable>(out var target))
                        target.TakeDamage(bodySlamDamage);

                    var kbForce = new Vector2(dir * bodySlamKnockback, 2f);
                    if (col.TryGetComponent<MeleeMonster>(out var mm))
                        mm.ApplyKnockback(kbForce, smashdownRecovery);
                    else if (col.TryGetComponent<Rigidbody2D>(out var targetRb))
                        targetRb.AddForce(kbForce, ForceMode2D.Impulse);
                }

                TakeDamage(bodySlamSelfDamage);
                rb.linearVelocity = new Vector2(-dir * bodySlamSelfKnockback, rb.linearVelocity.y);
            }
        }

        if (!collided)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        // Hold isSlamming during recovery so FixedUpdate doesn't cancel the rebound velocity.
        yield return new WaitForSeconds(smashdownRecovery);

        isSlamming = false;
        isAttacking = false;
    }

    // ── Stun ──────────────────────────────────────────────────────────────────

    public void ApplyStun()
    {
        TryApplyStun(stunDuration * ArmStunMultiplier);
    }

    void TryApplyStun(float scaled)
    {
        _stunAccumulator += scaled;
        if (IsStunned || IsDown || _stunAccumulator >= downThreshold)
            EnterDown();
        else
            StartCoroutine(StunCoroutine(scaled));
    }

    IEnumerator StunCoroutine(float duration)
    {
        IsStunned = true;
        SetInputEnabled(false);
        yield return new WaitForSeconds(duration);
        if (!IsDown)
        {
            IsStunned = false;
            SetInputEnabled(true);
        }
    }

    void TickStunAccumulator()
    {
        if (_stunAccumulator > 0f && !IsDown)
            _stunAccumulator = Mathf.Max(0f, _stunAccumulator - stunDecayRate * Time.deltaTime);
    }

    void EnterDown()
    {
        StopCoroutine(nameof(StunCoroutine));
        IsStunned = false;
        IsDown = true;
        SetInputEnabled(false);
        HUDDisplay.Log("다운! 점프로 일어나세요.");
    }

    void RecoverFromDown()
    {
        IsDown = false;
        _stunAccumulator = 0f;
        SetInputEnabled(true);
    }

    // ───── Dodge ──────────────────────────────────────────────────────────────

    IEnumerator DodgeCoroutine()
    {
        isDodging = true;
        isAttacking = false;
        float dd = DodgeDelayMultiplier;

        int dodgeDir = moveInput != 0f ? (int)Mathf.Sign(moveInput) : lastFacingDir;

        int playerLayer = gameObject.layer;
        int enemyLayerIndex = Mathf.RoundToInt(Mathf.Log(enemyLayer.value, 2));
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayerIndex, true);

        IsInvincible = true;
        rb.linearVelocity = new Vector2(dodgeDir * EffectiveDodgeForce, rb.linearVelocity.y);

        yield return new WaitForSeconds(iFrameDuration * dd);

        IsInvincible = false;

        yield return new WaitForSeconds((dodgeDuration - iFrameDuration) * dd);

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayerIndex, false);
        isDodging = false;
    }

    // ���� Guard / Parry ������������������������������������������������������������������������������������������������������������������

    IEnumerator ParryWindowCoroutine()
    {
        isParryActive = true;
        yield return new WaitForSeconds(parryWindowDuration);
        isParryActive = false;
    }

    // ���� IDamageable ����������������������������������������������������������������������������������������������������������������������

    public void TakeDamage(float amount, float stunDuration = 0f)
    {
        if (IsInvincible || IsBerserker) return;

        if (IsDown) EnterDown();

        if (IsGuarding)
        {
            if (isParryActive)
            {
                SpendStamina(parryStaminaCost);
                StartCoroutine(HitFlash(Color.yellow));
                return;
            }

            amount *= (1f - guardDamageReduction);
        }

        CurrentHp -= amount;
        StartCoroutine(HitFlash(Color.red));
        HUDDisplay.Log($"{(int)amount} 데미지를 입었다!");

        if (stunDuration > 0f)
            TryApplyStun(stunDuration * ArmStunMultiplier);

        if (isAttacking)
            SlayRandomBodyPart();

        if (CurrentHp <= 1f && !_berserkerUsed)
        {
            CurrentHp = 1f;
            ActivateBerserker();
            return;
        }

        if (CurrentHp <= 0f)
            Die();
    }

    void SlayRandomBodyPart()
    {
        if (IsBerserker) return;

        var available = new System.Collections.Generic.List<BodyPart>();
        foreach (BodyPart part in System.Enum.GetValues(typeof(BodyPart)))
            if (!IsSlayed(part)) available.Add(part);

        if (available.Count == 0) return;
        SlayBodyPart(available[Random.Range(0, available.Count)]);
    }

    void Die()
    {
        CurrentHp = 0f;
        IsDead = true;
        SetInputEnabled(false);
        rb.linearVelocity = Vector2.zero;
        Debug.Log("Player died.");
    }

    // ���� Stamina ������������������������������������������������������������������������������������������������������������������������������

    public bool SpendStamina(float cost)
    {
        if (CurrentStamina < cost) return false;
        CurrentStamina -= cost;
        staminaRegenTimer = staminaRegenDelay;
        return true;
    }

    void TickStaminaRegen()
    {
        if (staminaRegenTimer > 0f)
        {
            staminaRegenTimer -= Time.deltaTime;
            return;
        }

        CurrentStamina = Mathf.Min(CurrentStamina + staminaRegen * Time.deltaTime, maxStamina);
    }

    void TickRun()
    {
        if (_shiftDown && moveInput != 0f && !isDodging && !isAttacking && !IsDown)
        {
            _runHoldTimer += Time.deltaTime;
            if (_runHoldTimer >= runHoldTime && IsGrounded())
                _isRunning = true;
        }
    }

    void TickHpDrain()
    {
        if (IsDead || hpDrainPerSecond <= 0f || CurrentHp <= hpDrainFloor) return;
        CurrentHp = Mathf.Max(CurrentHp - hpDrainPerSecond * Time.deltaTime, hpDrainFloor);
    }

    // ���� BloodGage ����������������������������������������������������������������������������������������������������������������������������������

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHp = Mathf.Min(CurrentHp + amount, maxHp);
        HUDDisplay.Log($"HP {(int)amount} 회복!");
    }

    public void RestoreStamina(float amount)
    {
        CurrentStamina = Mathf.Min(CurrentStamina + amount, maxStamina);
    }

    public void AddBloodGage(float amount)
    {
        CurrentBloodGage = Mathf.Min(CurrentBloodGage + amount, maxBloodGage);
        if (CurrentBloodGage >= maxBloodGage)
        {
            CurrentBloodGage = 0f;
            HealNextBodyPart();
        }
    }

    void HealNextBodyPart()
    {
        foreach (BodyPart part in HealPriority)
        {
            if (!_slayedParts[(int)part]) continue;
            _slayedParts[(int)part] = false;
            HUDDisplay.Log($"{part} healed.");
            if (part == BodyPart.Head) ApplyHeadEffects(false);
            return;
        }
    }

    public void RestoreAllLimbs()
    {
        bool headWasSlayed = _slayedParts[(int)BodyPart.Head];
        for (int i = 0; i < _slayedParts.Length; i++)
            _slayedParts[i] = false;
        if (headWasSlayed) ApplyHeadEffects(false);
    }

    void ApplyHeadEffects(bool slayed)
    {
        if (ScreenFader.Instance != null)
        {
            if (slayed) ScreenFader.Instance.GameFadeIn(headFadeDuration);
            else ScreenFader.Instance.GameFadeOut(headFadeDuration);
        }
        SoundManager.SetHeadMuffle(slayed);
    }
    public bool ConsumeBloodGage(float amount)
    {
        if (CurrentBloodGage < amount)
            return false;
        CurrentBloodGage = Mathf.Max(CurrentBloodGage - amount, 0f);
        return true;
    }

    public void AddBloodMoney(int amount)
    {
        CurrentBloodMoney += amount;
    }

    public bool SpendBloodMoney(int amount)
    {
        if (CurrentBloodMoney < amount) return false;
        CurrentBloodMoney -= amount;
        return true;
    }

    public void SetBloodMoney(int amount)
    {
        CurrentBloodMoney = Mathf.Max(amount, 0);
    }

    // ���� Helpers ������������������������������������������������������������������������������������������������������������������������������

    void HitEnemies(Vector2 origin, float radius, float damage, Vector2 knockbackForce, float bleedDps = 0f, float bleedDuration = 0f)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, enemyLayer);
        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(damage);
                if (target.IsDead && bloodPuddleMaker != null)
                    bloodPuddleMaker.SpawnStrongPuddle((Vector2)col.bounds.center);
            }

            if (bleedDps > 0f)
            {
                if (col.TryGetComponent<MeleeMonster>(out var mm)) mm.ApplyBloodloss(bleedDps, bleedDuration);
            }

            if (col.TryGetComponent<Rigidbody2D>(out var targetRb))
                targetRb.AddForce(knockbackForce, ForceMode2D.Impulse);

            Vector2 contactPt = col.ClosestPoint(origin);
            Vector2 normal = (contactPt - (Vector2)col.bounds.center).normalized;
            SpawnBlood(contactPt, normal);
        }
    }

    void HitEnemies(Vector2 origin, float radius, float damage, float knockback, int dir, float bleedDps = 0f, float bleedDuration = 0f)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, enemyLayer);
        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(damage);
                if (target.IsDead && bloodPuddleMaker != null)
                    bloodPuddleMaker.SpawnStrongPuddle((Vector2)col.bounds.center);
            }

            if (bleedDps > 0f)
            {
                if (col.TryGetComponent<MeleeMonster>(out var mm)) mm.ApplyBloodloss(bleedDps, bleedDuration);
            }

            if (col.TryGetComponent<Rigidbody2D>(out var targetRb))
                targetRb.AddForce(new Vector2(dir * knockback, 2f), ForceMode2D.Impulse);

            Vector2 contactPt = col.ClosestPoint(origin);
            Vector2 normal = (contactPt - (Vector2)col.bounds.center).normalized;
            SpawnBlood(contactPt, normal);
        }
    }

    void SpawnBlood(Vector2 point, Vector2 normal, int moneyValue = 0, float hpHeal = 0f)
    {
        effects?.SpawnBlood(point, normal);
        if (bloodPuddleMaker != null)
        {
            Vector2 _randomPoint = new Vector2(point.x + Random.Range(-0.5f, 0.5f), point.y);
            bloodPuddleMaker.SpawnPuddle(_randomPoint, moneyValue, hpHeal);
        }
    }


    Vector2 HitOrigin(float range)
    {
        return hitPoint != null
            ? (Vector2)hitPoint.position
            : (Vector2)transform.position + Vector2.right * FacingDir() * range * 0.5f;
    }

    public void SetNearLadder(bool near)
    {
        isNearLadder = near;
        if (!near) ExitLadder();
    }

    void EnterLadder()
    {
        isOnLadder = true;
        jumpQueued = false;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    public void ExitLadder()
    {
        isOnLadder = false;
        rb.gravityScale = 1f;
    }

    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
            actions.Player2D.Enable();
        else
        {
            actions.Player2D.Disable();
            moveInput = 0f;
            jumpQueued = false;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    // ── Body Part Slay ────────────────────────────────────────────────────────

    public void SlayBodyPart(BodyPart part)
    {
        int i = (int)part;
        if (_slayedParts[i]) return;
        _slayedParts[i] = true;
        HUDDisplay.Log($"{part} slayed — debuff applied.");
        if (effects != null) effects.SpawnSlicedLimb(part, transform.position);
        if (part == BodyPart.Head) ApplyHeadEffects(true);

        if (part == BodyPart.Torso)
        {
            SlayBodyPart(BodyPart.LeftLeg);
            SlayBodyPart(BodyPart.RightLeg);
        }
    }

    public bool IsSlayed(BodyPart part) => _slayedParts[(int)part];

    // Returns the combined multiplier for a stat across all slayed parts.
    // Usage example: float effectiveSpeed = speed * GetCombinedMultiplier(d => d.speedMultiplier);
    public float GetCombinedMultiplier(System.Func<BodyPartDebuff, float> selector)
    {
        float result = 1f;
        for (int i = 0; i < _slayedParts.Length; i++)
            if (_slayedParts[i]) result *= selector(bodyPartDebuffs[i]);
        return result;
    }

    public int FacingDir() => lastFacingDir;

    bool IsGrounded()
    {
        return Physics2D.OverlapCircle(groundCheck.position, 0.1f, groundLayer);
    }

    IEnumerator HitFlash(Color flashColor)
    {
        if (sr == null) yield break;
        sr.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }

    // ���� Gizmos ��������������������������������������������������������������������������������������������������������������������������������

    void OnDrawGizmosSelected()
    {
        float gizmoRange = comboHits != null && comboHits.Length > 0 ? comboHits[0].range : 1f;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(HitOrigin(gizmoRange), gizmoRange * 0.6f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(HitOrigin(heavyRange), heavyRange);

        // Slice ray — direction follows mouse at runtime; show a range circle in editor
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.3f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, sliceRange);
    }
}
