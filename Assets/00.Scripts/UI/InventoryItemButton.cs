using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class InventoryItemButton : MonoBehaviour
{
    public enum ItemCategory { Item, Weapon, Equipment }

    public ItemCategory category;
    public Image icon;
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI quantityLabel;

    protected Inventory.InventorySlot _slot;

    public virtual void Setup(Inventory.InventorySlot slot)
    {
        _slot = slot;

        if (icon != null)
        {
            icon.sprite = slot.icon;
            icon.enabled = slot.icon != null;
        }

        if (nameLabel != null)
            nameLabel.text = slot.itemName;

        if (quantityLabel != null)
            quantityLabel.text = slot.quantity > 1 ? $"x{slot.quantity}" : "";
    }

    public virtual void Selected()
    {
        ItemSelectionManager.Instance?.Select(_slot);
    }
}
