using UnityEngine;

public class AttackState : BaseState
{
    readonly MiddleEnemyMeele _e;
    public AttackState(MiddleEnemyMeele e) { _e = e; }

    public override void Enter() { }
    public override void Exit() { }

    public override void Perform()
    {
        if (_e.IsBlocked) return;

        float dist = _e.Player != null
            ? Vector2.Distance(_e.transform.position, _e.Player.position)
            : float.MaxValue;

        if (dist > _e.attackRange) { StateMachine.ChangeState(new ChaseState(_e)); return; }

        if (!_e.isLunging)
            _e.Rb.linearVelocity = new Vector2(0f, _e.Rb.linearVelocity.y);

        if (Time.time < _e.nextAttackTime) return;
        if (_e.attackPatterns == null || _e.attackPatterns.Length == 0) return;

        int idx = Random.Range(0, _e.attackPatterns.Length);
        MiddleEnemyMeele.AttackPattern chosen = _e.attackPatterns[idx];
        _e.nextAttackTime = Time.time + _e.attackCooldown * chosen.endDelayFactor;
        _e.StartCoroutine(_e.MeleeSwing(chosen));
    }
}
