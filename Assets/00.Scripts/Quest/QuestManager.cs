using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
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

    public static event Action OnQuestsChanged;

    private readonly List<QuestData> _active = new();
    private readonly HashSet<string> _completed = new();

    public IReadOnlyList<QuestData> ActiveQuests => _active;

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
        _completed.Add(quest.questId);
        OnQuestsChanged?.Invoke();
    }

    public void RemoveQuest(QuestData quest)
    {
        if (_active.Remove(quest))
            OnQuestsChanged?.Invoke();
    }
    public bool IsActive(QuestData quest)
    {
        return _active.Contains(quest);
    }
}
