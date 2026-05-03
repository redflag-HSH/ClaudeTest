using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quest/Quest")]
public class QuestData : ScriptableObject
{
    public string questId;
    [TextArea(1, 3)] public string text;
}
