using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ── Setup ─────────────────────────────────────────────────────────────────────
// Add these actions to your _2DActions Input Action Asset under Player2D map:
//   LightAttack  (Button)  – e.g. Mouse Left / Gamepad West
//   HeavyAttack  (Button)  – e.g. Mouse Right / Gamepad North
//   Dodge        (Button)  – e.g. Shift / Gamepad South
//   Guard        (Button)  – e.g. Q / Gamepad Left Trigger
//
// On the Player GameObject attach:
//   • This script
//   • A child Transform named "HitPoint" in front of the character (hitbox origin)
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerCombat : MonoBehaviour, IDamageable
{
    // ── Stats ─────────────────────────────────────────────────────────────────

    [Header("Health")]
    public float maxHp = 100f;
    public float CurrentHp { get; private set; }

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegen = 20f;       // per second
    public float staminaRegenDelay = 1.2f; // seconds after action before regen starts
    public float CurrentStamina { get; private set; }

    // ── Light Attack ──────────────────────────────────────────────────────────

    [Header("Light Attack")]
    public float lightDamage = 15f;
    public float lightStaminaCost = 20f;
    public float lightRange = 1.0f;
    public float lightKnockback = 3f;
    public float comboWindowDuration = 0.5f;   // time to press again for next combo step
    public int maxComboSteps = 3;

    // ── Heavy Attack ──────────────────────────────────────────────────────────

    [Header("Heavy Attack")]
    public float heavyDamage = 40f;
    public float heavyStaminaCost = 45f;
    public float heavyRange = 1.3f;
    public float heavyKnockback = 7f;
    public float heavyStartupDuration = 0.25f; // wind-up before hit lands

    // ── Dodge ─────────────────────────────────────────────────────────────────

    [Header("Dodge")]
    public float dodgeForce = 10f;
    public float dodgeStaminaCost = 25f;
    public float dodgeDuration = 0.3f;     // total roll time
    public float iFrameDuration = 0.25f;   // invincibility window inside roll

    // ── Guard / Parry ─────────────────────────────────────────────────────────

    [Header("Guard")]
    public float guardDamageReduction = 0.6f;  // 60% damage blocked while guarding
    public float parryWindowDuration = 0.2f;   // perfect parry window at guard start
    public float parryStaminaCost = 15f;        // cost when a hit is parried

    // ── Layer Masks ───────────────────────────────────────────────────────────

    [Header("Layers")]
    public LayerMask enemyLayer;

    // ── References ────────────────────────────────────────────────────────────

    [Header("References")]
    public Transform hitPoint;             // child transform in front of character

    // ── State ─────────────────────────────────────────────────────────────────

    public bool IsInvincible { get; private set; }
    public bool IsGuarding { get; private set; }

    bool isParryActive;
    bool isAttacking;
    bool isDodging;

    int comboStep;
    float comboTimer;
    bool comboInputQueued;

    float staminaRegenTimer;

    Rigidbody2D rb;
    SpriteRenderer sr;
    _2DActions actions;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        CurrentHp = maxHp;
        CurrentStamina = maxStamina;

        actions = new _2DActions();
    }

    void OnEnable()
    {
        actions.Player2D.LightAttack.performed += OnLightAttack;
        actions.Player2D.HeavyAttack.performed += OnHeavyAttack;
        actions.Player2D.Dodge.performed += OnDodge;
        actions.Player2D.Guard.performed += OnGuardStart;
        actions.Player2D.Guard.canceled += OnGuardEnd;
        actions.Player2D.Enable();
    }

    void OnDisable()
    {
        actions.Player2D.LightAttack.performed -= OnLightAttack;
        actions.Player2D.HeavyAttack.performed -= OnHeavyAttack;
        actions.Player2D.Dodge.performed -= OnDodge;
        actions.Player2D.Guard.performed -= OnGuardStart;
        actions.Player2D.Guard.canceled -= OnGuardEnd;
        actions.Player2D.Disable();
    }

    void Update()
    {
        TickComboWindow();
        TickStaminaRegen();
    }

    // ── Input Callbacks ───────────────────────────────────────────────────────

    void OnLightAttack(InputAction.CallbackContext ctx)
    {
        if (isDodging || IsGuarding) return;

        if (isAttacking)
        {
            // queue the next combo step
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
        print("Dodge!");
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

    // ── Light Attack Combo ────────────────────────────────────────────────────

    IEnumerator LightAttackCoroutine()
    {
        isAttacking = true;
        comboInputQueued = false;

        // ── startup ────────────────────────────────────────────────
        yield return new WaitForSeconds(0.08f);

        // ── active frame ───────────────────────────────────────────
        int facingDir = FacingDir();
        Vector2 hitOrigin = hitPoint != null
            ? (Vector2)hitPoint.position
            : (Vector2)transform.position + Vector2.right * facingDir * lightRange * 0.5f;

        HitEnemies(hitOrigin, lightRange * 0.6f, lightDamage, lightKnockback, facingDir);

        // scale damage up slightly with each combo step (combo step 0→1→2)
        float damageMultiplier = 1f + comboStep * 0.2f;
        HitEnemies(hitOrigin, lightRange * 0.6f, lightDamage * damageMultiplier, lightKnockback, facingDir);

        // ── recovery ───────────────────────────────────────────────
        yield return new WaitForSeconds(0.12f);

        isAttacking = false;

        // advance combo or reset
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

    // ── Heavy Attack ──────────────────────────────────────────────────────────

    IEnumerator HeavyAttackCoroutine()
    {
        isAttacking = true;

        // wind-up: slow player briefly
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
        yield return new WaitForSeconds(heavyStartupDuration);

        int facingDir = FacingDir();
        Vector2 hitOrigin = hitPoint != null
            ? (Vector2)hitPoint.position
            : (Vector2)transform.position + Vector2.right * facingDir * heavyRange * 0.5f;

        HitEnemies(hitOrigin, heavyRange, heavyDamage, heavyKnockback, facingDir);

        // screen-shake hook (optional)
        yield return new WaitForSeconds(0.2f);

        isAttacking = false;
        comboStep = 0;
    }

    // ── Dodge / Roll ──────────────────────────────────────────────────────────

    IEnumerator DodgeCoroutine()
    {
        isDodging = true;
        isAttacking = false;

        // direction: use move input from PlayerMovement singleton
        /* float moveInput = PlayerMovement.Instance != null
             ? PlayerMovement.Instance.GetComponent<Rigidbody2D>().linearVelocity.x
             : 0f;
         int dodgeDir = moveInput != 0f ? (int)Mathf.Sign(moveInput) : FacingDir();
 */
        int dodgeDir = FacingDir();
        // i-frames
        IsInvincible = true;
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
                // perfect parry — negate all damage, drain attacker poise (no implementation needed here)
                SpendStamina(parryStaminaCost);
                StartCoroutine(HitFlash(Color.yellow));
                return;
            }

            // normal guard — absorb a portion
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
        // trigger death animation / game over here
        Debug.Log("Player died.");
    }

    // ── Stamina ───────────────────────────────────────────────────────────────

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

    int FacingDir()
    {
        return transform.localScale.x >= 0f ? 1 : -1;
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
        Vector2 origin = hitPoint != null
            ? (Vector2)hitPoint.position
            : (Vector2)transform.position + Vector2.right * FacingDir() * lightRange * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(origin, lightRange * 0.6f);

        Vector2 heavyOrigin = hitPoint != null
            ? (Vector2)hitPoint.position
            : (Vector2)transform.position + Vector2.right * FacingDir() * heavyRange * 0.5f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(heavyOrigin, heavyRange);
    }
}
