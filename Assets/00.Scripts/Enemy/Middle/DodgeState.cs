public class DodgeState : BaseState
{
    readonly MiddleEnemyMeele _e;
    public DodgeState(MiddleEnemyMeele e) { _e = e; }

    public override void Enter() { _e.StartCoroutine(_e.DodgeCoroutine()); }
    public override void Perform() { }
    public override void Exit() { }
}
