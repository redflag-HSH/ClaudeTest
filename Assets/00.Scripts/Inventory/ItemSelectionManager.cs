using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSelectionManager : MonoBehaviour
{
    public static ItemSelectionManager Instance { get; private set; }

    public Inventory.InventorySlot SelectedSlot { get; private set; }
    public bool HasSelection => SelectedSlot != null;

    bool _skipNextClick;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        if (!HasSelection) return;

        if (!Input.GetMouseButtonDown(0)) return;

        if (_skipNextClick) { _skipNextClick = false; return; }

        // If the clicked object is not a QuickSlotButton, clear selection
        GameObject clicked = EventSystem.current?.currentSelectedGameObject;
        if (clicked == null || clicked.GetComponent<QuickSlotButton>() == null)
            ClearSelection();
    }

    public void Select(Inventory.InventorySlot slot)
    {
        SelectedSlot = slot;
        _skipNextClick = true; // ignore the click that triggered this selection
    }

    public void AssignToQuickSlot(int index)
    {
        if (!HasSelection) return;
        PlayerControl.Instance?.GetComponent<ItemConsumer>()?.SetQuickSlot(index, SelectedSlot);
        ClearSelection();
    }

    public void ClearSelection()
    {
        SelectedSlot = null;
        _skipNextClick = false;
    }
}
