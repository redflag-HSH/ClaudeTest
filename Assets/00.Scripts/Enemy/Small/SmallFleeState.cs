using UnityEngine;

public class SmallFleeState : BaseState
{
    readonly SmallEnemy _e;

    public SmallFleeState(SmallEnemy e) => _e = e;

    public override void Enter() { }

    public override void Perform()
    {
        if (_e.IsDead || _e.Player == null) return;

        if (_e.PlayerCtrl == null || !_e.PlayerCtrl.IsBerserker)
        {
            StateMachine.ChangeState(new SimpleChaseState(_e));
            return;
        }

        int dir = _e.Player.position.x > _e.transform.position.x ? -1 : 1;
        _e.Move(dir, _e.chaseSpeed);
    }

    public override void Exit() { }
}
