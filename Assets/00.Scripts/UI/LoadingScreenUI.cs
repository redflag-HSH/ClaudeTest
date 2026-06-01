using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Place on a GameObject in your dedicated Loading scene.
/// Reads SceneLoader.TargetScene, async-loads it with a progress bar,
/// then activates the scene once ready.
/// </summary>
public class LoadingScreenUI : MonoBehaviour
{
    [Header("Progress")]
    [SerializeField] private Slider progressBar;
    [Tooltip("Minimum seconds the loading screen is visible even if the load finishes instantly.")]
    [SerializeField] private float minDisplayTime = 0.5f;

    [Header("Fade")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    void Start()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        StartCoroutine(LoadRoutine());
    }

    IEnumerator LoadRoutine()
    {
        string target = SceneLoader.TargetScene;
        if (string.IsNullOrEmpty(target))
        {
            Debug.LogError("[LoadingScreenUI] No target scene set in SceneLoader.");
            yield break;
        }

        yield return FadeCanvas(0f, 1f);

        float startTime = Time.unscaledTime;

        AsyncOperation op = SceneManager.LoadSceneAsync(target);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            SetProgress(op.progress / 0.9f);
            yield return null;
        }

        SetProgress(1f);

        float elapsed = Time.unscaledTime - startTime;
        if (elapsed < minDisplayTime)
            yield return new WaitForSecondsRealtime(minDisplayTime - elapsed);

        yield return FadeCanvas(1f, 0f);

        op.allowSceneActivation = true;
    }

    void SetProgress(float t)
    {
        if (progressBar != null) progressBar.value = t;
    }

    IEnumerator FadeCanvas(float from, float to)
    {
        if (canvasGroup == null) yield break;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
