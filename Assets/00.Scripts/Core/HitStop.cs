using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ──── HitStop ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Freezes time for <paramref name="duration"/> real-time seconds.
    /// </summary>
    /// <param name="duration">How long (real seconds) to hold the freeze.</param>
    /// <param name="timeScale">Time scale during freeze. Default 0 = full freeze.</param>
    public void DoHitStop(float duration, float timeScale = 0f)
    {
        StopAllCoroutines();
        StartCoroutine(HitStopCoroutine(duration, timeScale));
    }

    IEnumerator HitStopCoroutine(float duration, float timeScale)
    {
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
