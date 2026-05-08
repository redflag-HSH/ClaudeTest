using UnityEngine;

public class PartSliceable : EnemySliceable, IDamageable
{
    [SerializeField] float hp = 100;
    PartSliceEnemy parent { get => GetComponentInParent<PartSliceEnemy>(); }
    public PartSliceEnemy.Limb limbPart;
    public bool IsDead { get; set; }
    public void TakeDamage(float damage, float stunDuration = 0f)
    {
        hp -= damage;
        if (hp <= 0)
        {
            IsDead = true;
        }
    }
}
