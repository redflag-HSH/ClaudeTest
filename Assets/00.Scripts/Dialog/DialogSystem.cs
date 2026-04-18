using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Blue-Archive-style dialog system.
///
/// UI hierarchy to build in the Inspector:
///   Canvas
///   └─ DialogPanel
///      ├─ PortraitLeft   (Image)
///      ├─ PortraitRight  (Image)
///      └─ DialogBox      (bottom bar)
///         ├─ NamePlate   (Image)
///         │  └─ NameText (TextMeshProUGUI)
///         ├─ BodyText    (TextMeshProUGUI)
///         └─ NextIndicator (GameObject with Image — e.g. "▶")
/// </summary>
public class DialogSystem : MonoBehaviour
{
    public static DialogSystem Instance { get; private set; }

    [Header("Panel")]
    public GameObject dialogPanel;

    [Header("Portraits")]
    public DialogIlust dialogIlust;

    [Header("Name Plate")]
    public Image namePlateBackground;
    public TextMeshProUGUI nameText;

    [Header("Body")]
    public TextMeshProUGUI bodyText;

    [Header("Typewriter")]
    [Tooltip("Characters per second. 0 = instant.")]
    public float charsPerSecond = 40f;

    public bool IsOpen { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────────

    Dialog current;
    int lineIndex;
    bool isTyping;

    Coroutine typewriterRoutine;


    // ── Unity ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        dialogPanel.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Open(Dialog dialog)
    {
        if (dialog == null || dialog.lines == null || dialog.lines.Length == 0) return;

        current = dialog;
        lineIndex = 0;
        IsOpen = true;

        dialogPanel.SetActive(true);
        dialogIlust.ToggleShow();
        if (PlayerControl.Instance != null) PlayerControl.Instance.SetInputEnabled(false);
        ShowLine(0);
    }

    /// <summary>Press Interact to advance. First press skips typewriter; second advances.</summary>
    public void Advance()
    {
        if (!IsOpen) return;

        if (isTyping)
        {
            SkipTypewriter();
            return;
        }

        lineIndex++;
        if (lineIndex < current.lines.Length)
            ShowLine(lineIndex);
        else
            Close();
    }

    public void Close()
    {
        StopAllCoroutines();
        isTyping = false;
        IsOpen = false;
        current = null;
        dialogPanel.SetActive(false);
        dialogIlust.ToggleShow();
        if (PlayerControl.Instance != null) PlayerControl.Instance.SetInputEnabled(true);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    void ShowLine(int index)
    {
        DialogLine line = current.lines[index];

        // ── Portraits ──
        if (dialogIlust != null) dialogIlust.Apply(line);

        // ── Name plate ──
        bool hasName = !string.IsNullOrWhiteSpace(line.speakerName);
        if (nameText != null)
        {
            nameText.gameObject.SetActive(hasName);
            nameText.text = line.speakerName;
        }
        if (namePlateBackground != null)
            namePlateBackground.color = line.namePlateColor;

        // ── Typewriter ──
        if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
        if (charsPerSecond > 0f)
            typewriterRoutine = StartCoroutine(Typewriter(line.text));
        else
            bodyText.text = line.text;
    }

    void SkipTypewriter()
    {
        if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
        bodyText.text = current.lines[lineIndex].text;
        bodyText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator Typewriter(string line)
    {
        isTyping = true;
        bodyText.text = line;
        bodyText.maxVisibleCharacters = 0;
        bodyText.ForceMeshUpdate();

        int total = bodyText.textInfo.characterCount;
        float delay = 1f / charsPerSecond;

        for (int i = 0; i <= total; i++)
        {
            bodyText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
    }
}
