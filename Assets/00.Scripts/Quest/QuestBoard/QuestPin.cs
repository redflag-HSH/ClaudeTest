using UnityEngine;

public class QuestPin : DialogTrigger
{
    [Tooltip("Quest this pin represents. Set by QuestBoard when the pin is spawned.")]
    public QuestData quest;

    void OnEnable() => QuestManager.OnQuestsChanged += HandleQuestsChanged;
    void OnDisable() => QuestManager.OnQuestsChanged -= HandleQuestsChanged;

    void HandleQuestsChanged()
    {
        // Quest left the available list (accepted or removed) — this pin is stale.
        if (quest == null || QuestManager.Instance == null) return;
        if (!QuestManager.Instance.IsAvailable(quest))
            Destroy(gameObject);
    }

    public void PinClicked()
    {
        if (DialogSystem.Instance == null) return;

        if (DialogSystem.Instance.IsOpen)
            DialogSystem.Instance.Advance();
        else
            // The quest board disabled input and stays open after this dialog —
            // it re-enables input itself when closed (Show(false)).
            DialogSystem.Instance.Open(SelectDialog(), restoreInput: false);
    }
}
