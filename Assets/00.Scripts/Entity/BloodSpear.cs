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
    private BloodPuddleMaker _puddleMaker;
    private bool _fireRequested;

    // Original timed-hover init (hover for hoverDuration then auto-seek)
    public void Init(Transform player, Vector2 hoverOffset, int facingDir, float dmg, LayerMask layer, BloodPuddleMaker puddleMaker = null)
    {
        _player = player;
        _hoverOffset = hoverOffset;
        _facingDir = new Vector2(facingDir, 0f);
        damage = dmg;
        enemyLayer = layer;
        _puddleMaker = puddleMaker;

        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;

        SetRotation(_facingDir);

        Destroy(gameObject, lifetime);
        StartCoroutine(SpearRoutine());
    }

    // Hold-gimmick init — spears orbit indefinitely until Fire() is called
    public void InitHold(Transform player, Vector2 hoverOffset, int facingDir, float dmg, LayerMask layer, BloodPuddleMaker puddleMaker = null)
    {
        _player = player;
        _hoverOffset = hoverOffset;
        _facingDir = new Vector2(facingDir, 0f);
        damage = dmg;
        enemyLayer = layer;
        _puddleMaker = puddleMaker;
        _fireRequested = false;

        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.linearVelocity = Vector2.zero;

        SetRotation(_facingDir);

        Destroy(gameObject, lifetime);
        StartCoroutine(HoldRoutine());
    }

    // Call this (from PlayerSkill) when the Skill button is released
    public void Fire()
    {
        _fireRequested = true;
    }

    // Orbit indefinitely until Fire() is called, then seek enemies
    private IEnumerator HoldRoutine()
    {
        while (!_fireRequested)
        {
            if (_player == null) break;
            Vector2 target = (Vector2)_player.position + _hoverOffset;
            _rb.linearVelocity = (target - (Vector2)transform.position) * hoverFollowSpeed;
            SetRotation(_facingDir);
            yield return null;
        }

        _rb.linearVelocity = Vector2.zero;
        yield return StartCoroutine(SeekAndFly());
    }

    // Original timed hover then seek
    private IEnumerator SpearRoutine()
    {
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
        yield return StartCoroutine(SeekAndFly());
    }

    private IEnumerator SeekAndFly()
    {
        Transform enemy = FindNearestEnemy();
        while (enemy != null)
        {
            Vector2 toEnemy = (Vector2)enemy.position - (Vector2)transform.position;
            float angle = Mathf.Atan2(toEnemy.y, toEnemy.x) * Mathf.Rad2Deg;
            float current = transform.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(current, angle, turnSpeed * Time.deltaTime);
            transform.rotation = Quaternion.AngleAxis(newAngle, Vector3.forward);

            _rb.linearVelocity = transform.right * speed;
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
        if (_puddleMaker != null) _puddleMaker.SpawnPuddle(transform.position);
        Destroy(gameObject);
    }
}
