using UnityEngine;

public class MiddleGuardState : BaseState
{
    readonly MiddleEnemy _e;

    public MiddleGuardState(MiddleEnemy e) { _e = e; }

    public override void Enter() { }
    public override void Perform() { }
    public override void Exit() { }
}
