using UnityEngine;

public class SimpleDeadState : BaseState
{
    readonly IMonsterCore _e;

    public SimpleDeadState(IMonsterCore e) => _e = e;

    public override void Enter()
    {
        _e.Rb.linearVelocity = Vector2.zero;
        _e.DeathAnimation();
    }

    public override void Perform() { }

    public override void Exit() { }
}
