using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class QuestTest : MonoBehaviour, IQuestTrigger, IInteractable
{
    [field: SerializeField] public QuestData Quest { get; private set; }

    public void Interact(GameObject interactor)
    {
        Trigger();
    }

    public void Trigger()
    {
        if (QuestManager.Instance.IsActive(Quest))
        {
            QuestManager.Instance.CompleteQuest(Quest);
            Debug.Log($"Quest '{Quest.text}' completed!");
        }
        else if (!QuestManager.Instance.IsCompleted(Quest))
        {
            QuestManager.Instance.AddQuest(Quest);
            Debug.Log($"Quest '{Quest.text}' added!");
        }
    }


}
