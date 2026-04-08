using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControl : MonoBehaviour, IDamageable
{
    public static PlayerControl Instance { get; private set; }

    // ── Movement ──────────────────────────────────────────────────────────────

    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 10f;
    public Transform groundCheck;
    public LayerMask groundLayer;

    // ── Health ────────────────────────────────────────────────────────────────

    [Header("Health")]
    public float maxHp = 100f;
    public float CurrentHp { get; private set; }

    // ── Stamina ───────────────────────────────────────────────────────────────

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegen = 20f;
    public float staminaRegenDelay = 1.2f;
    public float CurrentStamina { get; private set; }

    // ── Light Attack ──────────────────────────────────────────────────────────

    [Header("Light Attack")]
    public float lightDamage = 15f;
    public float lightStaminaCost = 20f;
    public float lightRange = 1.0f;
    public float lightKnockback = 3f;
    public float comboWindowDuration = 0.5f;
    public int maxComboSteps = 3;

    // ── Heavy Attack ──────────────────────────────────────────────────────────

    [Header("Heavy Attack")]
    public float heavyDamage = 40f;
    public float heavyStaminaCost = 45f;
    public float heavyRange = 1.3f;
    public float heavyKnockback = 7f;
    public float heavyStartupDuration = 0.25f;

    // ── Dodge ─────────────────────────────────────────────────────────────────

    [Header("Dodge")]
    public float dodgeForce = 10f;
    public float dodgeStaminaCost = 25f;
    public float dodgeDuration = 0.3f;
    public float iFrameDuration = 0.25f;

    // ── Guard / Parry ─────────────────────────────────────────────────────────

    [Header("Guard")]
    public float guardDamageReduction = 0.6f;
    public float parryWindowDuration = 0.2f;
    public float parryStaminaCost = 15f;

    // ── Layers / References ───────────────────────────────────────────────────

    [Header("Layers")]
    public LayerMask enemyLayer;

    [Header("References")]
    public Transform hitPoint;

    // ── State ─────────────────────────────────────────────────────────────────

    public bool IsInvincible { get; private set; }
    public bool IsGuarding   { get; private set; }

    bool isParryActive;
    bool isAttacking;
    bool isDodging;

    int   comboStep;
    float comboTimer;
    bool  comboInputQueued;

    float staminaRegenTimer;
    float moveInput;
    bool  jumpQueued;

    Rigidbody2D rb;
    SpriteRenderer sr;
    _2DActions actions;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        sr = GetComponent<SpriteRenderer>();
        CurrentHp      = maxHp;
        CurrentStamina = maxStamina;

        actions = new _2DActions();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnEnable()
    {
        actions.Player2D.Move.performed        += OnMove;
        actions.Player2D.Move.canceled         += OnMove;
        actions.Player2D.Jump.performed        += OnJump;
        actions.Player2D.LightAttack.performed += OnLightAttack;
        actions.Player2D.HeavyAttack.performed += OnHeavyAttack;
        actions.Player2D.Dodge.performed       += OnDodge;
        actions.Player2D.Guard.performed       += OnGuardStart;
        actions.Player2D.Guard.canceled        += OnGuardEnd;
        actions.Player2D.Enable();
    }

    void OnDisable()
    {
        actions.Player2D.Move.performed        -= OnMove;
        actions.Player2D.Move.canceled         -= OnMove;
        actions.Player2D.Jump.performed        -= OnJump;
        actions.Player2D.LightAttack.performed -= OnLightAttack;
        actions.Player2D.HeavyAttack.performed -= OnHeavyAttack;
        actions.Player2D.Dodge.performed       -= OnDodge;
        actions.Player2D.Guard.performed       -= OnGuardStart;
        actions.Player2D.Guard.canceled        -= OnGuardEnd;
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
    }

    void FixedUpdate()
    {
        if (isDodging) return;

        rb.linearVelocity = new Vector2(moveInput * speed, rb.linearVelocity.y);

        if (jumpQueued)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpQueued = false;
        }
        else if (IsGrounded() && moveInput != 0)
        {
            RaycastHit2D hit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.3f, groundLayer);
            if (hit.collider != null)
            {
                Vector2 slopeRight = new(hit.normal.y, -hit.normal.x);
                Vector2 slopeVel   = moveInput * speed * slopeRight;
                if (slopeVel.y < 0)
                    rb.linearVelocity = slopeVel;
            }
        }
    }

    // ── Input Callbacks ───────────────────────────────────────────────────────

    void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>().x;
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
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
        IsGuarding    = false;
        isParryActive = false;
    }

    // ── Light Attack Combo ────────────────────────────────────────────────────

    IEnumerator LightAttackCoroutine()
    {
        isAttacking      = true;
        comboInputQueued = false;

        yield return new WaitForSeconds(0.08f);

        int    facingDir  = FacingDir();
        Vector2 hitOrigin = HitOrigin(lightRange);
        float  multiplier = 1f + comboStep * 0.2f;

        HitEnemies(hitOrigin, lightRange * 0.6f, lightDamage * multiplier, lightKnockback, facingDir);

        yield return new WaitForSeconds(0.12f);

        isAttacking = false;

        if (comboInputQueued && comboStep < maxComboSteps - 1)
        {
            comboStep++;
            comboTimer       = comboWindowDuration;
            comboInputQueued = false;

            if (SpendStamina(lightStaminaCost))
                StartCoroutine(LightAttackCoroutine());
        }
        else
        {
            comboStep        = 0;
            comboTimer       = 0f;
            comboInputQueued = false;
        }
    }

    void TickComboWindow()
    {
        if (comboStep == 0) return;

        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f)
        {
            comboStep        = 0;
            comboInputQueued = false;
        }
    }

    // ── Heavy Attack ──────────────────────────────────────────────────────────

    IEnumerator HeavyAttackCoroutine()
    {
        isAttacking = true;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
        yield return new WaitForSeconds(heavyStartupDuration);

        HitEnemies(HitOrigin(heavyRange), heavyRange, heavyDamage, heavyKnockback, FacingDir());

        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
        comboStep   = 0;
    }

    // ── Dodge ─────────────────────────────────────────────────────────────────

    IEnumerator DodgeCoroutine()
    {
        isDodging   = true;
        isAttacking = false;

        int dodgeDir = moveInput != 0f ? (int)Mathf.Sign(moveInput) : FacingDir();

        IsInvincible      = true;
        rb.linearVelocity = new Vector2(dodgeDir * dodgeForce, rb.linearVelocity.y);

        yield return new WaitForSeconds(iFrameDuration);

        IsInvincible = false;

        yield return new WaitForSeconds(dodgeDuration - iFrameDuration);

        isDodging = false;
    }

    // ── Guard / Parry ─────────────────────────────────────────────────────────

    IEnumerator ParryWindowCoroutine()
    {
        isParryActive = true;
        yield return new WaitForSeconds(parryWindowDuration);
        isParryActive = false;
    }

    // ── IDamageable ───────────────────────────────────────────────────────────

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

        if (CurrentHp <= 0f)
            Die();
    }

    void Die()
    {
        CurrentHp = 0f;
        Debug.Log("Player died.");
    }

    // ── Stamina ───────────────────────────────────────────────────────────────

    bool SpendStamina(float cost)
    {
        if (CurrentStamina < cost) return false;
        CurrentStamina    -= cost;
        staminaRegenTimer  = staminaRegenDelay;
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    void HitEnemies(Vector2 origin, float radius, float damage, float knockback, int dir)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, enemyLayer);
        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(damage);

            if (col.TryGetComponent<Rigidbody2D>(out var targetRb))
                targetRb.AddForce(new Vector2(dir * knockback, 2f), ForceMode2D.Impulse);
        }
    }

    Vector2 HitOrigin(float range)
    {
        return hitPoint != null
            ? (Vector2)hitPoint.position
            : (Vector2)transform.position + Vector2.right * FacingDir() * range * 0.5f;
    }

    int FacingDir()
    {
        return transform.localScale.x >= 0f ? 1 : -1;
    }

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

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(HitOrigin(lightRange), lightRange * 0.6f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(HitOrigin(heavyRange), heavyRange);
    }
}
