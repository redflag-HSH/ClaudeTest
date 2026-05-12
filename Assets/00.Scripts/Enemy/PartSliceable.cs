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
        hp -= damage;
        parent.GetComponent<IDamageable>().TakeDamage(damage, stunDuration);
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
