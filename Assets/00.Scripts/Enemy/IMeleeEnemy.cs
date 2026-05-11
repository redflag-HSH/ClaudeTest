using UnityEngine;

public interface IMeleeEnemy
{
    // Core refs
    Transform Transform    { get; }
    Rigidbody2D Rb         { get; }
    Transform Player       { get; }
    bool IsBlocked         { get; }
    bool IsDead            { get; set; }

    // Movement / detection data
    float AttackRange      { get; }
    float ChaseRange       { get; }
    float PatrolDistance   { get; }
    Vector2 SpawnPoint     { get; }
    int PatrolDir          { get; set; }
    float PatrolSpeed      { get; }
    float ChaseSpeed       { get; }

    // Enemy-specific behaviour hook (berserker flee, etc.)
    bool ShouldRunaway { get; }

    // Shared movement helpers
    void Move(int dir, float speed);
    void FaceDirection(int dir);
    bool GroundAhead();
    bool HasLineOfSight(float dist);

    // State factories — each enemy creates the right concrete state
    BaseState CreatePatrolState();
    BaseState CreateChaseState();
    BaseState CreateAttackState();
    BaseState CreateDeadState();
    BaseState CreateRunawayState();
}
