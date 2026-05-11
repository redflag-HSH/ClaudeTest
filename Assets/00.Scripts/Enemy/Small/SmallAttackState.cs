using UnityEngine;

public class SmallAttackState : BaseState
{
    readonly SmallMonsterMelee _e;
    public SmallAttackState(SmallMonsterMelee e) { _e = e; }

    public override void Enter() { }
    public override void Exit() { }

    public override void Perform()
    {
        if (_e.IsBlocked) return;

        if (_e.PlayerCtrl != null && _e.PlayerCtrl.IsBerserker)
        {
            StateMachine.ChangeState(new SmallRunawayState(_e));
            return;
        }

        float dist = _e.Player != null
            ? Vector2.Distance(_e.transform.position, _e.Player.position)
            : float.MaxValue;

        if (dist > _e.attackRange) { StateMachine.ChangeState(new ChaseState(_e)); return; }

        if (!_e.isLunging)
            _e.Rb.linearVelocity = new Vector2(0f, _e.Rb.linearVelocity.y);

        if (Time.time < _e.nextAttackTime) return;
        _e.nextAttackTime = Time.time + _e.attackCooldown;
        _e.StartCoroutine(_e.MeleeSwing());
    }
}
