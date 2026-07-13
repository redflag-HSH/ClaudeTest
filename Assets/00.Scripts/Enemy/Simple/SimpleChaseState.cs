using UnityEngine;

public class SimpleChaseState : BaseState
{
    readonly IMonsterCore _e;

    public SimpleChaseState(IMonsterCore e) => _e = e;

    public override void Enter() { _e.IsAwareOfPlayer = true; }

    public override void Perform()
    {
        if (_e.IsDead || _e.Player == null) return;

        var flee = _e.CreateFleeState();
        if (flee != null) { StateMachine.ChangeState(flee); return; }

        float dist = Vector2.Distance(_e.Transform.position, _e.Player.position);

        if (dist <= _e.MaxAttackRange())
        {
            StateMachine.ChangeState(_e.CreateAttackState());
            return;
        }

        if (dist > _e.ChaseRange * 1.2f)
        {
            StateMachine.ChangeState(new SimplePatrolState(_e));
            return;
        }

        Vector2 toPlayer = (Vector2)_e.Player.position - (Vector2)_e.Transform.position;
        _e.IsBlocked = _e.GroundLayer.value != 0 &&
            Physics2D.Raycast(_e.Transform.position, toPlayer.normalized, toPlayer.magnitude, _e.GroundLayer);

        if (_e.IsBlocked) return;

        int dir = _e.Player.position.x > _e.Transform.position.x ? 1 : -1;
        _e.Move(dir, _e.ChaseSpeed);
    }

    public override void Exit() { }
}
