using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Handles UI display for the Inventory component.
/// Attach to a UI panel. Assign the Inventory reference and slot prefab in the Inspector.
/// </summary>
public class InventoryDisplay : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;
    public GameObject slotPrefab;
    public Transform slotContainer;

    [Header("UI")]
    public GameObject inventoryPanel;
    public TextMeshProUGUI itemCountText;

    private bool isOpen = false;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (inventory == null)
            inventory = PlayerMovement.Instance.GetComponent<Inventory>();

        inventory.onItemAdded.AddListener(_ => RefreshDisplay());
        inventory.onItemRemoved.AddListener(_ => RefreshDisplay());

        CloseInventory();
    }

    // ── Toggle ───────────────────────────────────────────────────────────────

    /// <summary>Toggle the inventory panel open/closed.</summary>
    public void ToggleInventory()
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        isOpen = true;
        inventoryPanel.SetActive(true);
        RefreshDisplay();
    }

    public void CloseInventory()
    {
        isOpen = false;
        inventoryPanel.SetActive(false);
    }

    // ── Display ──────────────────────────────────────────────────────────────

    /// <summary>Rebuild the slot list from the current inventory state.</summary>
    public void RefreshDisplay()
    {
        ClearSlots();

        IReadOnlyList<Inventory.InventorySlot> slots = inventory.Slots;

        foreach (Inventory.InventorySlot slot in slots)
            CreateSlotUI(slot);

        UpdateItemCount(slots.Count);
    }

    /// <summary>Show a highlight or tooltip for a specific item by name.</summary>
    public void HighlightItem(string itemName)
    {
        foreach (Transform child in slotContainer)
        {
            bool match = child.name == itemName;
            child.GetComponent<Image>().color = match ? Color.yellow : Color.white;
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void CreateSlotUI(Inventory.InventorySlot slot)
    {
        GameObject slotObj = Instantiate(slotPrefab, slotContainer);
        slotObj.name = slot.itemName;

        Image icon = slotObj.transform.Find("Icon")?.GetComponent<Image>();
        if (icon != null)
        {
            icon.sprite = slot.icon;
            icon.enabled = slot.icon != null;
        }

        TextMeshProUGUI label = slotObj.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (label != null)
            label.text = slot.itemName;

        TextMeshProUGUI qty = slotObj.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
        if (qty != null)
            qty.text = slot.quantity > 1 ? $"x{slot.quantity}" : "";
    }

    private void ClearSlots()
    {
        foreach (Transform child in slotContainer)
            Destroy(child.gameObject);
    }

    private void UpdateItemCount(int count)
    {
        if (itemCountText != null)
            itemCountText.text = $"{count} / {inventory.maxSlots}";
    }
}
