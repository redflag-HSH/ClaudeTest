/// <summary>
/// Implement this on any MonoBehaviour that should change the active quest
/// (e.g. an NPC, a trigger zone, an interactable object).
/// Call QuestManager.Instance.SetQuest(Quest) inside Trigger().
/// </summary>
public interface IQuestTrigger
{
    QuestData Quest { get; }
    void Trigger();
}
