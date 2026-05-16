using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(StateMachine))]
public class MiddleEnemy : PartSliceEnemy, IMonsterCore, IDamageable
{
    [Header("Stats")]
    public float maxHp = 150f;
    public float CurrentHp { get; private set; }

    [Header("Patrol")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 4f;

    [Header("Detection")]
    public float chaseRange = 8f;
    public float chaseSpeed = 3f;

    [Header("Ground")]
    public LayerMask groundLayer;
    [SerializeField] float groundCheckDist = 0.05f;
    [SerializeField] float groundOffset = 0.5f;

    [Header("Dodge")]
    public float dodgeChance = 0.3f;

    [Header("Attack")]
    public IMonsterCore.AttackPattern[] attackPatterns;
    IMonsterCore.AttackPattern[] IMonsterCore.AttackPatterns => attackPatterns;

    public float attackCooldown = 1.5f;
    public LayerMask playerLayer;

    [HideInInspector] public Vector2 spawnPoint;
    [HideInInspector] public int patrolDir = 1;
    [HideInInspector] public float nextAttackTime;

    public Rigidbody2D Rb { get; private set; }
    public Transform Player { get; private set; }
    public PlayerControl PlayerCtrl { get; private set; }
    public StateMachine Sm { get; private set; }
    public bool IsBlocked { get; set; }
    public bool IsBleeding { get; private set; }
    public bool IsDodging { get; set; }

    float _bleedTimer;
    float _bleedDps;
    float _knockbackTimer;

    bool IMonsterCore.IsBleeding { get => IsBleeding; set => IsBleeding = value; }
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
    BaseState IMonsterCore.CreateAttackState() => new MiddleAttackState(this);
    void IMonsterCore.DeathAnimation() => gameObject.SetActive(false);

    void IMonsterCore.StartGrab() { }
    void IMonsterCore.Throw(Vector2 velocity, float collisionDamage, float duration, LayerMask enemyLayer) { }

    void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Sm = GetComponent<StateMachine>();
        Rb.freezeRotation = true;
        CurrentHp = maxHp;
        spawnPoint = transform.position;

        foreach (var limb in GetComponentsInChildren<PartSliceable>())
            limb.destroyOnSlice = false;
    }

    void Start()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Player = playerObj.transform;
            PlayerCtrl = playerObj.GetComponent<PlayerControl>();
        }

        Sm.ChangeState(new SimplePatrolState(this));
    }

    void FixedUpdate()
    {
        if (IsDead) return;
        if (Rb.linearVelocity.y > 0f) return;
        var hit = Physics2D.Raycast(transform.position, Vector2.down, groundOffset + groundCheckDist, groundLayer);
        if (hit)
        {
            Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, 0f);
            transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);
        }
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

    public IMonsterCore.AttackPattern PickRandomPattern()
    {
        var normals = System.Array.FindAll(attackPatterns, p =>
            p.attacktype == IMonsterCore.AttackPattern.attackType.meleeNormal ||
            p.attacktype == IMonsterCore.AttackPattern.attackType.rangeNormal);
        if (normals.Length > 0)
            return normals[Random.Range(0, normals.Length)];
        return attackPatterns[Random.Range(0, attackPatterns.Length)];
    }

    public IMonsterCore.AttackPattern PickSpecialPattern()
    {
        var specials = System.Array.FindAll(attackPatterns, p =>
            p.attacktype == IMonsterCore.AttackPattern.attackType.meleeSpecial ||
            p.attacktype == IMonsterCore.AttackPattern.attackType.rangeSpecial);
        if (specials.Length > 0)
            return specials[Random.Range(0, specials.Length)];
        return attackPatterns[Random.Range(0, attackPatterns.Length)];
    }

    public float MaxAttackRange()
    {
        float max = 0f;
        foreach (var p in attackPatterns) max = Mathf.Max(max, p.range);
        return max;
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
            IsBleeding = false;
        if (CurrentHp <= 0f) Die();
    }

    public void TakeDamage(float amount, float stunDuration = 0f)
    {
        if (IsDead) return;

        if (!IsDodging && Random.value < dodgeChance)
        {
            Sm.ChangeState(new MiddleDodgeState(this));
            return;
        }

        if (EffectGenerator.Instance != null) EffectGenerator.Instance.SpawnBlood(transform.position, Vector2.up);
        CurrentHp -= amount;
        if (CurrentHp <= 0f)
            Die();
    }

    void Die()
    {
        CurrentHp = 0f;
        IsDead = true;
        Sm.ChangeState(new SimpleDeadState(this));
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

        Vector2 rayStart = transform.position;
        Vector2 rayEnd = rayStart + Vector2.down * (groundOffset + groundCheckDist);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(rayStart, rayEnd);
        Gizmos.DrawWireSphere(rayEnd, 0.05f);
    }
}
