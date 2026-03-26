using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player interaction with nearby IInteractable objects.
/// Attach to the player. Assign an interactRange and interactLayer.
/// Requires a '2DInteractor' action map with an 'Interact' action in your Input Actions asset.
/// </summary>
public class Interactor : MonoBehaviour
{
    [Header("Settings")]
    public float interactRange = 1.5f;
    public LayerMask interactLayer;

    [Header("Visual Feedback")]
    public GameObject interactPrompt; // optional UI prompt (e.g. "Press E")

    private IInteractable currentTarget;
    private _2DActions actions;

    void Awake()
    {
        actions = new _2DActions();
    }

    void OnEnable()
    {
        actions.Player2D.Interact.performed += OnInteract;
        actions.Enable();
    }

    void OnDisable()
    {
        actions.Player2D.Interact.performed -= OnInteract;
        actions.Disable();
    }

    void Update()
    {
        FindTarget();

        if (interactPrompt != null)
            interactPrompt.SetActive(currentTarget != null);
    }

    void OnInteract(InputAction.CallbackContext _)
    {
        currentTarget?.Interact(gameObject);
    }

    void FindTarget()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, interactLayer);

        if (hit != null && hit.TryGetComponent(out IInteractable interactable))
            currentTarget = interactable;
        else
            currentTarget = null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

/// <summary>
/// Implement this interface on any object the player can interact with.
/// </summary>
public interface IInteractable
{
    void Interact(GameObject interactor);
}