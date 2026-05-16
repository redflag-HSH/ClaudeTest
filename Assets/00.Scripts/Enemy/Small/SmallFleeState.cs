using UnityEngine;

public class SmallFleeState : BaseState
{
    readonly SmallEnemy _e;

    public SmallFleeState(SmallEnemy e) => _e = e;

    public override void Enter() { }

    public override void Perform()
    {
        if (_e.IsDead || _e.Player == null) return;

        bool berserker = _e.PlayerCtrl != null && _e.PlayerCtrl.IsBerserker;
        float dist = Vector2.Distance(_e.transform.position, _e.Player.position);

        if (!berserker && dist > _e.chaseRange)
        {
            StateMachine.ChangeState(new SimplePatrolState(_e));
            return;
        }

        int dir = _e.Player.position.x > _e.transform.position.x ? -1 : 1;
        _e.Move(dir, _e.chaseSpeed);
    }

    public override void Exit() { }
}
