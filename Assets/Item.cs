using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A pickupable / usable item that implements IInteractable.
/// Attach to any GameObject with a Collider2D on the interact layer.
/// </summary>
public class Item : MonoBehaviour, IInteractable
{
    public enum ItemType { Collectible, Usable, QuestItem }

    [Header("Item Info")]
    public string itemName = "Item";
    [TextArea] public string description = "";
    public ItemType itemType = ItemType.Collectible;
    public Sprite icon;

    [Header("Behaviour")]
    [Tooltip("Destroy this GameObject after interaction.")]
    public bool destroyOnPickup = true;
    [Tooltip("Seconds before the item can be interacted with again (ignored if destroyOnPickup).")]
    public float cooldown = 0f;

    [Header("Events")]
    public UnityEvent<GameObject> onInteract;  // fired every interaction
    public UnityEvent onPickedUp;              // fired when collected (destroyOnPickup = true)

    [Header("Audio / Visual")]
    public AudioClip interactSound;
    public GameObject pickupEffectPrefab;      // optional VFX spawned on interact

    private float _nextInteractTime;
    private AudioSource _audio;

    void Awake()
    {
        _audio = GetComponent<AudioSource>();
    }

    // ── IInteractable ───────────────────────────────────────────────────────

    public void Interact(GameObject interactor)
    {
        if (Time.time < _nextInteractTime) return;
        _nextInteractTime = Time.time + cooldown;

        PlayFeedback();
        onInteract.Invoke(interactor);
        GetItem(interactor);

        if (destroyOnPickup)
        {
            onPickedUp.Invoke();
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds this item to the Inventory found on <paramref name="interactor"/>.
    /// Returns true if successfully added, false if no Inventory or inventory full.
    /// </summary>
    public bool GetItem(GameObject interactor)
    {
        if (!interactor.TryGetComponent(out Inventory inventory))
        {
            Debug.LogWarning($"[Item] {interactor.name} has no Inventory component.");
            return false;
        }

        return inventory.AddItem(this);
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private void PlayFeedback()
    {
        if (interactSound != null)
        {
            if (_audio != null)
                _audio.PlayOneShot(interactSound);
            else
                AudioSource.PlayClipAtPoint(interactSound, transform.position);
        }

        if (pickupEffectPrefab != null)
            Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
    }

    // ── Editor ──────────────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Gizmos.color = itemType switch
        {
            ItemType.QuestItem   => Color.yellow,
            ItemType.Usable      => Color.green,
            _                    => Color.white,
        };
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
