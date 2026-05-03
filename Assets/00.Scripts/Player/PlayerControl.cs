using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    // ── HP Drain ──────────────────────────────────────────────────────────────

    [Header("HP Drain")]
    [Tooltip("HP lost per second passively. Set to 0 to disable.")]
    public float hpDrainPerSecond = 2f;
    [Tooltip("HP drain cannot reduce HP below this value.")]
    public float hpDrainFloor = 1f;

    // ���� Light Attack ��������������������������������������������������������������������������������������������������������������������

    [Header("Light Attack")]
    public float lightDamage = 15f;
    public float lightStaminaCost = 20f;
    public float lightRange = 1.0f;
    public float lightKnockback = 3f;
    public float lightSliceForcePower = 4f;
    public float comboWindowDuration = 0.5f;
    public int maxComboSteps = 3;

    // ���� Heavy Attack ��������������������������������������������������������������������������������������������������������������������

    [Header("Heavy Attack")]
    public float heavyDamage = 40f;
    public float heavyStaminaCost = 45f;
    public float heavyRange = 1.3f;
    public float heavyKnockback = 7f;
    public float heavyStartupDuration = 0.25f;
    public float heavySliceForcePower = 9f;

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
    public float sliceForcePower = 6f;

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

    bool isParryActive;
    bool isAttacking;
    bool isDodging;

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

    float nextSliceTime;
    LineRenderer slashLine;

    Rigidbody2D rb;
    SpriteRenderer sr;
    _2DActions actions;
    EffectGenerator effects;
    public BloodPuddleMaker bloodPuddleMaker;
    PlayerSkill playerSkill;

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
        playerSkill = GetComponent<PlayerSkill>();
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
        actions.Player2D.LightAttack.performed += OnLightAttack;
        //actions.Player2D.HeavyAttack.performed += OnHeavyAttack;
        actions.Player2D.Dodge.performed += OnDodge;
        actions.Player2D.Guard.performed += OnGuardStart;
        actions.Player2D.Guard.canceled += OnGuardEnd;
        actions.Player2D.Parry.performed += OnParry;
        actions.Player2D.Gather.performed += ctx => SphereSummon();
        actions.Player2D.Skill.performed += OnSkill;
        actions.Player2D.Enable();
    }

    void OnDisable()
    {
        actions.Player2D.Move.performed -= OnMove;
        actions.Player2D.Move.canceled -= OnMove;
        actions.Player2D.Jump.performed -= OnJump;
        actions.Player2D.LightAttack.performed -= OnLightAttack;
        //actions.Player2D.HeavyAttack.performed -= OnHeavyAttack;
        actions.Player2D.Dodge.performed -= OnDodge;
        actions.Player2D.Guard.performed -= OnGuardStart;
        actions.Player2D.Guard.canceled -= OnGuardEnd;
        actions.Player2D.Parry.performed -= OnParry;
        actions.Player2D.Gather.performed -= ctx => SphereSummon();
        actions.Player2D.Skill.performed -= OnSkill;
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
        CheckSlope();
    }

    void FixedUpdate()
    {
        if (isDodging) return;

        if (isNearLadder && !isOnLadder && climbInput != 0f)
            EnterLadder();

        if (isOnLadder)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(moveInput * speed, climbInput * climbSpeed);
            return;
        }

        if (jumpQueued)
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = new Vector2(moveInput * speed, jumpForce);
            jumpQueued = false;
            return;
        }

        if (isOnSlope && moveInput != 0)
        {
            Vector2 slopeDir = new(slopeNormal.y, -slopeNormal.x);
            rb.gravityScale = 0f;
            rb.linearVelocity = moveInput * speed * slopeDir;
        }
        else if (isOnSlope && moveInput == 0)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            rb.gravityScale = 1f;
            rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);
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
        moveInput = input.x;
        climbInput = input.y;
        if (moveInput != 0f)
            lastFacingDir = (int)Mathf.Sign(moveInput);
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (isOnLadder)
        {
            ExitLadder();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * 0.6f);
            return;
        }
        if (IsGrounded())
            jumpQueued = true;
    }

    void OnLightAttack(InputAction.CallbackContext ctx)
    {
        if (isDodging || IsGuarding) return;

        if (isAttacking)
        {
            comboInputQueued = true;
            return;
        }

        if (!SpendStamina(lightStaminaCost)) return;

        StartCoroutine(LightAttackCoroutine());
    }

    void OnHeavyAttack(InputAction.CallbackContext ctx)
    {
        if (isDodging || isAttacking || IsGuarding) return;
        if (!SpendStamina(heavyStaminaCost)) return;

        StartCoroutine(HeavyAttackCoroutine());
    }

    void OnDodge(InputAction.CallbackContext ctx)
    {
        if (isDodging || !SpendStamina(dodgeStaminaCost)) return;

        StartCoroutine(DodgeCoroutine());
    }

    void OnGuardStart(InputAction.CallbackContext ctx)
    {
        if (isDodging || isAttacking) return;
        IsGuarding = true;
        StartCoroutine(ParryWindowCoroutine());
    }

    void OnGuardEnd(InputAction.CallbackContext ctx)
    {
        IsGuarding = false;
        isParryActive = false;
    }

    void OnParry(InputAction.CallbackContext ctx)
    {
        if (isDodging || isAttacking || IsGuarding) return;
        if (!SpendStamina(parryStaminaCost)) return;
        StartCoroutine(ParryCoroutine());
    }

    IEnumerator ParryCoroutine()
    {
        IsGuarding = true;
        isParryActive = true;
        yield return new WaitForSeconds(parryWindowDuration);
        isParryActive = false;
        IsGuarding = false;
    }

    // Call this from code or bind to an input action to trigger a standalone slice.
    void OnSlice()
    {
        if (isDodging || IsGuarding || Time.time < nextSliceTime) return;
        if (!SpendStamina(sliceStaminaCost)) return;

        nextSliceTime = Time.time + sliceCooldown;
        DoSlice(sliceForcePower);
    }

    void SphereSummon()
    {
        Instantiate(bloodSpherePrefab, transform.position, Quaternion.identity);
    }

    void OnSkill(InputAction.CallbackContext ctx)
    {
        playerSkill?.UseSkill();
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

                if (hit.collider.TryGetComponent<IDamageable>(out var d))
                    d.TakeDamage(sliceDamage);

                bool isDead = !hit.collider.TryGetComponent<IDamageable>(out var m) || m.IsDead;
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

    IEnumerator LightAttackCoroutine()
    {
        isAttacking = true;
        comboInputQueued = false;

        yield return new WaitForSeconds(0.08f);

        int facingDir = FacingDir();
        Vector2 hitOrigin = HitOrigin(lightRange);
        float multiplier = 1f + comboStep * 0.2f;

        HitEnemies(hitOrigin, lightRange * 0.6f, lightDamage * multiplier, lightKnockback, facingDir);
        DoSlice(lightSliceForcePower);

        yield return new WaitForSeconds(0.12f);

        isAttacking = false;

        if (comboInputQueued && comboStep < maxComboSteps - 1)
        {
            comboStep++;
            comboTimer = comboWindowDuration;
            comboInputQueued = false;

            if (SpendStamina(lightStaminaCost))
                StartCoroutine(LightAttackCoroutine());
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

    // ���� Heavy Attack ��������������������������������������������������������������������������������������������������������������������

    IEnumerator HeavyAttackCoroutine()
    {
        isAttacking = true;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
        yield return new WaitForSeconds(heavyStartupDuration);

        HitEnemies(HitOrigin(heavyRange), heavyRange, heavyDamage, heavyKnockback, FacingDir());
        DoSlice(heavySliceForcePower);

        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
        comboStep = 0;
    }

    // ���� Dodge ����������������������������������������������������������������������������������������������������������������������������������

    IEnumerator DodgeCoroutine()
    {
        isDodging = true;
        isAttacking = false;

        int dodgeDir = moveInput != 0f ? (int)Mathf.Sign(moveInput) : lastFacingDir;

        IsInvincible = true;
        rb.linearVelocity = new Vector2(dodgeDir * dodgeForce, rb.linearVelocity.y);

        yield return new WaitForSeconds(iFrameDuration);

        IsInvincible = false;

        yield return new WaitForSeconds(dodgeDuration - iFrameDuration);

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

    public void TakeDamage(float amount)
    {
        if (IsInvincible) return;

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

        if (CurrentHp <= 0f)
            Die();
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

    bool SpendStamina(float cost)
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

    void HitEnemies(Vector2 origin, float radius, float damage, float knockback, int dir)
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

            if (col.TryGetComponent<Rigidbody2D>(out var targetRb))
                targetRb.AddForce(new Vector2(dir * knockback, 2f), ForceMode2D.Impulse);

            // Spawn blood at the closest surface point facing the attacker
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
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(HitOrigin(lightRange), lightRange * 0.6f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(HitOrigin(heavyRange), heavyRange);

        // Slice ray — direction follows mouse at runtime; show a range circle in editor
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.3f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, sliceRange);
    }
}
