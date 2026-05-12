using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(StateMachine))]
public class SimpleEnemy : EnemySliceable, IMonsterCore, IDamageable
{
    [Header("Stats")]
    public float maxHp = 50f;
    public float CurrentHp { get; private set; }
    public bool IsDead { get; set; }

    [Header("Patrol")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 4f;

    [Header("Detection")]
    public float chaseRange = 6f;
    public float chaseSpeed = 4f;


    [Header("Attack")]
    public IMonsterCore.AttackPattern[] attackPatterns;
    IMonsterCore.AttackPattern[] IMonsterCore.AttackPatterns => attackPatterns;

    public IMonsterCore.AttackPattern PickRandomPattern() =>
        attackPatterns[Random.Range(0, attackPatterns.Length)];

    public float MaxAttackRange()
    {
        float max = 0f;
        foreach (var p in attackPatterns) max = Mathf.Max(max, p.range);
        return max;
    }

    public float attackCooldown = 1.2f;
    public LayerMask playerLayer;
    public LayerMask groundLayer;

    [HideInInspector] public Vector2 spawnPoint;
    [HideInInspector] public int patrolDir = 1;
    [HideInInspector] public float nextAttackTime;

    [Header("Bloodloss")]
    public Color bleedColor = new Color(0.45f, 0f, 0f);

    public Rigidbody2D Rb { get; private set; }
    public Transform Player { get; private set; }
    public StateMachine Sm { get; private set; }
    public bool IsBlocked { get; set; }
    public bool IsBleeding { get; private set; }

    float _bleedTimer;
    float _bleedDps;

    bool _isGrabbed;
    bool _isThrown;
    float _thrownCollisionDamage;
    float _knockbackTimer;

    // IMonsterCore
    bool IMonsterCore.IsBleeding { get => IsBleeding; set { IsBleeding = value; if (!value && sr != null) sr.color = Color.white; } }
    void IMonsterCore.ApplyBloodloss(float dps, float duration) => ApplyBloodloss(dps, duration);
    Transform IMonsterCore.Transform => transform;
    Rigidbody2D IMonsterCore.Rb => Rb;
    Transform IMonsterCore.Player => Player;
    bool IMonsterCore.IsBlocked { get => IsBlocked; set => IsBlocked = value; }
    LayerMask IMonsterCore.GroundLayer => groundLayer;
    float IMonsterCore.ChaseRange => chaseRange;
    float IMonsterCore.PatrolDistance => patrolDistance;
    Vector2 IMonsterCore.SpawnPoint => spawnPoint;
    int IMonsterCore.PatrolDir { get => patrolDir; set => patrolDir = value; }
    float IMonsterCore.PatrolSpeed => patrolSpeed;
    float IMonsterCore.ChaseSpeed => chaseSpeed;

    [Header("Type")]
    [SerializeField] IMonsterCore.Type type;
    IMonsterCore.Type IMonsterCore.MonsterType => type;

    float IMonsterCore.AttackCooldown => attackCooldown;
    float IMonsterCore.NextAttackTime { get => nextAttackTime; set => nextAttackTime = value; }
    LayerMask IMonsterCore.PlayerLayer => playerLayer;
    BaseState IMonsterCore.CreateAttackState() => new SimpleAttackState(this);
    void IMonsterCore.DeathAnimation() => Slice(Vector2.up);

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
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            Player = playerObj.transform;

        Sm.ChangeState(new SimplePatrolState(this));
    }

    void Update()
    {
        if (IsDead) return;
        if (_knockbackTimer > 0f)
        {
            _knockbackTimer -= Time.deltaTime;
            if (_knockbackTimer <= 0f) Sm.enabled = true;
        }
        TickBloodloss();
    }

    public void StartGrab()
    {
        if (IsDead) return;
        _isGrabbed = true;
        Sm.enabled = false;
        Rb.gravityScale = 0f;
        Rb.linearVelocity = Vector2.zero;
    }

    public void Throw(Vector2 velocity, float collisionDamage, float duration, LayerMask enemyLayer)
    {
        if (!_isGrabbed) return;
        _isGrabbed = false;
        _isThrown = true;
        _thrownCollisionDamage = collisionDamage;
        Rb.gravityScale = 1f;
        Rb.linearVelocity = velocity;
        StartCoroutine(ThrownCoroutine(collisionDamage, duration, enemyLayer));
    }

    IEnumerator ThrownCoroutine(float collisionDamage, float duration, LayerMask enemyLayer)
    {
        yield return ThrownStartDelay;
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

        _isThrown = false;
        Sm.enabled = true;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!_isThrown) return;
        if ((groundLayer.value & (1 << col.gameObject.layer)) == 0) return;
        TakeDamage(_thrownCollisionDamage);
        _isThrown = false;
        Sm.enabled = true;
    }

    public void ApplyKnockback(Vector2 force, float duration)
    {
        Sm.enabled = false;
        Rb.linearVelocity = new Vector2(force.x, Rb.linearVelocity.y);
        _knockbackTimer = duration;
    }

    public void ApplyBloodloss(float dps, float duration)
    {
        if (IsDead) return;
        if (!IsBleeding && sr != null) sr.color = bleedColor;
        _bleedDps = Mathf.Max(_bleedDps, dps);
        _bleedTimer = Mathf.Max(_bleedTimer, duration);
        IsBleeding = true;
    }

    void TickBloodloss()
    {
        if (_bleedTimer <= 0f) return;
        _bleedTimer -= Time.deltaTime;
        CurrentHp -= _bleedDps * Time.deltaTime;
        if (_bleedTimer <= 0f)
        {
            IsBleeding = false;
            if (sr != null) sr.color = Color.white;
        }
        if (CurrentHp <= 0f) Die();
    }

    public void TakeDamage(float amount, float stunDuration = 0f)
    {
        if (IsDead) return;
        if (EffectGenerator.Instance != null) EffectGenerator.Instance.SpawnBlood(transform.position, Vector2.up);
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
        Sm.ChangeState(new SimpleDeadState(this));
    }

    static readonly WaitForSeconds FlashWait = new(0.1f);
    static readonly WaitForSeconds ThrownStartDelay = new(0.1f);
    IEnumerator HitFlash()
    {
        if (sr == null) yield break;
        sr.color = Color.red;
        yield return FlashWait;
        sr.color = Color.white;
    }

    public void Move(int dir, float speed)
    {
        Rb.linearVelocity = new Vector2(dir * speed, Rb.linearVelocity.y);
        FaceDirection(dir);
    }

    public void FaceDirection(int dir)
    {
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * dir;
        transform.localScale = s;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 origin = Application.isPlaying ? spawnPoint : (Vector2)transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin + Vector2.left * patrolDistance, origin + Vector2.right * patrolDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        Gizmos.color = Color.red;
    }
}
