using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class DialogClip : PlayableAsset, ITimelineClipAsset
{
    public Dialog dialog;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var behaviour = ScriptPlayable<DialogBehaviour>.Create(graph);
        var data = behaviour.GetBehaviour();
        data.dialog = dialog;
        data.director = owner.GetComponent<PlayableDirector>();
        return behaviour;
    }
}
