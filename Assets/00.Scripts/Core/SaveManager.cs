using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central save / load system. Handles checkpoint position, player stats,
/// session progress, and which bonfires have been lit.
///
/// Usage:
///   SaveManager.Instance.Save();
///   SaveManager.Instance.Load();
///   SaveManager.Instance.ApplyToPlayer();  // call after scene load
/// </summary>
public class SaveManager : MonoBehaviour
{
    // ──────────────────────────────────────────────────────────────
    //  Singleton
    // ──────────────────────────────────────────────────────────────

    public static SaveManager Instance { get; private set; }

    // ──────────────────────────────────────────────────────────────
    //  Save Data
    // ──────────────────────────────────────────────────────────────

    [Serializable]
    public class CheckpointData
    {
        public string sceneName;
        public string bonfireId;
        public float posX;
        public float posY;
    }

    [Serializable]
    public class PlayerStatsData
    {
        public float currentHp;
        public float currentStamina;
        public float currentBloodGage;
    }

    [Serializable]
    public class SessionData
    {
        public int score;
        public int enemiesKilled;
        public int sliceKills;
        public int damageTaken;
        public int bloodCollected;
        public float playTime;
    }

    [Serializable]
    public class InventorySlotData
    {
        public string itemName;
        public string description;
        public Item.ItemType itemType;
        public string iconResourcePath;
        public int quantity;
    }

    [Serializable]
    public class GameSaveData
    {
        public CheckpointData checkpoint = new();
        public PlayerStatsData playerStats = new();
        public SessionData session = new();
        public List<string> litBonfires = new();
        public List<InventorySlotData> inventory = new();
    }

    // ──────────────────────────────────────────────────────────────
    //  Inspector
    // ──────────────────────────────────────────────────────────────

    [Header("Settings")]
    [Tooltip("File name written to Documents folder (same convention as InventorySave).")]
    [SerializeField] private string saveFileName = "gamesave.json";
    [Tooltip("Automatically load save data when this component starts.")]
    [SerializeField] private bool autoLoadOnStart = true;

    [Header("Game Over")]
    [Tooltip("Seconds after the Game Over screen before the last checkpoint is loaded.")]
    [SerializeField] private float checkpointLoadDelay = 3f;

    // ──────────────────────────────────────────────────────────────
    //  Public State
    // ──────────────────────────────────────────────────────────────

    public GameSaveData Data { get; private set; } = new();

    // ──────────────────────────────────────────────────────────────
    //  Private
    // ──────────────────────────────────────────────────────────────

    private bool _applyOnNextLoad;

    private string SavePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        saveFileName);

    // ──────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ──────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (autoLoadOnStart && Load())
            ApplyToPlayer();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameManager.OnGameOver    += OnGameOver;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameManager.OnGameOver    -= OnGameOver;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_applyOnNextLoad)
        {
            _applyOnNextLoad = false;
            ApplyToPlayer();
        }
    }

    private void OnGameOver()
    {
        if (HasSave())
            StartCoroutine(LoadCheckpointAfterDelay());
    }

    private IEnumerator LoadCheckpointAfterDelay()
    {
        yield return new WaitForSecondsRealtime(checkpointLoadDelay);
        _applyOnNextLoad = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(Data.checkpoint.sceneName);
    }

    // ──────────────────────────────────────────────────────────────
    //  Bonfire API
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by Bonfire on rest. Records the checkpoint and writes to disk.
    /// </summary>
    /// <summary>
    /// Loads save data then transitions to the saved checkpoint scene.
    /// OnSceneLoaded will call ApplyToPlayer() once the scene is ready.
    /// </summary>
    public void LoadGame()
    {
        if (!Load()) return;
        _applyOnNextLoad = true;
        Time.timeScale = 1f;
        GameManager.Instance.LoadScene(Data.checkpoint.sceneName);
    }

    public void SaveAtBonfire(Bonfire bonfire)
    {
        RecordCheckpoint(bonfire);
        RecordPlayerStats();
        RecordSession();
        RecordInventory();
        RecordLitBonfire(bonfire.BonfireId);
        WriteFile();

        Debug.Log($"[SaveManager] Saved at bonfire '{bonfire.BonfireId}' " +
                  $"pos=({bonfire.transform.position.x:F1}, {bonfire.transform.position.y:F1})");
    }

    // ──────────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────────

    public void Save()
    {
        RecordPlayerStats();
        RecordSession();
        RecordInventory();
        WriteFile();
    }

    public bool Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] No save file found.");
            return false;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData loaded = JsonUtility.FromJson<GameSaveData>(json);

        if (loaded == null)
        {
            Debug.LogWarning("[SaveManager] Save file could not be parsed.");
            return false;
        }

        Data = loaded;
        Debug.Log($"[SaveManager] Loaded save. Last bonfire: '{Data.checkpoint.bonfireId}' " +
                  $"scene: '{Data.checkpoint.sceneName}'");
        return true;
    }

    /// <summary>
    /// Moves the player to the last saved checkpoint position and restores stats.
    /// Call this after the scene has finished loading.
    /// </summary>
    public void ApplyToPlayer()
    {
        var player = PlayerControl.Instance;
        if (player == null) return;

        player.transform.position = new Vector2(Data.checkpoint.posX, Data.checkpoint.posY);
        player.Heal(Data.playerStats.currentHp - player.CurrentHp);
        player.RestoreStamina(Data.playerStats.currentStamina - player.CurrentStamina);
        player.AddBloodGage(Data.playerStats.currentBloodGage - player.CurrentBloodGage);
        ApplyInventory();
    }

    /// <summary>Returns true if the bonfire with the given ID was lit in the save.</summary>
    public bool IsBonfireLit(string bonfireId) =>
        Data.litBonfires.Contains(bonfireId);

    public void Delete()
    {
        if (!File.Exists(SavePath)) return;
        File.Delete(SavePath);
        Data = new GameSaveData();
        Debug.Log("[SaveManager] Save file deleted.");
    }

    public bool HasSave() => File.Exists(SavePath);

    // ──────────────────────────────────────────────────────────────
    //  Internal Helpers
    // ──────────────────────────────────────────────────────────────

    private void RecordCheckpoint(Bonfire bonfire)
    {
        Data.checkpoint.sceneName = SceneManager.GetActiveScene().name;
        Data.checkpoint.bonfireId = bonfire.BonfireId;
        Data.checkpoint.posX = bonfire.transform.position.x;
        Data.checkpoint.posY = bonfire.transform.position.y;
    }

    private void RecordPlayerStats()
    {
        var player = PlayerControl.Instance;
        if (player == null) return;

        Data.playerStats.currentHp = player.CurrentHp;
        Data.playerStats.currentStamina = player.CurrentStamina;
        Data.playerStats.currentBloodGage = player.CurrentBloodGage;
    }

    private void RecordSession()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        Data.session.enemiesKilled = gm.EnemiesKilled;
        Data.session.sliceKills = gm.SliceKills;
        Data.session.damageTaken = gm.DamageTaken;
        Data.session.bloodCollected = gm.BloodCollected;
        Data.session.playTime = gm.PlayTime;
    }

    private Inventory GetPlayerInventory()
    {
        var player = PlayerControl.Instance;
        return player != null ? player.GetComponent<Inventory>() : null;
    }

    private void RecordInventory()
    {
        var inventory = GetPlayerInventory();
        if (inventory == null) return;

        Data.inventory.Clear();
        foreach (var slot in inventory.Slots)
        {
            Data.inventory.Add(new InventorySlotData
            {
                itemName = slot.itemName,
                description = slot.description,
                itemType = slot.itemType,
                iconResourcePath = slot.icon != null ? slot.icon.name : string.Empty,
                quantity = slot.quantity,
            });
        }
    }

    private void ApplyInventory()
    {
        var inventory = GetPlayerInventory();
        if (inventory == null) return;

        inventory.ClearInventory();
        foreach (var slotData in Data.inventory)
        {
            Sprite icon = string.IsNullOrEmpty(slotData.iconResourcePath)
                ? null
                : Resources.Load<Sprite>(slotData.iconResourcePath);

            inventory.AddSlotDirect(new Inventory.InventorySlot
            {
                itemName = slotData.itemName,
                description = slotData.description,
                itemType = slotData.itemType,
                icon = icon,
                quantity = slotData.quantity,
            });
        }
    }

    private void RecordLitBonfire(string bonfireId)
    {
        if (!string.IsNullOrEmpty(bonfireId) && !Data.litBonfires.Contains(bonfireId))
            Data.litBonfires.Add(bonfireId);
    }

    private void WriteFile()
    {
        string json = JsonUtility.ToJson(Data, prettyPrint: true);
        File.WriteAllText(SavePath, json);
    }

    // ──────────────────────────────────────────────────────────────
    //  Editor Debug
    // ──────────────────────────────────────────────────────────────

#if UNITY_EDITOR
    [ContextMenu("Debug / Print Save Data")]
    private void Debug_Print() =>
        Debug.Log($"[SaveManager]\n{JsonUtility.ToJson(Data, prettyPrint: true)}");

    [ContextMenu("Debug / Delete Save")]
    private void Debug_Delete() => Delete();
#endif
}
