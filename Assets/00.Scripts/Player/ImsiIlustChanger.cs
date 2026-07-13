using UnityEngine;

public class ImsiIlustChanger : MonoBehaviour
{
    [SerializeField] SpriteRenderer _renderer;
    [SerializeField] Sprite _Attack;
    [SerializeField] Sprite _Idle;
    [SerializeField] Sprite _Jump;
    public void Idle()
    {
        _renderer.sprite = _Idle;
    }
    public void Jump()
    {
        _renderer.sprite = _Jump;
    }
    public void Attack()
    {
        _renderer.sprite = _Attack;
    }
}
