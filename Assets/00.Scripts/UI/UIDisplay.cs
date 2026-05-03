using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class UIDisplay : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;
    public GameObject slotPrefab;

    [Header("UI Panels")]
    public GameObject PanelContainer;
    public GameObject inventoryPanel;
    public GameObject skillPanel;
    public GameObject optionPanel;
    public GameObject quitPanel;
    public GameObject gameOverPanel;

    [Header("InventoryUI Elements")]
    public Transform slotContainer;
    public ScrollRect scrollView;
    public GridLayoutGroup gridLayout;
    public TextMeshProUGUI itemCountText;
    public Button quitPanelQuitButton;

    [Header("InventoryGrid")]
    public int columnCount = 4;
    public Vector2 cellSize = new(80f, 80f);
    public Vector2 spacing = new(8f, 8f);

    private bool PanelOpen = false;
    private int currentPanelIndex = 0;
    private GameObject[] panels;

    _2DActions actions;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        actions = new _2DActions();
        panels = new GameObject[] { inventoryPanel, skillPanel, optionPanel, quitPanel };
    }

    void OnEnable()
    {
        actions.Player2D.Escape.performed += OnEscape;
        actions.Player2D.Move.performed += OnMove;
        actions.Player2D.Enable();
        GameManager.OnGameOver += ShowGameOverScreen;
        quitPanelQuitButton.onClick.AddListener(() => GameManager.Instance.GoToMainMenu());
    }

    void OnDisable()
    {
        actions.Player2D.Escape.performed -= OnEscape;
        actions.Player2D.Move.performed -= OnMove;
        actions.Player2D.Disable();
        GameManager.OnGameOver -= ShowGameOverScreen;
        quitPanelQuitButton.onClick.RemoveListener(() => GameManager.Instance.GoToMainMenu());
    }

    void OnEscape(InputAction.CallbackContext ctx) => ToggleMenu();

    void OnMove(InputAction.CallbackContext ctx)
    {
        if (!PanelOpen) return;

        float x = ctx.ReadValue<Vector2>().x;
        if (x > 0.5f) NavigatePanel(1);
        else if (x < -0.5f) NavigatePanel(-1);
    }

    void NavigatePanel(int dir)
    {
        currentPanelIndex = (currentPanelIndex + dir + panels.Length) % panels.Length;
        ShowPanel(currentPanelIndex);
    }

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

        panels = new GameObject[] { inventoryPanel, skillPanel, optionPanel, quitPanel };

        CloseAllPanels();
        HideGameOverScreen();
        CloseMenu();
    }

    // ── Menu Container ───────────────────────────────────────────────────────

    public void ToggleMenu()
    {
        if (PanelOpen) CloseMenu();
        else OpenMenu();
    }

    public void OpenMenu()
    {
        PanelOpen = true;
        Time.timeScale = 0f;
        if (PanelContainer != null) PanelContainer.SetActive(true);
        if (PlayerControl.Instance != null) PlayerControl.Instance.SetInputEnabled(false);
        ShowPanel(currentPanelIndex);
    }

    public void CloseMenu()
    {
        PanelOpen = false;
        Time.timeScale = 1f;
        CloseAllPanels();
        if (PanelContainer != null) PanelContainer.SetActive(false);
        if (PlayerControl.Instance != null) PlayerControl.Instance.SetInputEnabled(true);
    }

    // ── Panel Navigation ─────────────────────────────────────────────────────

    void ShowPanel(int index)
    {
        CloseAllPanels();
        if (panels[index] != null) panels[index].SetActive(true);
        if (index == 0) RefreshDisplay();
    }

    public void OpenInventory()  => OpenMenuAtIndex(0);
    public void OpenSkillPanel() => OpenMenuAtIndex(1);
    public void OpenOptionPanel()=> OpenMenuAtIndex(2);
    public void OpenQuitPanel()  => OpenMenuAtIndex(3);

    void OpenMenuAtIndex(int index)
    {
        currentPanelIndex = index;
        if (!PanelOpen) OpenMenu();
        else ShowPanel(index);
    }

    // ── Game Over Screen ─────────────────────────────────────────────────────

    public void ShowGameOverScreen()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void HideGameOverScreen()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    // ── Shared ───────────────────────────────────────────────────────────────

    public void CloseAllPanels()
    {
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (skillPanel != null)     skillPanel.SetActive(false);
        if (optionPanel != null)    optionPanel.SetActive(false);
        if (quitPanel != null)      quitPanel.SetActive(false);
    }

    // ── Inventory Display ─────────────────────────────────────────────────────

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

    // ── Inventory Helpers ─────────────────────────────────────────────────────

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
