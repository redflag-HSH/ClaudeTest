using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(StateMachine))]
public class MiddleEnemyMeele : PartSliceEnemy, IDamageable, IMeleeEnemy
{
    // ── Stats ─────────────────────────────────────────────────────────────

    [Header("Stats")]
    public float maxHp = 30f;
    public float CurrentHp { get; private set; }
    public override bool IsDead { get; set; }

    // ── Patrol ────────────────────────────────────────────────────────────

    [Header("Patrol")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 4f;
    public float edgeCheckDistance = 0.5f;
    public LayerMask groundLayer;

    [HideInInspector] public Vector2 spawnPoint;
    [HideInInspector] public int patrolDir = 1;

    // ── Detection ─────────────────────────────────────────────────────────

    [Header("Detection")]
    public float chaseRange = 6f;
    public float attackRange = 1.2f;
    public float chaseSpeed = 4f;
    public LayerMask playerLayer;

    // ── Attack Pattern ────────────────────────────────────────────────────

    [System.Serializable]
    public struct AttackPattern
    {
        public string animationName;
        public float range;
        public float lungeForce;
        public float windupTime;
        public float endDelayFactor;
        public float damage;
        public float knockbackForce;
    }

    [Header("Attack")]
    public AttackPattern[] attackPatterns;
    public float attackCooldown = 1f;

    [HideInInspector] public float nextAttackTime;
    [HideInInspector] public bool isLunging;

    // ── Stun / Knockback ──────────────────────────────────────────────────

    float _stunTimer;
    float _knockbackTimer;

    // ── Bloodloss ─────────────────────────────────────────────────────────

    [Header("Bloodloss")]
    public Color bleedColor = new Color(0.45f, 0f, 0f);

    float bleedTimer;
    float bleedDps;
    public bool IsBleeding => bleedTimer > 0f;

    // ── Dodge ─────────────────────────────────────────────────────────────

    [Header("Dodge")]
    public float dodgeSpeed = 7f;
    public float dodgeDuration = 0.25f;
    public float dodgeCooldown = 3f;
    [Range(0f, 1f)] public float dodgeChance = 0.4f;
    public float dodgeTriggerRange = 1.8f;

    [HideInInspector] public float dodgeCooldownTimer;
    [HideInInspector] public bool isDodging;
    [HideInInspector] public bool dodgeInvincible;

    // ── References ────────────────────────────────────────────────────────

    public Rigidbody2D Rb { get; private set; }
    public Transform Player { get; private set; }
    public StateMachine Sm { get; private set; }

    SpriteRenderer _sr;

    // ── Block flag (states check this before acting) ──────────────────────

    public bool IsBlocked { get; private set; }

    // ── Unity ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        Sm = GetComponent<StateMachine>();
        Rb.freezeRotation = true;
        CurrentHp = maxHp;
        spawnPoint = transform.position;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            Player = playerObj.transform;

        Sm.ChangeState(new PatrolState(this));
    }

    void Update()
    {
        if (IsDead) return;

        TickBloodloss();
        if (dodgeCooldownTimer > 0f) dodgeCooldownTimer -= Time.deltaTime;

        IsBlocked = false;

        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.deltaTime;
            Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
            IsBlocked = true;
        }
        if (_stunTimer > 0f)
        {
            _stunTimer -= Time.deltaTime;
            Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
            IsBlocked = true;
        }
    }

    // ── Coroutines ────────────────────────────────────────────────────────

    public IEnumerator MeleeSwing(AttackPattern pattern)
    {
        isLunging = true;

        yield return new WaitForSeconds(pattern.windupTime);

        int dir = Player != null && Player.position.x > transform.position.x ? 1 : -1;
        FaceDirection(dir);
        Rb.linearVelocity = new Vector2(dir * pattern.lungeForce, Rb.linearVelocity.y);

        yield return new WaitForSeconds(0.1f);

        Vector2 hitOrigin = (Vector2)transform.position + Vector2.right * dir * pattern.range * 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitOrigin, pattern.range * 0.6f, playerLayer);

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(pattern.damage, .5f);
            if (col.TryGetComponent<Rigidbody2D>(out var targetRb))
                targetRb.AddForce(new Vector2(dir * pattern.knockbackForce, 2f), ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(0.15f);
        isLunging = false;
    }

    public IEnumerator DodgeCoroutine()
    {
        isDodging = true;
        dodgeInvincible = true;
        dodgeCooldownTimer = dodgeCooldown;

        int dir = (Player != null && Player.position.x > transform.position.x) ? -1 : 1;
        FaceDirection(-dir);

        float elapsed = 0f;
        while (elapsed < dodgeDuration)
        {
            Rb.linearVelocity = new Vector2(dir * dodgeSpeed, Rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Rb.linearVelocity = new Vector2(0f, Rb.linearVelocity.y);
        dodgeInvincible = false;
        isDodging = false;
        Sm.ChangeState(new ChaseState(this));
    }

    // ── Public API ────────────────────────────────────────────────────────

    public bool TriggerDodge()
    {
        if (isDodging || dodgeCooldownTimer > 0f || IsDead) return false;
        if (Random.value > dodgeChance) return false;
        Sm.ChangeState(new DodgeState(this));
        return true;
    }

    public void ApplyKnockback(Vector2 force, float duration)
    {
        Rb.linearVelocity = new Vector2(force.x, Rb.linearVelocity.y);
        _knockbackTimer = duration;
    }

    public void ApplyBloodloss(float dps, float duration)
    {
        if (IsDead) return;
        if (!IsBleeding && _sr != null) _sr.color = bleedColor;
        bleedDps = Mathf.Max(bleedDps, dps);
        bleedTimer = Mathf.Max(bleedTimer, duration);
    }

    // ── IDamageable ───────────────────────────────────────────────────────

    public void TakeDamage(float amount, float stunDuration = 0f)
    {
        if (IsDead) return;
        if (dodgeInvincible) return;
        if (TriggerDodge()) return;

        if (EffectGenerator.Instance != null)
            EffectGenerator.Instance.SpawnBlood(transform.position, Vector2.up);

        CurrentHp -= amount;

        if (stunDuration > 0f)
            _stunTimer = Mathf.Max(_stunTimer, stunDuration);

        if (CurrentHp <= 0f)
            Die();
        else
            StartCoroutine(HitFlash());
    }

    // ── Private helpers ───────────────────────────────────────────────────

    void Die()
    {
        CurrentHp = 0f;
        Sm.ChangeState(new DeadState(this));
    }

    void TickBloodloss()
    {
        if (bleedTimer <= 0f) return;
        bleedTimer -= Time.deltaTime;
        if (IsDead) return;
        CurrentHp -= bleedDps * Time.deltaTime;
        if (bleedTimer <= 0f && _sr != null) _sr.color = Color.white;
        if (CurrentHp <= 0f) Die();
    }

    static readonly WaitForSeconds HitFlashWait = new(0.1f);

    IEnumerator HitFlash()
    {
        if (_sr == null) yield break;
        Color restoreColor = IsBleeding ? bleedColor : Color.white;
        _sr.color = Color.red;
        yield return HitFlashWait;
        _sr.color = restoreColor;
    }

    public void Move(int dir, float speed)
    {
        bool bothLegsCut = cuttedLimbs != null
            && cuttedLimbs.Contains(Limb.Llegs)
            && cuttedLimbs.Contains(Limb.Rlegs);
        if (bothLegsCut) speed *= 0.5f;
        Rb.linearVelocity = new Vector2(dir * speed, Rb.linearVelocity.y);
    }

    public void FaceDirection(int dir)
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    public bool GroundAhead()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.right * patrolDir * edgeCheckDistance;
        return Physics2D.Raycast(origin, Vector2.down, 1f, groundLayer);
    }

    public bool HasLineOfSight(float dist)
    {
        Vector2 dir = Player.position - transform.position;
        return Physics2D.Raycast(transform.position, dir, dist, groundLayer).collider == null;
    }

    // ── IMeleeEnemy ───────────────────────────────────────────────────────

    Transform IMeleeEnemy.Transform    => transform;
    float IMeleeEnemy.AttackRange      => attackRange;
    float IMeleeEnemy.ChaseRange       => chaseRange;
    float IMeleeEnemy.PatrolDistance   => patrolDistance;
    Vector2 IMeleeEnemy.SpawnPoint     => spawnPoint;
    int IMeleeEnemy.PatrolDir          { get => patrolDir; set => patrolDir = value; }
    float IMeleeEnemy.PatrolSpeed      => patrolSpeed;
    float IMeleeEnemy.ChaseSpeed       => chaseSpeed;
    bool IMeleeEnemy.ShouldRunaway     => false;

    BaseState IMeleeEnemy.CreatePatrolState()  => new PatrolState(this);
    BaseState IMeleeEnemy.CreateChaseState()   => new ChaseState(this);
    BaseState IMeleeEnemy.CreateAttackState()  => new AttackState(this);
    BaseState IMeleeEnemy.CreateDeadState()    => new DeadState(this);
    BaseState IMeleeEnemy.CreateRunawayState() => null;

    // ── Gizmos ────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? spawnPoint : (Vector2)transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin + Vector2.left * patrolDistance, origin + Vector2.right * patrolDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
