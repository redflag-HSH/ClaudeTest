using System.Collections;
using UnityEngine;

/// <summary>
/// Animator lives on the parent Canvas.
/// Required triggers:  GameFadeIn  GameFadeOut  FullFadeIn  FullFadeOut
/// Required states with those same names on layer 0.
/// gameBlack GameObject is enabled before FadeIn and disabled after FadeOut.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Game-only overlay  (behind UI)")]
    public CanvasGroup gameBlack;

    [Header("Full overlay  (above UI)")]
    public CanvasGroup fullBlack;

    private Animator _animator;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _animator = GetComponentInParent<Animator>();

        SetActive(gameBlack, false);
        SetActive(fullBlack, false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    // Void versions for Timeline Signal Receivers
    public void GameFadeIn() => StartCoroutine(PlayAnim("GameFadeIn", gameBlack, true));
    public void GameFadeOut() => StartCoroutine(PlayAnim("GameFadeOut", gameBlack, false));
    public void FullFadeIn() => StartCoroutine(PlayAnim("FullFadeIn", fullBlack, true));
    public void FullFadeOut() => StartCoroutine(PlayAnim("FullFadeOut", fullBlack, false));

    // Instant snaps — no animation, safe to call from Timeline signals or code
    public void SnapToBlack() { SetActive(fullBlack, true); fullBlack.alpha = 1f; }
    public void SnapToClear() { SetActive(fullBlack, false); fullBlack.alpha = 0f; }
    public void GameSnapToBlack() { SetActive(gameBlack, true); gameBlack.alpha = 1f; }
    public void GameSnapToClear() { SetActive(gameBlack, false); gameBlack.alpha = 0f; }

    // Called at cutscene end to guarantee both overlays are hidden
    public void ResetAll()
    {
        StopAllCoroutines();
        SetActive(gameBlack, false); if (gameBlack != null) gameBlack.alpha = 1f;
        SetActive(fullBlack, false); if (fullBlack != null) fullBlack.alpha = 1f;
    }

    // Awaitable versions (yield return these to wait for the fade to finish)
    public Coroutine GameFadeInRoutine() => StartCoroutine(PlayAnim("GameFadeIn", gameBlack, true));
    public Coroutine GameFadeOutRoutine() => StartCoroutine(PlayAnim("GameFadeOut", gameBlack, false));
    public Coroutine FullFadeInRoutine() => StartCoroutine(PlayAnim("FullFadeIn", fullBlack, true));
    public Coroutine FullFadeOutRoutine() => StartCoroutine(PlayAnim("FullFadeOut", fullBlack, false));

    // ── Internal ──────────────────────────────────────────────────────────────

    IEnumerator PlayAnim(string triggerName, CanvasGroup cg, bool fadeIn)
    {
        if (_animator == null) yield break;

        if (fadeIn) SetActive(cg, true);

        _animator.SetTrigger(triggerName);

        // Two frames: first lets the trigger register, second lets the transition begin.
        yield return null;
        yield return null;

        // During a transition, GetCurrentAnimatorStateInfo(0) returns the SOURCE state,
        // not the destination. Wait until the transition ends and we are inside the
        // target state before checking normalizedTime.
        while (_animator.IsInTransition(0) ||
               !_animator.GetCurrentAnimatorStateInfo(0).IsName(triggerName))
        {
            yield return null;
        }

        // Now wait for the animation clip to play to completion.
        while (_animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        if (!fadeIn) SetActive(cg, false);
    }

    static void SetActive(CanvasGroup cg, bool on)
    {
        if (cg == null) return;
        cg.gameObject.SetActive(on);
    }
}
