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
    public Image portraitLeft;
    public Image portraitRight;

    [Tooltip("Alpha of the portrait that is NOT speaking.")]
    [Range(0f, 1f)]
    public float dimmedAlpha = 0.45f;

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

    Dialog  current;
    int     lineIndex;
    bool    isTyping;

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

        current   = dialog;
        lineIndex = 0;
        IsOpen    = true;

        dialogPanel.SetActive(true);
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
        IsOpen   = false;
        current  = null;
        dialogPanel.SetActive(false);
        if (PlayerControl.Instance != null) PlayerControl.Instance.SetInputEnabled(true);
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    void ShowLine(int index)
    {
        DialogLine line = current.lines[index];

        // ── Portraits ──
        ApplyPortrait(line);

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

    void ApplyPortrait(DialogLine line)
    {
        bool leftSpeaking  = line.side == DialogSide.Left;
        bool rightSpeaking = line.side == DialogSide.Right;

        // Left portrait
        if (portraitLeft != null)
        {
            bool hasLeft = leftSpeaking && line.portrait != null;
            // Keep existing sprite visible but dimmed when the other side speaks
            if (leftSpeaking && line.portrait != null)
                portraitLeft.sprite = line.portrait;

            SetPortraitAlpha(portraitLeft, leftSpeaking ? 1f : dimmedAlpha);
        }

        // Right portrait
        if (portraitRight != null)
        {
            if (rightSpeaking && line.portrait != null)
                portraitRight.sprite = line.portrait;

            SetPortraitAlpha(portraitRight, rightSpeaking ? 1f : dimmedAlpha);
        }
    }

    static void SetPortraitAlpha(Image img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    void SkipTypewriter()
    {
        if (typewriterRoutine != null) StopCoroutine(typewriterRoutine);
        bodyText.text = current.lines[lineIndex].text;
        isTyping = false;
    }

    // ── Coroutines ────────────────────────────────────────────────────────────

    IEnumerator Typewriter(string line)
    {
        isTyping      = true;
        bodyText.text = string.Empty;

        float delay = 1f / charsPerSecond;
        foreach (char c in line)
        {
            bodyText.text += c;
            yield return new WaitForSeconds(delay);
        }

        isTyping = false;
    }
}
