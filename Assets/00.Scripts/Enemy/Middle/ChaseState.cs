using UnityEngine;

public class ChaseState : BaseState
{
    readonly IMeleeEnemy _e;
    public ChaseState(IMeleeEnemy e) { _e = e; }

    public override void Enter() { }
    public override void Exit() { }

    public override void Perform()
    {
        if (_e.IsBlocked || _e.Player == null) return;

        if (_e.ShouldRunaway) { StateMachine.ChangeState(_e.CreateRunawayState()); return; }

        float dist = Vector2.Distance(_e.Transform.position, _e.Player.position);

        if (dist <= _e.AttackRange) { StateMachine.ChangeState(_e.CreateAttackState()); return; }
        if (dist > _e.ChaseRange)   { StateMachine.ChangeState(_e.CreatePatrolState()); return; }

        int dir = _e.Player.position.x > _e.Transform.position.x ? 1 : -1;
        _e.Move(dir, _e.ChaseSpeed);
        _e.FaceDirection(dir);
    }
}
