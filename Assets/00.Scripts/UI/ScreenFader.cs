using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// gameBlack  — behind all UI  (Canvas sort order lower than HUD canvas)
/// fullBlack  — above all UI   (Canvas sort order highest)
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Game-only overlay  (behind UI)")]
    public CanvasGroup gameBlack;

    [Header("Full overlay  (above UI)")]
    public CanvasGroup fullBlack;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        SetAlpha(gameBlack, 0f);
        SetAlpha(fullBlack, 0f);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void GameFadeIn(float duration)  => StartCoroutine(Fade(gameBlack, 0f, 1f, duration));
    public void GameFadeOut(float duration) => StartCoroutine(Fade(gameBlack, 1f, 0f, duration));
    public void FullFadeIn(float duration)  => StartCoroutine(Fade(fullBlack, 0f, 1f, duration));
    public void FullFadeOut(float duration) => StartCoroutine(Fade(fullBlack, 1f, 0f, duration));

    // Awaitable versions (use with StartCoroutine or yield return)
    public Coroutine GameFadeInRoutine(float duration)  => StartCoroutine(Fade(gameBlack, 0f, 1f, duration));
    public Coroutine GameFadeOutRoutine(float duration) => StartCoroutine(Fade(gameBlack, 1f, 0f, duration));
    public Coroutine FullFadeInRoutine(float duration)  => StartCoroutine(Fade(fullBlack, 0f, 1f, duration));
    public Coroutine FullFadeOutRoutine(float duration) => StartCoroutine(Fade(fullBlack, 1f, 0f, duration));

    // Instant set
    public void SetGameBlack(bool on) => SetAlpha(gameBlack, on ? 1f : 0f);
    public void SetFullBlack(bool on) => SetAlpha(fullBlack, on ? 1f : 0f);

    // ── Internal ──────────────────────────────────────────────────────────────

    IEnumerator Fade(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;

        cg.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
        cg.blocksRaycasts = to > 0f;
    }

    static void SetAlpha(CanvasGroup cg, float a)
    {
        if (cg == null) return;
        cg.alpha = a;
        cg.blocksRaycasts = a > 0f;
    }
}
