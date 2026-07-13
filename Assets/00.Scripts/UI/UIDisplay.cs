using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;


public class UIDisplay : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;

    [Header("UI Panels")]
    public GameObject PanelContainer;
    public GameObject inventoryPanel;
    public GameObject skillPanel;
    public GameObject optionPanel;
    public GameObject quitPanel;
    public GameObject gameOverPanel;

    [Header("Inventory Slots")]
    public Transform[] slotPositions;   // pre-placed empty slot transforms in the UI
    public GameObject itemSlotPrefab; // prefab with InventoryItemButton component

    [Header("Quick Slots")]
    public QuickSlotButton[] quickSlotButtons = new QuickSlotButton[3];

    [Header("Other")]
    public Button quitPanelQuitButton;

    private bool PanelOpen = false;
    private int currentPanelIndex = 0;
    private GameObject[] panels;

    private readonly Dictionary<string, GameObject> _slotButtons = new();

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
        GameManager.OnPlayerDied += ShowGameOverScreen;
        quitPanelQuitButton.onClick.AddListener(() => GameManager.Instance.GoToMainMenu());
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        actions.Player2D.Escape.performed -= OnEscape;
        actions.Player2D.Move.performed -= OnMove;
        actions.Player2D.Disable();
        GameManager.OnPlayerDied -= ShowGameOverScreen;
        quitPanelQuitButton.onClick.RemoveListener(() => GameManager.Instance.GoToMainMenu());
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        PanelOpen = false;
        currentPanelIndex = 0;
        CloseAllPanels();
        HideGameOverScreen();
        if (PanelContainer != null) PanelContainer.SetActive(false);
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

    public void OpenInventory() => OpenMenuAtIndex(0);
    public void OpenSkillPanel() => OpenMenuAtIndex(1);
    public void OpenOptionPanel() => OpenMenuAtIndex(2);
    public void OpenQuitPanel() => OpenMenuAtIndex(3);

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
        if (skillPanel != null) skillPanel.SetActive(false);
        if (optionPanel != null) optionPanel.SetActive(false);
        if (quitPanel != null) quitPanel.SetActive(false);
    }

    // ── Inventory Display ─────────────────────────────────────────────────────

    public void RefreshDisplay()
    {
        ClearButtons();

        foreach (var slot in inventory.Slots)
        {
            if (slot.itemCode < 0 || slot.itemCode >= slotPositions.Length) continue;
            SpawnButton(slot, slotPositions[slot.itemCode]);
        }

        foreach (var qsb in quickSlotButtons)
            if (qsb != null) qsb.Refresh();
    }

    private void SpawnButton(Inventory.InventorySlot slot, Transform slotTransform)
    {
        if (itemSlotPrefab == null || slotTransform == null) return;

        GameObject btn = Instantiate(itemSlotPrefab, slotTransform);
        btn.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        btn.GetComponent<InventoryItemButton>()?.Setup(slot);
        _slotButtons[slot.itemName] = btn;
    }

    private void ClearButtons()
    {
        foreach (var btn in _slotButtons.Values)
            if (btn != null) Destroy(btn);
        _slotButtons.Clear();
    }
}
