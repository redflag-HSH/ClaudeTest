using UnityEngine;
public interface IMonsterCore
{
    Transform Transform { get; }
    Rigidbody2D Rb { get; }
    Transform Player { get; }
    bool IsBlocked { get; set; }
    bool IsDead { get; set; }
    LayerMask GroundLayer { get; }

    // Effect
    bool IsBleeding { get; set; }
    void ApplyBloodloss(float dps, float duration);

    // Grab / Throw / Knockback
    void StartGrab();
    void Throw(Vector2 velocity, float collisionDamage, float duration, LayerMask enemyLayer);
    void ApplyKnockback(Vector2 force, float duration);

    [System.Serializable]
    public enum Type { small, middle, big, boss }
    Type MonsterType { get; }

    // Movement / detection data
    float ChaseRange { get; }
    float PatrolDistance { get; }
    Vector2 SpawnPoint { get; }
    int PatrolDir { get; set; }
    float PatrolSpeed { get; }
    float ChaseSpeed { get; }

    // Movement helpers
    void Move(int dir, float speed);
    void FaceDirection(int dir);

    // Attack patterns
    [System.Serializable]
    public struct AttackPattern
    {
        public string animationName;
        public float range;
        public float lungeForce;
        public float windupTime;
        public float endDelay;
        public float damage;
        public float knockbackForce;
        public float stunDuration;
        public attackType attacktype;
        public enum attackType { meleeNormal, meleeSpecial, rangeNormal, rangeSpecial }

        public GameObject projectilePrefab;
        public float projectileSpeed;
    }
    AttackPattern[] AttackPatterns { get; }
    AttackPattern PickRandomPattern();
    float MaxAttackRange();

    // Attack config (forwarded from serialized fields)
    float AttackCooldown { get; }
    float NextAttackTime { get; set; }
    LayerMask PlayerLayer { get; }

    // State factories — override per enemy type for unique behaviour
    BaseState CreateAttackState();
    BaseState CreateFleeState() => null;

    // Death visual
    void DeathAnimation();
}
