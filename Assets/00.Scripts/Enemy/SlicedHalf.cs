using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlicedHalf : MonoBehaviour
{
    // Scene-wide cap set by EnemySliceable.maxActiveHalves
    public static int MaxActive = 12;

    [Header("Hit")]
    public float damage = 20f;
    public LayerMask enemyLayer;
    public LayerMask groundLayer;
    [Tooltip("Layer the hitbox child sits on — must interact with enemy and ground layers in Physics 2D settings.")]
    public int hitboxLayer = 0;
    public float hitboxRadius = 0.3f;

    bool _hasHit;
    GameObject _hitboxObj;

    void Awake()
    {
        if (PlayerControl.Instance != null)
        {
            enemyLayer  = PlayerControl.Instance.enemyLayer;
            groundLayer = PlayerControl.Instance.groundLayer;
        }

        _hitboxObj = new GameObject("SliceHitbox");
        _hitboxObj.transform.SetParent(transform, false);
        _hitboxObj.layer = hitboxLayer;

        var col = _hitboxObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = hitboxRadius;

        _hitboxObj.AddComponent<SlicedHalfHitbox>();
    }

    void DisableHitbox()
    {
        _hasHit = true;
        if (_hitboxObj != null) _hitboxObj.SetActive(false);
    }

    static readonly List<SlicedHalf> s_active = new List<SlicedHalf>();

    public void Init(SpriteRenderer pieceRenderer, float lifetime)
    {
        StartCoroutine(FadeAndDestroy(pieceRenderer, lifetime));
    }

    void OnEnable()
    {
        // Prune destroyed entries
        s_active.RemoveAll(h => h == null);

        // Destroy the oldest half when over the cap
        if (s_active.Count >= MaxActive)
        {
            var oldest = s_active[0];
            s_active.RemoveAt(0);
            if (oldest != null) Destroy(oldest.gameObject);
        }

        s_active.Add(this);
    }

    void OnDestroy() => s_active.Remove(this);

    // Called by SlicedHalfHitbox child when its trigger overlaps something
    public void OnHitGround()
    {
        DisableHitbox();
    }

    public void OnHitEnemy(Collider2D col)
    {
        if (_hasHit) return;
        if ((enemyLayer.value & (1 << col.gameObject.layer)) == 0) return;

        if (col.TryGetComponent<IDamageable>(out var target))
            target.TakeDamage(damage);

        DisableHitbox();
    }

    IEnumerator FadeAndDestroy(SpriteRenderer pieceRenderer, float lifetime)
    {
        float elapsed   = 0f;
        float fadeStart = lifetime * 0.55f;

        Color startColor = pieceRenderer != null ? pieceRenderer.color : Color.white;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;

            if (pieceRenderer != null && elapsed > fadeStart)
            {
                float t = (elapsed - fadeStart) / (lifetime - fadeStart);
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                pieceRenderer.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
