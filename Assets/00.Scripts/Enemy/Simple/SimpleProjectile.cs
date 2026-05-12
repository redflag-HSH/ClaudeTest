using UnityEngine;

// Requires a Collider2D set to Is Trigger on the prefab.
[RequireComponent(typeof(Collider2D))]
public class SimpleProjectile : MonoBehaviour
{
    Vector2 _dir;
    float _speed;
    float _damage;
    bool _isSpecial;
    LayerMask _playerLayer;
    public System.Action onPlayerHit;

    public void Init(Vector2 dir, float speed, float damage, bool isSpecial, LayerMask playerLayer, float lifetime = 5f)
    {
        _dir = dir.normalized;
        _speed = speed;
        _damage = damage;
        _isSpecial = isSpecial;
        _playerLayer = playerLayer;
        Destroy(gameObject, lifetime);

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.Translate(Vector2.right * _speed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if ((_playerLayer.value & (1 << col.gameObject.layer)) == 0) return;

        if (_isSpecial)
        {
            if (col.TryGetComponent<PlayerControl>(out var pc))
            {
                pc.TakeSpecialDamage(_damage);
                onPlayerHit?.Invoke();
            }
        }
        else
        {
            if (col.TryGetComponent<IDamageable>(out var target))
                target.TakeDamage(_damage);
        }

        Destroy(gameObject);
    }
}
