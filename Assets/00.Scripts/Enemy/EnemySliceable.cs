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
    public float halfSeparationForce = 4f;
    public float halfSpin = 200f;
    public float halfLifetime = 1.2f;
    public float halfGravityScale = 2f;

    [Header("Physics")]
    public float halfBounciness = 0.3f;
    public float halfFriction = 0.2f;

    [Header("Optimization")]
    [Tooltip("Max sliced halves alive at once across the whole scene. Oldest are removed first.")]
    public int maxActiveHalves = 22;

    [Header("Respawn")]
    [Tooltip("If false the GameObject is disabled instead of destroyed after slicing (used by Dummy).")]
    public bool destroyOnSlice = true;

    private string layerName = "SlicePieces";  // set this to the name of your slice piece sorting layer

    // Called after the visual halves are spawned. Subscribe in Awake when
    // destroyOnSlice = false so you can handle respawn logic yourself.
    public System.Action onSliced;

    [SerializeField] private int money = 10;
    public int Money => money;
    [SerializeField] private float hpHeal = 0f;
    public float HpHeal => hpHeal;

    // ── Internals ─────────────────────────────────────────────────────────────

    // Each slice pair gets a unique sorting order so SpriteMasks only affect
    // their own paired SpriteRenderer and nothing else in the scene.
    static int _nextSliceOrder = 5000;

    bool _sliced;
    protected SpriteRenderer sr;
    PhysicsMaterial2D _halfPhysMat;

    protected virtual void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        _halfPhysMat = new PhysicsMaterial2D("SlicedHalfMat")
        {
            bounciness = halfBounciness,
            friction = halfFriction
        };
        SlicedHalf.MaxActive = maxActiveHalves;
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Split the enemy along the plane defined by <paramref name="sliceNormal"/>.
    /// Call this from PlayerSlice when the slice ray hits this enemy.
    /// </summary>
    /// <param name="sliceNormal">
    /// World-space normal of the cut plane (e.g. Vector2.up = horizontal cut,
    /// Vector2.right = vertical cut, any normalized diagonal for angled cuts).
    /// </param>
    public void Slice(Vector2 sliceNormal, Vector2? contactPoint = null, float sliceForcePower = 0f, Vector2 playerPos = default)
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

        SpawnHalf(maskSprite, sliceNormal, +1, cp, sliceForcePower, playerPos);
        SpawnHalf(maskSprite, sliceNormal, -1, cp, sliceForcePower, playerPos);

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

    // ── Whole Creation ────────────────────────────────────────────────────────

    /// <summary>
    /// Spawns the full sprite as a single physics prop that arcs, spins, and fades —
    /// mirrors SpawnHalf but skips the mask/cut entirely.
    /// </summary>
    /// <param name="launchVelocity">
    /// World-space velocity applied to the prop. Defaults to a centred upward arc.
    /// </param>
    public void SpawnWhole(Vector2? launchVelocity = null)
    {
        if (sr == null) return;

        Bounds bounds = sr.bounds;
        int order = _nextSliceOrder++;

        // ── Root — mirrors SpawnHalf root setup ───────────────────────────────
        var root = new GameObject("SpawnedWhole");
        root.transform.position = transform.position;
        root.transform.rotation = transform.rotation;
        root.gameObject.layer = LayerMask.NameToLayer(layerName);

        var sg = root.AddComponent<UnityEngine.Rendering.SortingGroup>();
        sg.sortingLayerID = sr.sortingLayerID;
        sg.sortingOrder = order;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = halfGravityScale;
        rb.freezeRotation = false;
        rb.angularVelocity = halfSpin * (Random.value > 0.5f ? 1 : -1);
        rb.linearVelocity = launchVelocity ?? new Vector2(0f, halfSeparationForce);

        var col = root.AddComponent<BoxCollider2D>();
        col.size = bounds.size;
        col.sharedMaterial = _halfPhysMat;

        // ── Sprite child — no mask needed, full sprite visible ────────────────
        var spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(root.transform, false);
        spriteObj.transform.localScale = transform.localScale;

        var wholeSr = spriteObj.AddComponent<SpriteRenderer>();
        wholeSr.sprite = sr.sprite;
        wholeSr.color = sr.color;
        wholeSr.flipX = sr.flipX;
        wholeSr.flipY = sr.flipY;

        // ── Fade & self-destruct — same as SpawnHalf ──────────────────────────
        root.AddComponent<SlicedHalf>().Init(wholeSr, halfLifetime);
    }

    // ── Half Creation ─────────────────────────────────────────────────────────

    void SpawnHalf(Sprite maskSprite, Vector2 sliceNormal, int side, Vector2 contactPoint, float sliceForcePower = 0f, Vector2 playerPos = default)
    {
        Bounds bounds = sr.bounds;                          // world-space bounds
        float diag = bounds.extents.magnitude * 4f + 1f;   // safely larger than sprite

        int order = _nextSliceOrder++;

        // ── Root (owns Rigidbody2D, moves & rotates freely) ───────────────────
        var root = new GameObject($"SlicedHalf_{(side > 0 ? "A" : "B")}");
        root.transform.position = transform.position;
        root.transform.rotation = transform.rotation;

        root.gameObject.layer = LayerMask.NameToLayer(layerName);

        // SortingGroup guarantees the SpriteMask below only affects the
        // SpriteRenderer below — no custom range needed, no cross-bleed possible.
        var sg = root.AddComponent<UnityEngine.Rendering.SortingGroup>();
        sg.sortingLayerID = sr.sortingLayerID;
        sg.sortingOrder = order;

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = halfGravityScale;
        rb.freezeRotation = false;
        rb.angularVelocity = halfSpin * side;

        // Physical collider — sized to the original sprite bounds so the half
        // lands and bounces on geometry instead of passing through it.
        var col = root.AddComponent<BoxCollider2D>();
        col.size = bounds.size;
        col.sharedMaterial = _halfPhysMat;

        Vector2 separateVel = sliceNormal * side * halfSeparationForce;
        separateVel.y += 1.5f;

        if (sliceForcePower > 0f)
        {
            Vector2 awayDir = (Vector2)transform.position - playerPos;
            if (awayDir.sqrMagnitude > 0.001f) awayDir.Normalize();
            separateVel += awayDir * sliceForcePower;
        }

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
