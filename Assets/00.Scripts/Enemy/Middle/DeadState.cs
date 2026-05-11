public class DeadState : BaseState
{
    readonly IMeleeEnemy _e;
    public DeadState(IMeleeEnemy e) { _e = e; }

    public override void Enter()
    {
        _e.IsDead = true;
        _e.Rb.linearVelocity = UnityEngine.Vector2.zero;
    }
    public override void Perform() { }
    public override void Exit() { }
}
