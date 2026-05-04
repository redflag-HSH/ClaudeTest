public interface IStunnable
{
    void Stun(float duration);
    bool IsStunned { get; }
}
