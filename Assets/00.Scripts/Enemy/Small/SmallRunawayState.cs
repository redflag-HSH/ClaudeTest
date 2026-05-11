using UnityEngine;

public class SmallRunawayState : BaseState
{
    readonly SmallMonsterMelee _e;
    public SmallRunawayState(SmallMonsterMelee e) { _e = e; }

    public override void Enter() { }
    public override void Exit() { }

    public override void Perform()
    {
        if (_e.IsBlocked || _e.Player == null) return;

        if (_e.PlayerCtrl == null || !_e.PlayerCtrl.IsBerserker)
        {
            StateMachine.ChangeState(new PatrolState(_e));
            return;
        }

        int dir = _e.transform.position.x > _e.Player.position.x ? 1 : -1;
        _e.Move(dir, _e.runawaySpeed);
        _e.FaceDirection(dir);
    }
}
