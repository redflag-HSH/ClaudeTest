using UnityEngine;
using UnityEngine.UI;

public class QuickSlotButton : MonoBehaviour
{
    [SerializeField] int slotIndex; // 0, 1, or 2
    [SerializeField] Image icon;

    void OnEnable()  => ItemConsumer.OnQuickSlotsChanged += Refresh;
    void OnDisable() => ItemConsumer.OnQuickSlotsChanged -= Refresh;

    public void OnClick()
    {
        if (ItemSelectionManager.Instance != null && ItemSelectionManager.Instance.HasSelection)
            ItemSelectionManager.Instance.AssignToQuickSlot(slotIndex);
        else
            PlayerControl.Instance?.GetComponent<ItemConsumer>()?.SetQuickSlot(slotIndex, null);

        Refresh();
    }

    public void Refresh()
    {
        ItemConsumer consumer = PlayerControl.Instance?.GetComponent<ItemConsumer>();
        if (consumer == null) return;

        Inventory.InventorySlot slot = consumer.GetQuickSlot(slotIndex);

        if (icon != null)
        {
            icon.sprite = slot?.icon;
            icon.enabled = slot?.icon != null;
        }
    }
}
