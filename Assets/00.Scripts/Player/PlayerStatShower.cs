using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ── Setup ─────────────────────────────────────────────────────────────────────
// Create a Canvas (Screen Space – Overlay) with two Slider groups, e.g.:
//
//   Canvas
//   └─ StatsPanel (anchored bottom-left)
//      ├─ HP Row
//      │  ├─ HPSlider     (Slider – disable interactable, set Min=0 Max=1)
//      │  └─ HPText       (TextMeshProUGUI)  ← optional
//      └─ Stamina Row
//         ├─ StaminaSlider (Slider – same setup)
//         └─ StaminaText   (TextMeshProUGUI)  ← optional
//
// To color the fill: Slider > Fill Area > Fill (Image) – set the color there,
// or leave it to this script via the Fill image reference on the Slider.
// Assign each field in the Inspector. Text fields are optional.
// ─────────────────────────────────────────────────────────────────────────────

public class PlayerStatShower : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerControl combat;

    [Header("HP Bar")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private TextMeshProUGUI hpText;       // optional – shows "75 / 100"

    [Header("Stamina Bar")]
    [SerializeField] private Slider staminaBar;
    [SerializeField] private TextMeshProUGUI staminaText;  // optional – shows "60 / 100"

    [Header("Blood Gage Bar")]
    [SerializeField] private Slider bloodGageBar;
    [SerializeField] private TextMeshProUGUI bloodGageText;  // optional – shows "50 / 100"

    [Header("Blood Money")]
    [SerializeField] private TextMeshProUGUI bloodMoneyText;  // shows "NNN L"

    [Header("Low-Stat Flash")]
    [SerializeField, Range(0f, 1f)] private float lowHpThreshold = 0.22f;
    [SerializeField, Range(0f, 1f)] private float lowStaminaThreshold = 0.20f;
    [SerializeField] private Color hpNormalColor = new Color(0.85f, 0.15f, 0.15f);  // red
    [SerializeField] private Color hpLowColor = new Color(1.00f, 0.30f, 0.00f);  // orange
    [SerializeField] private Color staminaFullColor = new Color(0.10f, 0.80f, 0.20f);  // green
    [SerializeField] private Color staminaLowColor = new Color(0.50f, 0.50f, 0.50f);  // grey
    [SerializeField] private Color bloodGageColor = new Color(0.35f, 0.02f, 0.02f);  // dark red
    [SerializeField] private float flashSpeed = 4f;

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Start()
    {
        combat ??= PlayerControl.Instance;

        if (combat == null)
            Debug.LogWarning("PlayerStatShower: no PlayerControl found.", this);
    }

    void Update()
    {
        if (combat == null) return;

        float hpRatio    = combat.maxHp > 0f          ? combat.CurrentHp       / combat.maxHp          : 0f;
        float stRatio    = combat.maxStamina > 0f     ? combat.CurrentStamina  / combat.maxStamina     : 0f;
        float bloodRatio = combat.maxBloodGage > 0f   ? combat.CurrentBloodGage / combat.maxBloodGage  : 0f;

        UpdateBar(hpBar,       hpRatio,    hpNormalColor,   hpLowColor,   lowHpThreshold);
        UpdateBar(staminaBar,  stRatio,    staminaFullColor, staminaLowColor, lowStaminaThreshold);
        UpdateBloodBar(bloodRatio);

        UpdateText(hpText,        combat.CurrentHp,       combat.maxHp);
        UpdateText(staminaText,   combat.CurrentStamina,  combat.maxStamina);
        UpdateText(bloodGageText, combat.CurrentBloodGage, combat.maxBloodGage);

        if (bloodMoneyText != null)
            bloodMoneyText.text = $"{combat.CurrentBloodMoney} L";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    void UpdateBar(Slider bar, float ratio, Color normalColor, Color lowColor, float threshold)
    {
        if (bar == null) return;

        bar.value = ratio;

        Image fill = bar.fillRect != null ? bar.fillRect.GetComponent<Image>() : null;
        if (fill == null) return;

        if (ratio <= threshold)
        {
            float t = Mathf.Abs(Mathf.Sin(Time.time * flashSpeed));
            fill.color = Color.Lerp(lowColor, normalColor, t);
        }
        else
        {
            fill.color = normalColor;
        }
    }

    void UpdateBloodBar(float ratio)
    {
        if (bloodGageBar == null) return;
        bloodGageBar.value = ratio;
        Image fill = bloodGageBar.fillRect != null ? bloodGageBar.fillRect.GetComponent<Image>() : null;
        if (fill != null) fill.color = bloodGageColor;
    }

    void UpdateText(TextMeshProUGUI label, float current, float max)
    {
        if (label == null) return;
        label.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
