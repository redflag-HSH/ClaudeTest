using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlicedHalf : MonoBehaviour
{
    // Scene-wide cap set by EnemySliceable.maxActiveHalves
    public static int MaxActive = 12;

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
