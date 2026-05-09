using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SwordAura : MonoBehaviour
{
    public float damage  = 40f;
    public float speed   = 18f;
    public float lifetime = 1.5f;

    private LayerMask _enemyLayer;
    private BloodPuddleMaker _puddleMaker;

    public void Init(int facingDir, float dmg, LayerMask enemyLayer, BloodPuddleMaker puddleMaker = null)
    {
        damage       = dmg;
        _enemyLayer  = enemyLayer;
        _puddleMaker = puddleMaker;

        var rb = GetComponent<Rigidbody2D>();
        rb.gravityScale    = 0f;
        rb.linearVelocity  = new Vector2(facingDir * speed, 0f);

        float angle = facingDir >= 0 ? 0f : 180f;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (((1 << col.gameObject.layer) & _enemyLayer) == 0) return;
        if (col.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage);
        if (_puddleMaker != null) _puddleMaker.SpawnPuddle(transform.position);
        Destroy(gameObject);
    }
}
