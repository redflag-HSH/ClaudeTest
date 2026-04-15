using System.Collections;
using UnityEngine;

// ── Setup ──────────────────────────────────────────────────────────────────────
// Attach to a training-dummy GameObject alongside EnemySliceable.
// • Normal hits (TakeDamage) flash red and drain HP.
// • When HP reaches 0 the dummy triggers the slice effect and hides itself.
// • After resetDelay it respawns with full HP, ready to be hit again.
//
// PlayerSlice can also slice the dummy directly (it calls EnemySliceable.Slice).
// Both paths end up at OnSliced() which handles the respawn countdown.
// ──────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EnemySliceable))]
public class Dummy : MonoBehaviour, IDamageable
{
    // ── Stats ──────────────────────────────────────────────────────────────────

    [Header("Health")]
    public float maxHp = 200f;
    public float resetDelay = 3f;   // seconds after death before respawning
    public bool IsDead { get; set; }

    // ── Feedback ───────────────────────────────────────────────────────────────

    [Header("Feedback")]
    public Color hitColor = Color.red;
    public float flashDuration = 0.1f;

    // ── Slice ──────────────────────────────────────────────────────────────────

    [Header("Slice")]
    [Tooltip("Cut direction used when a normal attack (not PlayerSlice) kills the dummy.")]
    public Vector2 defaultSliceNormal = Vector2.up;   // horizontal cut by default

    // ── State ──────────────────────────────────────────────────────────────────

    public float CurrentHp { get; private set; }

    SpriteRenderer sr;
    Color originalColor;
    EnemySliceable sliceable;

    // ── Unity ──────────────────────────────────────────────────────────────────

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;

        sliceable = GetComponent<EnemySliceable>();
        sliceable.destroyOnSlice = false;       // keep the GameObject alive for respawn
        sliceable.onSliced += OnSliced;         // hook into the slice callback

        CurrentHp = maxHp;
    }

    void OnDestroy()
    {
        // Clean up the delegate when the object is actually removed from the scene
        if (sliceable != null)
            sliceable.onSliced -= OnSliced;
    }

    // ── IDamageable ────────────────────────────────────────────────────────────

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        CurrentHp -= amount;
        Debug.Log($"[Dummy] Hit for {amount:F1} — HP: {CurrentHp:F1} / {maxHp:F1}");

        if (CurrentHp <= 0f)
        {
            CurrentHp = 0f;
            IsDead = true;
            IsDead = true;
            // Slice visual is handled by the caller (e.g. PlayerControl) with the
            // correct hit point. onSliced fires from there, triggering the respawn.
        }
        else
        {
            StartCoroutine(HitFlash());
        }
    }

    // ── Heal ───────────────────────────────────────────────────────────────────

    public void Heal(float amount)
    {
        if (IsDead) return;

        CurrentHp = Mathf.Min(CurrentHp + amount, maxHp);
        Debug.Log($"[Dummy] Healed {amount:F1} — HP: {CurrentHp:F1} / {maxHp:F1}");
    }

    // ── Slice callback (called by EnemySliceable after halves are spawned) ─────

    void OnSliced()
    {
        IsDead = true;
        IsDead = true;
        CurrentHp = 0f;
        Debug.Log($"[Dummy] Sliced — respawning in {resetDelay}s.");
        StartCoroutine(RespawnAfterDelay());
    }

    // ── Reset ──────────────────────────────────────────────────────────────────

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);
        ResetDummy();
    }

    public void ResetDummy()
    {
        IsDead = false;
        IsDead = false;
        CurrentHp = maxHp;

        // Re-enable the sprite and collider that EnemySliceable disabled
        sliceable.ResetSlice();

        if (sr != null) sr.color = originalColor;

        Debug.Log("[Dummy] Respawned.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    IEnumerator HitFlash()
    {
        if (sr == null) yield break;
        sr.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        if (!IsDead) sr.color = originalColor;
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        float fraction = maxHp > 0f ? CurrentHp / maxHp : 0f;
        Gizmos.color = Color.Lerp(Color.red, Color.green, fraction);
        Vector3 origin = transform.position + Vector3.up * 1.2f;
        Gizmos.DrawLine(origin - Vector3.right * 0.5f,
                        origin - Vector3.right * 0.5f + Vector3.right * fraction);
    }
}
