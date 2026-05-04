using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MeleeMonsterSpecial : EnemySliceable, IDamageable
{
    // ── State Machine ────────────────────────────────────────────────────────

    public enum State { Patrol, Chase, Attack, Dead }
    public State CurrentState { get; private set; } = State.Patrol;

    // ── Stats ────────────────────────────────────────────────────────────────

    [Header("Stats")]
    public float maxHp = 30f;
    public float CurrentHp { get; private set; }
    public bool IsDead { get; set; }

    // ── Patrol ───────────────────────────────────────────────────────────────

    [Header("Patrol")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 4f;   // half-width from spawn
    public float edgeCheckDistance = 0.5f;
    public LayerMask groundLayer;

    private Vector2 spawnPoint;
    private int patrolDir = 1;          // 1 = right, -1 = left

    // ── Detection ────────────────────────────────────────────────────────────

    [Header("Detection")]
    public float chaseRange = 6f;
    public float attackRange = 1.2f;
    public float chaseSpeed = 4f;
    public LayerMask playerLayer;

    // ── Attack ───────────────────────────────────────────────────────────────

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackCooldown = 1f;
    public float lungeForce = 5f;       // burst of speed toward player on swing
    public float knockbackForce = 4f;   // impulse applied to hit targets

    private float nextAttackTime;
    private bool isLunging;

    // ── Bloodloss ─────────────────────────────────────────────────────────────

    [Header("Bloodloss")]
    public Color bleedColor = new Color(0.45f, 0f, 0f);

    float bleedTimer;
    float bleedDps;
    public bool IsBleeding => bleedTimer > 0f;

    public void ApplyBloodloss(float dps, float duration)
    {
        if (IsDead) return;
        if (!IsBleeding && sr != null) sr.color = bleedColor;
        bleedDps = Mathf.Max(bleedDps, dps);
        bleedTimer = Mathf.Max(bleedTimer, duration);
    }

    void TickBloodloss()
    {
        if (bleedTimer <= 0f) return;
        bleedTimer -= Time.deltaTime;
        BleedTick(bleedDps * Time.deltaTime);
        if (bleedTimer <= 0f && sr != null) sr.color = Color.white;
    }

    void BleedTick(float amount)
    {
        if (CurrentState == State.Dead) return;
        CurrentHp -= amount;
        if (CurrentHp <= 0f) Die();
    }

    // ── References ───────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private Transform player;

    // ── Unity ────────────────────────────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        CurrentHp = maxHp;
        spawnPoint = transform.position;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (CurrentState == State.Dead) return;

        TickBloodloss();
        UpdateState();
        RunState();
    }

    // ── State Machine ────────────────────────────────────────────────────────

    void UpdateState()
    {
        float distToPlayer = player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

        if (distToPlayer <= attackRange)
            ChangeState(State.Attack);
        else if (distToPlayer <= chaseRange)
            ChangeState(State.Chase);
        else
            ChangeState(State.Patrol);
    }

    void RunState()
    {
        switch (CurrentState)
        {
            case State.Patrol: PatrolUpdate(); break;
            case State.Chase: ChaseUpdate(); break;
            case State.Attack: AttackUpdate(); break;
        }
    }

    void ChangeState(State next)
    {
        if (CurrentState == next) return;
        CurrentState = next;
    }

    // ── Patrol ───────────────────────────────────────────────────────────────

    void PatrolUpdate()
    {
        float leftEdge = spawnPoint.x - patrolDistance;
        float rightEdge = spawnPoint.x + patrolDistance;

        // flip at edges
        if (transform.position.x >= rightEdge)
            patrolDir = -1;
        else if (transform.position.x <= leftEdge)
            patrolDir = 1;

        // stop at ledge
        if (!GroundAhead())
            patrolDir = -patrolDir;

        Move(patrolDir, patrolSpeed);
        FaceDirection(patrolDir);
    }

    bool GroundAhead()
    {
        Vector2 checkOrigin = (Vector2)transform.position + Vector2.right * patrolDir * edgeCheckDistance;
        return Physics2D.Raycast(checkOrigin, Vector2.down, 1f, groundLayer);
    }

    // ── Chase ────────────────────────────────────────────────────────────────

    void ChaseUpdate()
    {
        if (player == null) return;
        int dir = player.position.x > transform.position.x ? 1 : -1;
        Move(dir, chaseSpeed);
        FaceDirection(dir);
    }

    // ── Attack ───────────────────────────────────────────────────────────────

    void AttackUpdate()
    {
        if (!isLunging)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        StartCoroutine(MeleeSwing());
    }

    IEnumerator MeleeSwing()
    {
        isLunging = true;

        // lunge toward player
        int dir = player != null && player.position.x > transform.position.x ? 1 : -1;
        FaceDirection(dir);
        rb.linearVelocity = new Vector2(dir * lungeForce, rb.linearVelocity.y);

        yield return new WaitForSeconds(0.1f);

        // overlap circle hitbox in front of the monster
        Vector2 hitOrigin = (Vector2)transform.position + Vector2.right * dir * attackRange * 0.5f;
        Collider2D[] hits = Physics2D.OverlapCircleAll(hitOrigin, attackRange * 0.6f, playerLayer);

        foreach (var col in hits)
        {
            if (col.TryGetComponent<IDamageable>(out var target))
            {
                if (player == col.transform)
                    player.gameObject.GetComponent<PlayerControl>().TakeSpecialDamage(attackDamage);
                else
                    target.TakeDamage(attackDamage);
            }
            if (col.TryGetComponent<Rigidbody2D>(out var targetRb))
                targetRb.AddForce(new Vector2(dir * knockbackForce, 2f), ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(0.15f);
        isLunging = false;
    }

    // ── Hit / IDamageable ────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (CurrentState == State.Dead) return;

        CurrentHp -= amount;

        if (CurrentHp <= 0f)
            Die();
        else
            StartCoroutine(HitFlash());
    }

    void Die()
    {
        CurrentHp = 0f;
        IsDead = true;
        ChangeState(State.Dead);
        rb.linearVelocity = Vector2.zero;
        // destruction is handled by EnemySliceable.Slice() when the player slices
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

    // ── Helpers ───────────────────────────────────────────────────────────────

    void Move(int dir, float speed)
    {
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    }

    void FaceDirection(int dir)
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    // ── Gizmos ───────────────────────────────────────────────────────────────

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
