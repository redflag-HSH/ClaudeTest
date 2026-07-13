using System.Collections.Generic;
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

    // ── Pending Slice ─────────────────────────────────────────────────────────

    // Set these before calling TakeDamage so DeathAnimation uses the correct cut.
    [HideInInspector] public Vector2 pendingSliceNormal = Vector2.up;
    [HideInInspector] public Vector2 pendingSliceContact;
    [HideInInspector] public float pendingSliceForcePower;
    [HideInInspector] public Vector2 pendingSlicePlayerPos;

    public void SetPendingSlice(Vector2 normal, Vector2 contact, float forcePower, Vector2 playerPos)
    {
        pendingSliceNormal = normal;
        pendingSliceContact = contact;
        pendingSliceForcePower = forcePower;
        pendingSlicePlayerPos = playerPos;
    }

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
        if (TryGetComponent<StateMachine>(out var sm))
            sm.enabled = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Fall back to the enemy's own centre when no contact point is supplied
        Vector2 cp = contactPoint ?? (Vector2)transform.position;

        SpawnHalf(sliceNormal, +1, cp, sliceForcePower, playerPos);
        SpawnHalf(sliceNormal, -1, cp, sliceForcePower, playerPos);

        GameManager.Instance.DoHitStop(0.08f);  // brief freeze for impact feel

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

    void SpawnHalf(Vector2 sliceNormal, int side, Vector2 contactPoint, float sliceForcePower = 0f, Vector2 playerPos = default)
    {
        // ── Clip sprite triangles ─────────────────────────────────────────────
        Vector2[] srcVerts = sr.sprite.vertices;
        Vector2[] srcUVs   = sr.sprite.uv;
        ushort[]  srcTris  = sr.sprite.triangles;

        Vector2 contactLocal = transform.InverseTransformPoint(contactPoint);
        Vector2 planeNormal  = sliceNormal * side;
        float   planeDist    = Vector2.Dot(contactLocal, planeNormal);

        var outVerts = new List<Vector2>();
        var outUVs   = new List<Vector2>();
        var outTris  = new List<int>();

        for (int t = 0; t < srcTris.Length; t += 3)
        {
            var triV = new List<Vector2> { srcVerts[srcTris[t]], srcVerts[srcTris[t+1]], srcVerts[srcTris[t+2]] };
            var triU = new List<Vector2> { srcUVs[srcTris[t]],   srcUVs[srcTris[t+1]],  srcUVs[srcTris[t+2]]  };
            ClipPolygon(triV, triU, planeNormal, planeDist);
            if (triV.Count < 3) continue;

            int baseIdx = outVerts.Count;
            outVerts.AddRange(triV);
            outUVs.AddRange(triU);
            for (int i = 0; i < triV.Count - 2; i++)
            {
                outTris.Add(baseIdx);
                outTris.Add(baseIdx + i + 1);
                outTris.Add(baseIdx + i + 2);
            }
        }

        if (outVerts.Count < 3) return;

        // ── Root ──────────────────────────────────────────────────────────────
        var root = new GameObject($"SlicedHalf_{(side > 0 ? "A" : "B")}");
        root.transform.position = transform.position;
        root.transform.rotation = transform.rotation;
        root.gameObject.layer = LayerMask.NameToLayer(layerName);

        var rb = root.AddComponent<Rigidbody2D>();
        rb.gravityScale = halfGravityScale;
        rb.freezeRotation = false;
        rb.angularVelocity = halfSpin * side;

        var col = root.AddComponent<BoxCollider2D>();
        col.size = sr.bounds.size;
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

        // ── Clipped mesh ──────────────────────────────────────────────────────
        var spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(root.transform, false);
        Vector3 scale = transform.localScale;
        if (sr.flipX) scale.x *= -1f;
        if (sr.flipY) scale.y *= -1f;
        spriteObj.transform.localScale = scale;

        var verts3 = new Vector3[outVerts.Count];
        for (int i = 0; i < outVerts.Count; i++)
            verts3[i] = new Vector3(outVerts[i].x, outVerts[i].y, 0f);

        var mesh = new Mesh();
        mesh.vertices  = verts3;
        mesh.uv        = outUVs.ToArray();
        mesh.triangles = outTris.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        spriteObj.AddComponent<MeshFilter>().mesh = mesh;

        var mat = new Material(sr.sharedMaterial);
        mat.mainTexture = sr.sprite.texture;
        mat.color = sr.color;

        var mr = spriteObj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.sortingLayerID = sr.sortingLayerID;
        mr.sortingOrder   = sr.sortingOrder;

        root.AddComponent<SlicedHalf>().Init(mat, halfLifetime, sr.color);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void ClipPolygon(List<Vector2> poly, List<Vector2> polyUVs, Vector2 planeNormal, float planeDist)
    {
        var outP = new List<Vector2>();
        var outU = new List<Vector2>();
        int n = poly.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = poly[i],       b = poly[(i + 1) % n];
            Vector2 ua = polyUVs[i],  ub = polyUVs[(i + 1) % n];
            float da = Vector2.Dot(a, planeNormal) - planeDist;
            float db = Vector2.Dot(b, planeNormal) - planeDist;
            if (da >= 0f) { outP.Add(a); outU.Add(ua); }
            if ((da >= 0f) != (db >= 0f))
            {
                float t = da / (da - db);
                outP.Add(Vector2.Lerp(a, b, t));
                outU.Add(Vector2.Lerp(ua, ub, t));
            }
        }
        poly.Clear();    poly.AddRange(outP);
        polyUVs.Clear(); polyUVs.AddRange(outU);
    }
}
