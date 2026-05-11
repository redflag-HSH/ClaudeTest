using UnityEngine;

public class PatrolState : BaseState
{
    readonly IMeleeEnemy _e;
    public PatrolState(IMeleeEnemy e) { _e = e; }

    public override void Enter() { }
    public override void Exit() { }

    public override void Perform()
    {
        if (_e.IsBlocked) return;

        if (_e.ShouldRunaway) { StateMachine.ChangeState(_e.CreateRunawayState()); return; }

        float dist = _e.Player != null
            ? Vector2.Distance(_e.Transform.position, _e.Player.position)
            : float.MaxValue;

        if (dist <= _e.AttackRange) { StateMachine.ChangeState(_e.CreateAttackState()); return; }
        if (dist <= _e.ChaseRange && _e.HasLineOfSight(dist)) { StateMachine.ChangeState(_e.CreateChaseState()); return; }

        float left  = _e.SpawnPoint.x - _e.PatrolDistance;
        float right = _e.SpawnPoint.x + _e.PatrolDistance;
        if (_e.Transform.position.x >= right) _e.PatrolDir = -1;
        else if (_e.Transform.position.x <= left) _e.PatrolDir = 1;
        if (!_e.GroundAhead()) _e.PatrolDir = -_e.PatrolDir;

        _e.Move(_e.PatrolDir, _e.PatrolSpeed);
        _e.FaceDirection(_e.PatrolDir);
    }
}
