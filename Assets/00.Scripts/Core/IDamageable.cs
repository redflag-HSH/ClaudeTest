public interface IDamageable
{
    void TakeDamage(float amount);
    bool IsDead { get; set; }
}
