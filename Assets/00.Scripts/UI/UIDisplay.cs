using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDisplay : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;
    public GameObject slotPrefab;


    [Header("UI Panels")]
    public GameObject PanelContainer; // Parent object for all panels (optional)
    public GameObject inventoryPanel;
    public GameObject savePanel;
    [Header("InventoryUI Elements")]
    public Transform slotContainer;   // ScrollView > Viewport > Content
    public ScrollRect scrollView;
    public GridLayoutGroup gridLayout;
    public TextMeshProUGUI itemCountText;

    [Header("InventoryGrid")]
    public int columnCount = 4;
    public Vector2 cellSize = new(80f, 80f);
    public Vector2 spacing = new(8f, 8f);

    private bool inventoryOpen = false;
    private bool savePanelOpen = false;
    private bool PanelOpen = false;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (inventory == null)
            inventory = PlayerControl.Instance.GetComponent<Inventory>();

        inventory.onItemAdded.AddListener(_ => RefreshDisplay());
        inventory.onItemRemoved.AddListener(_ => RefreshDisplay());

        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = columnCount;
            gridLayout.cellSize = cellSize;
            gridLayout.spacing = spacing;
        }

        CloseInventory();
        CloseSavePanel();
        ClosePanel();
    }

    // ── Inventory Toggle (for buttons) ───────────────────────────────────────

    public void ToggleInventory()
    {
        if (inventoryOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        closeAllPanels();
        inventoryOpen = true;
        inventoryPanel.SetActive(true);
        RefreshDisplay();
    }

    public void CloseInventory()
    {
        inventoryOpen = false;
        inventoryPanel.SetActive(false);
    }

    // ── Save Panel Toggle (for buttons) ─────────────────────────────────────

    public void ToggleSavePanel()
    {
        if (savePanelOpen) CloseSavePanel();
        else OpenSavePanel();
    }

    public void OpenSavePanel()
    {
        closeAllPanels();
        savePanelOpen = true;
        if (savePanel != null)
            savePanel.SetActive(true);
    }

    public void CloseSavePanel()
    {
        savePanelOpen = false;
        if (savePanel != null)
            savePanel.SetActive(false);
    }

    // ── Panel Toggle (for buttons) ─────────────────────────────────────

    public void TogglePanel()
    {
        if (PanelOpen) ClosePanel();
        else OpenPanel();
    }

    public void OpenPanel()
    {
        PanelOpen = true;
        if (PanelContainer != null)
            PanelContainer.SetActive(true);
    }

    public void ClosePanel()
    {
        PanelOpen = false;
        if (PanelContainer != null)
            PanelContainer.SetActive(false);
    }

    void closeAllPanels()
    {
        CloseInventory();
        CloseSavePanel();
    }

    // ── Inventory Display ──────────────────────────────────────────────────────────────

    public void RefreshDisplay()
    {
        ClearSlots();

        IReadOnlyList<Inventory.InventorySlot> slots = inventory.Slots;

        foreach (Inventory.InventorySlot slot in slots)
            CreateSlotUI(slot);

        UpdateItemCount(slots.Count);

        if (scrollView != null)
            scrollView.verticalNormalizedPosition = 1f;
    }

    public void HighlightItem(string itemName)
    {
        foreach (Transform child in slotContainer)
        {
            bool match = child.name == itemName;
            child.GetComponent<Image>().color = match ? Color.yellow : Color.white;
        }
    }

    // ── Inventory Helpers ──────────────────────────────────────────────────────────────

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
