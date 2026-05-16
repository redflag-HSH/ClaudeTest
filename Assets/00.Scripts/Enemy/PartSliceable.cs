using UnityEngine;

public class PartSliceable : EnemySliceable, IDamageable
{
    [SerializeField] float hp = 100;
    float _initialHp;

    PartSliceEnemy parent { get => GetComponentInParent<PartSliceEnemy>(); }
    public PartSliceEnemy.Limb limbPart;
    public bool IsDead { get; set; }

    protected override void Awake()
    {
        base.Awake();
        _initialHp = hp;
    }

    public void TakeDamage(float damage, float stunDuration = 0f)
    {
        float before = hp;
        hp -= damage;
        var p = parent;
        if (p != null)
        {
            p.LastHitLimb = this;
            // propagate only what the limb actually had, so limb death and parent death are independent
            float propagated = hp > 0f ? damage : Mathf.Max(0f, before);
            p.ReceiveLimbDamage(propagated, stunDuration);
        }
        if (hp <= 0)
            IsDead = true;
    }

    public void Restore()
    {
        hp = _initialHp;
        IsDead = false;
        ResetSlice();
    }
}
