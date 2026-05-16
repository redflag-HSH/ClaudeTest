using UnityEngine;

public class SimplePatrolState : BaseState
{
    readonly IMonsterCore _e;

    public SimplePatrolState(IMonsterCore e) => _e = e;

    public override void Enter() { }

    public override void Perform()
    {
        if (_e.IsDead || _e.Player == null) return;

        var flee = _e.CreateFleeState();
        if (flee != null) { StateMachine.ChangeState(flee); return; }

        float distToPlayer = Vector2.Distance(_e.Transform.position, _e.Player.position);
        bool hit = Physics2D.Raycast(_e.Transform.position, Vector2.down, distToPlayer, _e.GroundLayer);
        if (distToPlayer <= _e.ChaseRange && !hit)
        {
            StateMachine.ChangeState(new SimpleChaseState(_e));
            return;
        }

        float distFromSpawn = _e.Transform.position.x - _e.SpawnPoint.x;
        if (Mathf.Abs(distFromSpawn) >= _e.PatrolDistance)
            _e.PatrolDir *= -1;

        _e.Move(_e.PatrolDir, _e.PatrolSpeed);
    }

    public override void Exit() { }
}
