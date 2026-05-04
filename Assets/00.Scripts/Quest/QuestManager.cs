using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static QuestManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Tooltip("All QuestData assets in the project. Required for save/load lookup.")]
    [SerializeField] QuestData[] _registry;

    // ── Events ────────────────────────────────────────────────────────────────

    public static event Action OnQuestsChanged;

    // ── State ─────────────────────────────────────────────────────────────────

    readonly List<QuestData> _active    = new();
    readonly HashSet<string> _completed = new();

    public IReadOnlyList<QuestData> ActiveQuests     => _active;
    public IEnumerable<string>      CompletedQuestIds => _completed;

    // ── Public API ────────────────────────────────────────────────────────────

    public bool IsActive(QuestData quest)    => _active.Contains(quest);
    public bool IsCompleted(QuestData quest) => _completed.Contains(quest.questId);

    public void AddQuest(QuestData quest)
    {
        if (quest == null || _active.Contains(quest)) return;
        _active.Add(quest);
        OnQuestsChanged?.Invoke();
    }

    public void CompleteQuest(QuestData quest)
    {
        if (quest == null) return;
        _active.Remove(quest);
        _completed.Add(quest.questId);
        OnQuestsChanged?.Invoke();
    }

    public void RemoveQuest(QuestData quest)
    {
        if (_active.Remove(quest))
            OnQuestsChanged?.Invoke();
    }

    // ── Save / Load ───────────────────────────────────────────────────────────

    public void LoadFromSave(List<string> activeIds, List<string> completedIds)
    {
        _active.Clear();
        _completed.Clear();

        foreach (var id in completedIds)
            _completed.Add(id);

        foreach (var id in activeIds)
        {
            var quest = FindById(id);
            if (quest != null) _active.Add(quest);
            else Debug.LogWarning($"[QuestManager] No quest found for id '{id}' — skipped.");
        }

        OnQuestsChanged?.Invoke();
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    QuestData FindById(string id)
    {
        if (_registry == null) return null;
        foreach (var q in _registry)
            if (q != null && q.questId == id) return q;
        return null;
    }
}
