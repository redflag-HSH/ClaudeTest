using UnityEngine;

public class QuestBoardInteractor : MonoBehaviour, IInteractable
{
    [Tooltip("Quest board to open. If empty, found automatically (including inactive) on Awake.")]
    [SerializeField] QuestBoard questBoard;

    void Awake()
    {
        // The board is usually inactive (Show(false) disables its GameObject),
        // so the search must include inactive objects.
        if (questBoard == null)
            questBoard = FindFirstObjectByType<QuestBoard>(FindObjectsInactive.Include);
    }

    void IInteractable.Interact(GameObject interactor)
    {
        if (questBoard == null)
        {
            Debug.LogWarning("[QuestBoardInteractor] No QuestBoard found in the scene.", this);
            return;
        }

        questBoard.Show(true);
        PlayerControl.Instance.SetInputEnabled(false);
    }
}
