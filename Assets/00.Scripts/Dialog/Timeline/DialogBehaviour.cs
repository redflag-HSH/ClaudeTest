using UnityEngine;
using UnityEngine.Playables;

public class DialogBehaviour : PlayableBehaviour
{
    public Dialog dialog;
    public PlayableDirector director;

    bool opened;
    bool pausedByUs;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (!Application.isPlaying || opened) return;
        opened = true;

        if (DialogSystem.Instance != null && dialog != null)
        {
            // Set flag before Pause() — calling Pause() may synchronously fire
            // OnBehaviourPause on this same clip, and we must not close dialog there.
            pausedByUs = true;
            DialogSystem.Instance.OpenFromTimeline(dialog, director);
        }
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (!Application.isPlaying) return;

        // This pause was triggered by us — skip it
        if (pausedByUs) { pausedByUs = false; return; }

        // Genuine clip end or external stop — close dialog if still open
        opened = false;
        if (DialogSystem.Instance != null && DialogSystem.Instance.IsOpen)
            DialogSystem.Instance.Close();
    }
}
