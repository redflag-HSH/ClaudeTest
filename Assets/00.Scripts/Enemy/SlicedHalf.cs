using System.Collections;
using UnityEngine;

// Attached at runtime by EnemySliceable to each spawned half-piece.
// Handles the fade-out and self-destruction after the slice.
public class SlicedHalf : MonoBehaviour
{
    // ── Public API ────────────────────────────────────────────────────────────

    public void Init(SpriteRenderer pieceRenderer, float lifetime)
    {
        StartCoroutine(FadeAndDestroy(pieceRenderer, lifetime));
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    IEnumerator FadeAndDestroy(SpriteRenderer pieceRenderer, float lifetime)
    {
        float elapsed   = 0f;
        float fadeStart = lifetime * 0.55f;   // start fading slightly past halfway

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
