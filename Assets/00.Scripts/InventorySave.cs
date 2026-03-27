using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Save / load system for Inventory.
/// Can be placed on any GameObject – resolves Inventory via PlayerMovement.Instance.
///
/// Usage:
///   inventorySave.Save();   // write to disk
///   inventorySave.Load();   // read from disk and rebuild slots
///   inventorySave.Delete(); // wipe the save file
///
/// Sprites are restored by name via Resources.Load. Place your item sprites
/// inside a Resources folder and ensure the sprite asset name matches
/// InventorySlot.icon.name, or leave iconResourcePath blank to skip.
/// </summary>
public class InventorySave : MonoBehaviour
{
    // ── Serialisable data classes ────────────────────────────────────────────

    [Serializable]
    private class SlotData
    {
        public string itemName;
        public string description;
        public Item.ItemType itemType;
        public string iconResourcePath; // relative path inside a Resources folder
        public int quantity;
    }

    [Serializable]
    private class SaveData
    {
        public List<SlotData> slots = new();
    }

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Settings")]
    [Tooltip("File name written to Application.persistentDataPath.")]
    public string saveFileName = "inventory.json";

    [Tooltip("Automatically save when the application quits.")]
    public bool autoSaveOnQuit = true;

    [Tooltip("Automatically load when this component starts.")]
    public bool autoLoadOnStart = true;

    // ── Private ──────────────────────────────────────────────────────────────

    private Inventory _inventory;
    private string SavePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        saveFileName);

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Start()
    {
        _inventory = PlayerMovement.Instance.GetComponent<Inventory>();

        if (_inventory == null)
        {
            Debug.LogError("[InventorySave] Could not find Inventory on PlayerMovement.Instance.");
            return;
        }

        if (autoLoadOnStart)
            Load();
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnQuit)
            Save();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Serialise the current inventory to disk.</summary>
    public void Save()
    {
        SaveData data = new();

        foreach (Inventory.InventorySlot slot in _inventory.Slots)
        {
            data.slots.Add(new SlotData
            {
                itemName         = slot.itemName,
                description      = slot.description,
                itemType         = slot.itemType,
                iconResourcePath = slot.icon != null ? slot.icon.name : string.Empty,
                quantity         = slot.quantity,
            });
        }

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[InventorySave] Saved {data.slots.Count} slot(s) to {SavePath}");
    }

    /// <summary>
    /// Load inventory from disk, replacing current contents.
    /// Does nothing if no save file exists.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[InventorySave] No save file found – starting fresh.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        if (data == null)
        {
            Debug.LogWarning("[InventorySave] Save file could not be parsed.");
            return;
        }

        _inventory.ClearInventory();

        foreach (SlotData slotData in data.slots)
        {
            Sprite icon = string.IsNullOrEmpty(slotData.iconResourcePath)
                ? null
                : Resources.Load<Sprite>(slotData.iconResourcePath);

            _inventory.AddSlotDirect(new Inventory.InventorySlot
            {
                itemName    = slotData.itemName,
                description = slotData.description,
                itemType    = slotData.itemType,
                icon        = icon,
                quantity    = slotData.quantity,
            });
        }

        Debug.Log($"[InventorySave] Loaded {data.slots.Count} slot(s) from {SavePath}");
    }

    /// <summary>Delete the save file from disk.</summary>
    public void Delete()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log($"[InventorySave] Deleted save file at {SavePath}");
        }
    }

    /// <summary>Returns true if a save file exists on disk.</summary>
    public bool HasSave() => File.Exists(SavePath);
}