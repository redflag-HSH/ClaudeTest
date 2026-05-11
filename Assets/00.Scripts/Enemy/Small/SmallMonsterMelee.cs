using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(StateMachine))]
public class SmallMonsterMelee : EnemySliceable, IDamageable, IMeleeEnemy
{
    // ── Stats ─────────────────────────────────────────────────────────────

    [Header("Stats")]
    public float maxHp = 30f;
    public float CurrentHp { get; private set; }
    public bool IsDead { get; set; }

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
    public float runawaySpeed = 5f;
    public LayerMask playerLayer;

    // ── Attack ────────────────────────────────────────────────────────────

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    public float lungeForce = 5f;
    public float knockbackForce = 4f;

    [HideInInspector] public float nextAttackTime;
    [HideInInspector] public bool isLunging;

    // ── Grab / Throw ──────────────────────────────────────────────────────

    public bool IsGrabbed { get; private set; }
    public bool IsThrown { get; private set; }
    float _thrownCollisionDamage;

    // ── Stun / Knockback ──────────────────────────────────────────────────

    float _stunTimer;
    float _knockbackTimer;
    public bool IsStunned => _stunTimer > 0f;

    // ── Bloodloss ─────────────────────────────────────────────────────────

    [Header("Bloodloss")]
    public Color bleedColor = new Color(0.45f, 0f, 0f);

    float bleedTimer;
    float bleedDps;
    public bool IsBleeding => bleedTimer > 0f;

    // ── References ────────────────────────────────────────────────────────

    public Rigidbody2D Rb { get; private set; }
    public Transform Player { get; private set; }
    public PlayerControl PlayerCtrl { get; private set; }
    public StateMachine Sm { get; private set; }

    // ── Block flag (states check this before acting) ──────────────────────

    public bool IsBlocked { get; private set; }

    // ── Unity ─────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        Rb = GetComponent<Rigidbody2D>();
        Sm = GetComponent<StateMachine>();
        Rb.freezeRotation = true;
        CurrentHp = maxHp;
        spawnPoint = transform.position;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Player = playerObj.transform;
            PlayerCtrl = playerObj.GetComponent<PlayerControl>();
        }

        Sm.ChangeState(new PatrolState(this));
    }

    void Update()
    {
        if (IsDead) return;

        TickBloodloss();

        IsBlocked = IsGrabbed || IsThrown;

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

    public IEnumerator MeleeSwing()
    {
        isLunging = true;

        int dir = Player != null && Player.position.x > transform.position.x ? 1 : -1;
        FaceDirection(dir);
        Rb.linearVelocity = new Vector2(dir * lungeForce, Rb.linearVelocity.y);

        yield return new WaitForSeconds(0.1f);

        Vector2 hitOrigin = (Vector2)transform.position + Vector2.right * dir * attackRange * 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitOrigin, attackRange * 0.6f, playerLayer);

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(attackDamage, .5f);
            if (col.TryGetComponent<Rigidbody2D>(out var targetRb))
                targetRb.AddForce(new Vector2(dir * knockbackForce, 2f), ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(0.15f);
        isLunging = false;
    }

    // ── Grab / Throw ──────────────────────────────────────────────────────

    public void StartGrab()
    {
        if (IsDead) return;
        IsGrabbed = true;
        Rb.gravityScale = 0f;
        Rb.linearVelocity = Vector2.zero;
    }

    public void Throw(Vector2 velocity, float collisionDamage, float duration, LayerMask enemyLayer)
    {
        if (!IsGrabbed) return;
        IsGrabbed = false;
        IsThrown = true;
        _thrownCollisionDamage = collisionDamage;
        Rb.gravityScale = 1f;
        Rb.linearVelocity = velocity;
        StartCoroutine(ThrownCoroutine(collisionDamage, duration, enemyLayer));
    }

    IEnumerator ThrownCoroutine(float collisionDamage, float duration, LayerMask enemyLayer)
    {
        yield return new WaitForSeconds(0.1f);
        float elapsed = 0.1f;
        bool impacted = false;

        while (elapsed < duration && !impacted)
        {
            yield return null;
            elapsed += Time.deltaTime;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.5f, enemyLayer);
            foreach (var col in hits)
            {
                if (col.gameObject == gameObject) continue;
                if (col.TryGetComponent<IDamageable>(out var target))
                    target.TakeDamage(collisionDamage);
                TakeDamage(collisionDamage);
                impacted = true;
                break;
            }
        }

        IsThrown = false;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!IsThrown) return;
        if ((groundLayer.value & (1 << col.gameObject.layer)) == 0) return;
        TakeDamage(_thrownCollisionDamage);
        IsThrown = false;
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void ApplyKnockback(Vector2 force, float duration)
    {
        Rb.linearVelocity = new Vector2(force.x, Rb.linearVelocity.y);
        _knockbackTimer = duration;
    }

    public void ApplyBloodloss(float dps, float duration)
    {
        if (IsDead) return;
        if (!IsBleeding && sr != null) sr.color = bleedColor;
        bleedDps = Mathf.Max(bleedDps, dps);
        bleedTimer = Mathf.Max(bleedTimer, duration);
    }

    // ── IDamageable ───────────────────────────────────────────────────────

    public void TakeDamage(float amount, float stunDuration = 0f)
    {
        if (IsDead) return;

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
        if (bleedTimer <= 0f && sr != null) sr.color = Color.white;
        if (CurrentHp <= 0f) Die();
    }

    static readonly WaitForSeconds HitFlashWait = new(0.1f);

    IEnumerator HitFlash()
    {
        if (sr == null) yield break;
        Color restoreColor = IsBleeding ? bleedColor : Color.white;
        sr.color = Color.red;
        yield return HitFlashWait;
        sr.color = restoreColor;
    }

    public void Move(int dir, float speed)
    {
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
    bool IMeleeEnemy.ShouldRunaway     => PlayerCtrl != null && PlayerCtrl.IsBerserker;

    BaseState IMeleeEnemy.CreatePatrolState()  => new PatrolState(this);
    BaseState IMeleeEnemy.CreateChaseState()   => new ChaseState(this);
    BaseState IMeleeEnemy.CreateAttackState()  => new SmallAttackState(this);
    BaseState IMeleeEnemy.CreateDeadState()    => new DeadState(this);
    BaseState IMeleeEnemy.CreateRunawayState() => new SmallRunawayState(this);

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
