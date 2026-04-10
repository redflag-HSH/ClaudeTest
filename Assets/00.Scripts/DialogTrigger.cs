using UnityEngine;

/// <summary>
/// Attach to any NPC or object. Assign a Dialog asset.
/// When the player interacts, opens the dialog; on subsequent presses, advances it.
/// Requires the Interactor component on the player and the IInteractable interface.
/// </summary>
public class DialogTrigger : MonoBehaviour, IInteractable
{
    [Tooltip("The Dialog ScriptableObject to play when the player interacts.")]
    public Dialog dialog;

    public void Interact(GameObject interactor)
    {
        if (DialogSystem.Instance == null) return;

        if (DialogSystem.Instance.IsOpen)
            DialogSystem.Instance.Advance();
        else
            DialogSystem.Instance.Open(dialog);
    }
}
