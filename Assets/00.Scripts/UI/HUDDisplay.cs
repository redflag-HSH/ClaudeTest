using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDDisplay : MonoBehaviour
{
    public static HUDDisplay Instance { get; private set; }

    [Header("Combat Log")]
    public ScrollRect logScrollRect;
    public Transform logContainer;
    public GameObject logLinePrefab;
    public int maxLines = 20;
    public float lineDuration = 5f; // 0 = never fade

    [Header("Quest HUD")]
    public GameObject questHUD;
    public Transform questLineContainer;
    public GameObject questLinePrefab;

    private readonly Queue<GameObject> _logLines = new();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (logContainer != null)
        {
            RectTransform rt = logContainer as RectTransform;
            if (rt != null)
            {
                rt.anchorMin        = new Vector2(0f, 0f);
                rt.anchorMax        = new Vector2(1f, 0f);
                rt.pivot            = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta        = Vector2.zero;
            }

            if (!logContainer.TryGetComponent(out VerticalLayoutGroup vlg))
                vlg = logContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.LowerLeft;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;
            vlg.childForceExpandWidth  = false;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0f;

            if (!logContainer.TryGetComponent(out ContentSizeFitter csf))
                csf = logContainer.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    void OnEnable()
    {
        QuestManager.OnQuestsChanged += RefreshQuestDisplay;
    }

    void OnDisable()
    {
        QuestManager.OnQuestsChanged -= RefreshQuestDisplay;
    }

    void Start()
    {
        RefreshQuestDisplay();
    }

    // ── Combat Log ────────────────────────────────────────────────────────────

    public static void Log(string message) => Instance?.AddLine(message);

    void AddLine(string message)
    {
        GameObject row = Instantiate(logLinePrefab, logContainer);
        row.transform.SetAsLastSibling();

        if (!row.TryGetComponent(out ContentSizeFitter rowCsf))
            rowCsf = row.AddComponent<ContentSizeFitter>();
        rowCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        rowCsf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        if (row.TryGetComponent(out LayoutElement le))
        {
            le.minHeight       = -1f;
            le.preferredHeight = -1f;
        }

        row.transform.localScale = Vector3.one;

        TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
        if (label != null)
        {
            label.enableAutoSizing = false;
            label.fontSize         = 18f;
            label.margin           = Vector4.zero;
            label.text             = message;
        }

        _logLines.Enqueue(row);

        if (_logLines.Count > maxLines)
            Destroy(_logLines.Dequeue());

        if (lineDuration > 0f)
            StartCoroutine(FadeLine(row, label));

        StartCoroutine(ScrollLogToBottom());
    }

    IEnumerator ScrollLogToBottom()
    {
        if (logScrollRect == null) yield break;
        yield return new WaitForEndOfFrame();
        Canvas.ForceUpdateCanvases();
        logScrollRect.verticalNormalizedPosition = 0f;
    }

    IEnumerator FadeLine(GameObject row, TextMeshProUGUI label)
    {
        yield return new WaitForSeconds(lineDuration * 0.75f);

        float elapsed = 0f;
        float fadeDuration = lineDuration * 0.25f;
        Color original = label != null ? label.color : Color.white;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (label != null)
                label.color = Color.Lerp(original, Color.clear, elapsed / fadeDuration);
            yield return null;
        }

        if (row != null)
        {
            _logLines.TryDequeue(out _);
            Destroy(row);
        }
    }

    // ── Quest HUD ─────────────────────────────────────────────────────────────

    void RefreshQuestDisplay()
    {
        if (questHUD == null) return;

        if (questLineContainer != null)
        {
            foreach (Transform child in questLineContainer)
                Destroy(child.gameObject);
        }

        var quests = QuestManager.Instance != null ? QuestManager.Instance.ActiveQuests : null;
        bool hasAny = quests != null && quests.Count > 0;
        questHUD.SetActive(hasAny);

        if (!hasAny || questLinePrefab == null) return;

        foreach (QuestData quest in quests)
        {
            GameObject row = Instantiate(questLinePrefab, questLineContainer);
            TextMeshProUGUI label = row.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                bool done = QuestManager.Instance.IsCompleted(quest);
                string tag = done ? "<sprite name=\"checked\">" : "<sprite name=\"unchecked\">";
                label.text = $"{tag} {quest.text}";
            }
        }
    }
}
