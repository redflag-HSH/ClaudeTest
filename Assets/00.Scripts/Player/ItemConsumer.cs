using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to the Player alongside Inventory and PlayerControl.
/// Register every IUsableItem MonoBehaviour in usableItemImpls.
/// Quick slots are assigned at runtime via SetQuickSlot().
/// Keys 1/2/3 are bound in 2DActions and routed here via PlayerControl.
/// </summary>
public class ItemConsumer : MonoBehaviour
{
    [Tooltip("Every MonoBehaviour that implements IUsableItem.")]
    [SerializeField] MonoBehaviour[] usableItemImpls = new MonoBehaviour[0];

    Inventory _inventory;
    PlayerControl _player;

    public static event Action OnQuickSlotsChanged;

    readonly Dictionary<int, IUsableItem> _registry = new();
    readonly Inventory.InventorySlot[] _quickSlots = new Inventory.InventorySlot[3];

    void Awake()
    {
        _inventory = GetComponent<Inventory>();
        _player    = GetComponent<PlayerControl>();

        foreach (var mono in usableItemImpls)
            if (mono is IUsableItem usable)
                _registry[usable.ItemCode] = usable;
    }

    // ── Quick Slot Assignment (called by ItemSelectionManager) ────────────────

    public void SetQuickSlot(int index, Inventory.InventorySlot slot)
    {
        if (index < 0 || index >= _quickSlots.Length) return;

        // Clear any other slot that already holds the same item
        if (slot != null)
            for (int i = 0; i < _quickSlots.Length; i++)
                if (i != index && _quickSlots[i] != null && _quickSlots[i].itemCode == slot.itemCode)
                    _quickSlots[i] = null;

        _quickSlots[index] = slot;
        OnQuickSlotsChanged?.Invoke();
    }

    public Inventory.InventorySlot GetQuickSlot(int index)
    {
        if (index < 0 || index >= _quickSlots.Length) return null;
        return _quickSlots[index];
    }

    // ── Use (called by PlayerControl on key 1/2/3) ────────────────────────────

    public void TryAutoUseSunCream()
    {
        foreach (var usable in _registry.Values)
        {
            if (!(usable is SunCream sunCream)) continue;
            if (!_inventory.HasItem(sunCream.ItemCode, sunCream.ItemName)) continue;
            _inventory.RemoveItem(sunCream.ItemCode, sunCream.ItemName);
            sunCream.OnUse(_player);
            return;
        }
    }

public void UseSlot(int index)
    {
        if (index < 0 || index >= _quickSlots.Length) return;

        Inventory.InventorySlot slot = _quickSlots[index];
        if (slot == null)
        {
            Debug.Log($"[ItemConsumer] Quick slot {index + 1} is empty.");
            return;
        }

        if (!_registry.TryGetValue(slot.itemCode, out IUsableItem usable))
        {
            // Fallback: match by name when itemCode is 0 or unregistered
            usable = null;
            foreach (var entry in _registry.Values)
                if (entry.ItemName == slot.itemName) { usable = entry; break; }

            if (usable == null)
            {
                Debug.LogWarning($"[ItemConsumer] No IUsableItem registered for code {slot.itemCode} or name '{slot.itemName}'.");
                return;
            }
        }

        if (!_inventory.HasItem(slot.itemCode, slot.itemName))
        {
            Debug.Log($"[ItemConsumer] '{slot.itemName}' not in inventory.");
            return;
        }

        if (usable.IsConsumable)
        {
            _inventory.RemoveItem(slot.itemCode, slot.itemName);
            usable.OnUse(_player);

            if (!_inventory.HasItem(slot.itemCode, slot.itemName))
            {
                _quickSlots[index] = null;
                OnQuickSlotsChanged?.Invoke();
            }
        }
        else
        {
            usable.OnUse(_player);
        }
    }
}
