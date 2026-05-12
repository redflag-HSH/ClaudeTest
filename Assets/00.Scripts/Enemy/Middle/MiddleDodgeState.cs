using UnityEngine;

public class MiddleDodgeState : BaseState
{
    readonly MiddleEnemy _e;
    float _timer;
    readonly float _duration;

    public MiddleDodgeState(MiddleEnemy e, float duration = 0.4f)
    {
        _e = e;
        _duration = duration;
    }

    public override void Enter()
    {
        _e.IsDodging = true;
        _timer = _duration;

        if (_e.Player != null)
        {
            int dir = _e.Player.position.x > _e.transform.position.x ? -1 : 1;
            _e.Rb.linearVelocity = new Vector2(dir * _e.chaseSpeed * 2f, _e.Rb.linearVelocity.y);
            _e.FaceDirection(-dir);
        }
    }

    public override void Perform()
    {
        if (_e.IsDead) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
            StateMachine.ChangeState(new SimpleChaseState(_e));
    }

    public override void Exit()
    {
        _e.IsDodging = false;
    }
}
