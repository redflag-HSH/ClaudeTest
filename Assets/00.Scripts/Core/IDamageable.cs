public interface IDamageable
{
    void TakeDamage(float amount, float stunDuration = 0f);
    bool IsDead { get; set; }
}
