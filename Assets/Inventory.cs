using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple inventory system. Attach to the Player GameObject.
///
/// Hookup: On each Item in the scene, add a listener to its onInteract UnityEvent
/// and point it to this component's CollectItem(GameObject) method.
/// </summary>
public class Inventory : MonoBehaviour
{
    [System.Serializable]
    public class InventorySlot
    {
        public string itemName;
        public string description;
        public Item.ItemType itemType;
        public Sprite icon;
        public int quantity;

        public InventorySlot() { }

        public InventorySlot(Item item)
        {
            itemName    = item.itemName;
            description = item.description;
            itemType    = item.itemType;
            icon        = item.icon;
            quantity    = 1;
        }
    }

    [Header("Settings")]
    public int maxSlots = 20;

    [Header("Events")]
    public UnityEvent<InventorySlot> onItemAdded;
    public UnityEvent<InventorySlot> onItemRemoved;
    public UnityEvent onInventoryFull;

    [Header("Debug (read-only)")]
    [SerializeField] private List<InventorySlot> slots = new();

    public IReadOnlyList<InventorySlot> Slots => slots;

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Wire this to Item.onInteract in the Inspector (matches UnityEvent&lt;GameObject&gt;).
    /// </summary>
    public void CollectItem(GameObject itemObject)
    {
        if (itemObject.TryGetComponent(out Item item))
            AddItem(item);
    }

    /// <summary>Add an item directly.</summary>
    public bool AddItem(Item item)
    {
        // Stack existing slot of the same name
        InventorySlot existing = slots.Find(s => s.itemName == item.itemName);
        if (existing != null)
        {
            existing.quantity++;
            onItemAdded.Invoke(existing);
            return true;
        }

        if (slots.Count >= maxSlots)
        {
            Debug.Log($"[Inventory] Full – could not add {item.itemName}.");
            onInventoryFull.Invoke();
            return false;
        }

        InventorySlot newSlot = new(item);
        slots.Add(newSlot);
        onItemAdded.Invoke(newSlot);
        Debug.Log($"[Inventory] Added: {item.itemName} (x{newSlot.quantity})");
        return true;
    }

    /// <summary>Remove one of an item by name. Returns true if removed.</summary>
    public bool RemoveItem(string itemName)
    {
        InventorySlot slot = slots.Find(s => s.itemName == itemName);
        if (slot == null) return false;

        slot.quantity--;
        if (slot.quantity <= 0)
            slots.Remove(slot);

        onItemRemoved.Invoke(slot);
        Debug.Log($"[Inventory] Removed: {itemName}");
        return true;
    }

    /// <summary>
    /// Use a Usable item by name (removes one from inventory).
    /// Returns false if not found or wrong type.
    /// </summary>
    public bool UseItem(string itemName)
    {
        InventorySlot slot = slots.Find(s => s.itemName == itemName && s.itemType == Item.ItemType.Usable);
        if (slot == null)
        {
            Debug.Log($"[Inventory] Cannot use '{itemName}' – not found or not Usable.");
            return false;
        }

        Debug.Log($"[Inventory] Used: {itemName}");
        return RemoveItem(itemName);
    }

    /// <summary>Check if the inventory contains at least one of an item.</summary>
    public bool HasItem(string itemName) => slots.Exists(s => s.itemName == itemName);

    /// <summary>Total quantity of an item across all stacks.</summary>
    public int GetQuantity(string itemName)
    {
        InventorySlot slot = slots.Find(s => s.itemName == itemName);
        return slot?.quantity ?? 0;
    }

    // ── Save / load helpers (used by InventorySave) ──────────────────────────

    /// <summary>Remove all slots without firing events. Used by InventorySave.</summary>
    public void ClearInventory() => slots.Clear();

    /// <summary>Add a pre-built slot directly without firing events. Used by InventorySave.</summary>
    public void AddSlotDirect(InventorySlot slot) => slots.Add(slot);
}