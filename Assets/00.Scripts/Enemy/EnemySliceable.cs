using UnityEngine;

// ── Setup ──────────────────────────────────────────────────────────────────────
// Attach this component to any enemy alongside MeleeMonster (or any IDamageable).
// When Slice(sliceNormal) is called, the enemy is replaced by two masked sprite
// halves that fly apart, spin, and fade out.
//
// Requirements:
//   • Enemy must have a SpriteRenderer on the root GameObject.
//   • Create a Sorting Layer named "SlicePieces" in Project Settings → Tags & Layers
//     (or change the layer name in sliceSortingLayer below).
// ──────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(SpriteRenderer))]
public class EnemySliceable : MonoBehaviour
{
    // ── Slice Settings ────────────────────────────────────────────────────────

    [Header("Slice Settings")]
    public float halfSeparationForce = 4f;   // impulse pushing halves apart
    public float halfSpin = 200f;            // degrees/sec rotation applied to pieces
    public float halfLifetime = 1.2f;        // seconds before pieces disappear
    public float halfGravityScale = 2f;      // how fast pieces fall

    [Header("Respawn")]
    [Tooltip("If false the GameObject is disabled instead of destroyed after slicing (used by Dummy).")]
    public bool destroyOnSlice = true;

    // Called after the visual halves are spawned. Subscribe in Awake when
    // destroyOnSlice = false so you can handle respawn logic yourself.
    public System.Action onSliced;

    // ── Internals ─────────────────────────────────────────────────────────────

    // Each slice pair gets a unique sorting order so SpriteMasks only affect
    // their own paired SpriteRenderer and nothing else in the scene.
    static int _nextSliceOrder = 5000;

    bool _sliced;
    protected SpriteRenderer sr;

    protected void Awake() => sr = GetComponent<SpriteRenderer>();

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Split the enemy along the plane defined by <paramref name="sliceNormal"/>.
    /// Call this from PlayerSlice when the slice ray hits this enemy.
    /// </summary>
    /// <param name="sliceNormal">
    /// World-space normal of the cut plane (e.g. Vector2.up = horizontal cut,
    /// Vector2.right = vertical cut, any normalized diagonal for angled cuts).
    /// </param>
    public void Slice(Vector2 sliceNormal, Vector2? contactPoint = null)
    {
        if (sr == null || _sliced) return;
        _sliced = true;

        // Stop the enemy AI so it doesn't keep acting while being destroyed
        if (TryGetComponent<MeleeMonster>(out var monster))
            monster.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Sprite maskSprite = BuildWhiteSquareSprite();

        // Fall back to the enemy's own centre when no contact point is supplied
        Vector2 cp = contactPoint ?? (Vector2)transform.position;

        SpawnHalf(maskSprite, sliceNormal, +1, cp);
        SpawnHalf(maskSprite, sliceNormal, -1, cp);

        HitStop.Instance.DoHitStop(0.08f);  // brief freeze for impact feel

        if (destroyOnSlice)
        {
            onSliced?.Invoke();
            Destroy(gameObject);
        }
        else
        {
            // Hide the original sprite — the spawned halves carry the visual.
            // The host is responsible for re-enabling via ResetSlice().
            sr.enabled = false;
            onSliced?.Invoke();
        }
    }

    /// <summary>
    /// Re-enables the sprite and collider so the host object can be reused
    /// (e.g. a Dummy respawning). Only meaningful when destroyOnSlice = false.
    /// </summary>
    public void ResetSlice()
    {
        _sliced = false;
        if (sr != null) sr.enabled = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
    }

    // ── Half Creation ─────────────────────────────────────────────────────────

    void SpawnHalf(Sprite maskSprite, Vector2 sliceNormal, int side, Vector2 contactPoint)
    {
        Bounds bounds = sr.bounds;                          // world-space bounds
        float diag = bounds.extents.magnitude * 4f + 1f;   // safely larger than sprite

        int order = _nextSliceOrder++;

        // ── Root (owns Rigidbody2D, moves & rotates freely) ───────────────────
        var root = new GameObject($"SlicedHalf_{(side > 0 ? "A" : "B")}");
        root.transform.position = transform.position;
        root.transform.rotation = transform.rotation;

        // SortingGroup guarantees the SpriteMask below only affects the
        // SpriteRenderer below — no custom range needed, no cross-bleed possible.
        var sg = root.AddComponent<UnityEngine.Rendering.SortingGroup>();
        sg.sortingLayerID = sr.sortingLayerID;
        sg.sortingOrder = order;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = halfGravityScale;
        rb.freezeRotation = false;
        rb.angularVelocity = halfSpin * side;

        // Push halves apart along sliceNormal
        Vector2 separateVel = sliceNormal * side * halfSeparationForce;
        separateVel.y += 1.5f;   // slight upward kick so both halves visibly arc
        rb.linearVelocity = separateVel;

        // ── Sprite child (shows only the half inside the mask) ────────────────
        var spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(root.transform, false);
        spriteObj.transform.localScale = transform.localScale;  // preserve enemy scale

        var halfSr = spriteObj.AddComponent<SpriteRenderer>();
        halfSr.sprite = sr.sprite;
        halfSr.color = sr.color;
        halfSr.flipX = sr.flipX;
        halfSr.flipY = sr.flipY;
        halfSr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        // ── Mask child (covers one side of the cut line) ──────────────────────
        var maskObj = new GameObject("Mask");
        maskObj.transform.SetParent(root.transform, false);

        var mask = maskObj.AddComponent<SpriteMask>();
        mask.sprite = maskSprite;
        // No isCustomRangeActive needed — SortingGroup already isolates this mask
        // so it cannot bleed onto other enemies' sprites.

        // Project the contact point onto sliceNormal to get the cut depth.
        // Only this component matters — the lateral offset along the cut line is irrelevant
        // and would shift the mask sideways, making the cut appear in the wrong place.
        Vector2 localContact = contactPoint - (Vector2)transform.position;
        float cutDepth = Vector2.Dot(localContact, sliceNormal);
        Vector2 maskOffset = sliceNormal * (cutDepth + side * diag * 0.5f);
        maskObj.transform.localPosition = new Vector3(maskOffset.x, maskOffset.y, 0f);

        // Align mask rotation with the cut direction (cosmetic, square masks work either way)
        float angle = Mathf.Atan2(sliceNormal.y, sliceNormal.x) * Mathf.Rad2Deg;
        maskObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        maskObj.transform.localScale = new Vector3(diag, diag, 1f);

        // ── Fade & self-destruct ───────────────────────────────────────────────
        root.AddComponent<SlicedHalf>().Init(halfSr, halfLifetime);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Creates a 1×1 white pixel sprite at runtime — used as the SpriteMask shape.
    // Scales up to any size because it is a solid colour with no detail.
    static Sprite BuildWhiteSquareSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
}
