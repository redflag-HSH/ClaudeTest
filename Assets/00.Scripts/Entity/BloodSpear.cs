using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BloodSpear : MonoBehaviour
{
    [Header("Stats")]
    public float damage = 30f;
    public float speed = 14f;
    public float lifetime = 5f;

    [Header("Hover")]
    public float hoverDuration = 1.2f;
    public float hoverFollowSpeed = 10f;

    [Header("Seek")]
    public float seekRadius = 20f;
    public float turnSpeed = 200f;
    public LayerMask enemyLayer;

    private Transform _player;
    private Vector2 _hoverOffset;
    private Vector2 _facingDir;
    private Rigidbody2D _rb;

    public void Init(Transform player, Vector2 hoverOffset, int facingDir, float dmg, LayerMask layer)
    {
        _player = player;
        _hoverOffset = hoverOffset;
        _facingDir = new Vector2(facingDir, 0f);
        damage = dmg;
        enemyLayer = layer;

        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;

        SetRotation(_facingDir);

        Destroy(gameObject, lifetime);
        StartCoroutine(SpearRoutine());
    }

    private IEnumerator SpearRoutine()
    {
        // Phase 1: hover near player, always face player's direction
        float elapsed = 0f;
        while (elapsed < hoverDuration)
        {
            if (_player == null) break;
            Vector2 target = (Vector2)_player.position + _hoverOffset;
            _rb.linearVelocity = (target - (Vector2)transform.position) * hoverFollowSpeed;

            SetRotation(_facingDir);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _rb.linearVelocity = Vector2.zero;

        // Phase 2: seek nearest enemy
        Transform enemy = FindNearestEnemy();
        while (enemy != null)
        {
            Vector2 toEnemy = (Vector2)enemy.position - (Vector2)transform.position;
            float angle = Mathf.Atan2(toEnemy.y, toEnemy.x) * Mathf.Rad2Deg;
            float current = transform.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(current, angle, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.forward);

            Vector2 dir = transform.right;
            _rb.linearVelocity = dir * speed;

            yield return null;
        }

        // no enemy found — fly straight in current facing direction
        _rb.linearVelocity = transform.right * speed;
    }

    private Transform FindNearestEnemy()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, seekRadius, enemyLayer);
        Transform nearest = null;
        float bestDist = float.MaxValue;
        foreach (var col in hits)
        {
            float d = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; nearest = col.transform; }
        }
        return nearest;
    }

    private void SetRotation(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & enemyLayer) == 0) return;
        if (collision.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage);
        Destroy(gameObject);
    }
}
