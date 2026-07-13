using UnityEngine;

/// <summary>
/// Place this in the scene so Timeline SignalReceiver can call it.
/// Delegates to ScreenFader.Instance which lives on a DontDestroyOnLoad canvas.
/// </summary>
public class ScreenFaderBridge : MonoBehaviour
{
    public void GameFadeIn() { if (ScreenFader.Instance != null) ScreenFader.Instance.GameFadeIn(); }
    public void GameFadeOut() { if (ScreenFader.Instance != null) ScreenFader.Instance.GameFadeOut(); }
    public void FullFadeIn() { if (ScreenFader.Instance != null) ScreenFader.Instance.FullFadeIn(); }
    public void FullFadeOut() { if (ScreenFader.Instance != null) ScreenFader.Instance.FullFadeOut(); }

    public void GameSnapIn() { if (ScreenFader.Instance != null) ScreenFader.Instance.GameSnapToClear(); }
    public void GameSnapOut() { if (ScreenFader.Instance != null) ScreenFader.Instance.GameSnapToBlack(); }
    public void FullSnapIn() { if (ScreenFader.Instance != null) ScreenFader.Instance.SnapToClear(); }
    public void FullSnapOut() { if (ScreenFader.Instance != null) ScreenFader.Instance.SnapToBlack(); }
}
