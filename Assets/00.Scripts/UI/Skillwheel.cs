using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

// Dishonored-style radial skill selector.
//
// Triggered by the SkillChange action (R key / no gamepad default).
// Hold R → wheel opens + time slows. Move mouse away from center to
// hover a segment. Release R → commits selection, time resumes.
//
// ── Canvas setup (Screen Space – Overlay) ──────────────────────────────────
//   Skillwheel  (this MonoBehaviour + CanvasGroup)
//     SegmentsRoot
//       Segment_0 … Segment_N   Image, Filled / Radial360
//         fillAmount  = 1 / skillCount  (0.125 for 8 skills)
//         Z rotation  = i * (360 / skillCount)
//       Icon_0 … Icon_N         Image, centered on each segment (optional)
//     CenterPanel
//       CenterIcon   Image
//       SkillName    TextMeshProUGUI
//       SkillDesc    TextMeshProUGUI
//
// Assign segmentImages / segmentIcons in Inspector in the same order as
// PlayerMagicSkill.MagicSkillType (BloodSpear=0, Drain=1, Hedgehog=2 …).

public class Skillwheel : MonoBehaviour
{
    [Serializable]
    public struct SkillEntry
    {
        public string displayName;
        [TextArea(1, 3)] public string description;
        public Sprite icon;
    }

    // ── References ─────────────────────────────────────────────────────────────

    [Header("Player")]
    public PlayerMagicSkill magicSkill;

    [Header("Wheel UI")]
    public CanvasGroup canvasGroup;

    [Tooltip("One Image per skill, ordered by MagicSkillType enum value.")]
    public Image[] segmentImages;

    [Tooltip("Optional icon Images overlaid on each segment (same ordering).")]
    public Image[] segmentIcons;

    [Header("Center Panel")]
    public Image centerIcon;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescText;

    // ── Skill Data ─────────────────────────────────────────────────────────────

    [Header("Skill Data")]
    [Tooltip("Entries must match PlayerMagicSkill.MagicSkillType order.")]
    public SkillEntry[] skills = new SkillEntry[8];

    // ── Settings ───────────────────────────────────────────────────────────────

    [Header("Settings")]
    [Range(0f, 1f)]
    [Tooltip("Time scale while the wheel is open (0 = fully frozen).")]
    public float slowTimeScale = 0.08f;

    [Tooltip("Mouse/stick distance from center (pixels / 0-1 magnitude) before a segment is highlighted.")]
    public float innerDeadzone = 60f;

    public Color normalColor = new(0.15f, 0.15f, 0.15f, 0.80f);
    public Color hoverColor = new(0.90f, 0.60f, 0.10f, 0.95f);
    public Color activeColor = new(0.70f, 0.15f, 0.15f, 0.90f);

    // ── Private ────────────────────────────────────────────────────────────────

    int _skillCount;
    int _hoveredIndex = -1;
    int _activeIndex;
    bool _isOpen;

    // ── Unity ──────────────────────────────────────────────────────────────────

    void Awake()
    {
        int enumCount = Enum.GetValues(typeof(PlayerMagicSkill.MagicSkillType)).Length;
        _skillCount = segmentImages != null && segmentImages.Length > 0
            ? Mathf.Min(segmentImages.Length, skills.Length)
            : enumCount;

        if (magicSkill == null)
            magicSkill = FindFirstObjectByType<PlayerMagicSkill>();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        canvasGroup.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_isOpen)
            UpdateHover();
    }

    // ── Open / Close (called by PlayerControl) ────────────────────────────────

    public void Open()
    {
        if (magicSkill == null) return;

        _isOpen = true;
        _hoveredIndex = -1;
        _activeIndex = (int)magicSkill.currentMagicSkill;

        canvasGroup.gameObject.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        Time.timeScale = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * slowTimeScale;

        RefreshSegments();
        ShowCenterInfo(_activeIndex);
    }

    public void CommitClose() => Close(commit: true);

    void Close(bool commit)
    {
        if (!_isOpen) return;
        _isOpen = false;

        int enumCount = Enum.GetValues(typeof(PlayerMagicSkill.MagicSkillType)).Length;
        if (commit && _hoveredIndex >= 0 && _hoveredIndex < enumCount && magicSkill != null)
        {
            magicSkill.currentMagicSkill = (PlayerMagicSkill.MagicSkillType)_hoveredIndex;
            _activeIndex = _hoveredIndex;
        }

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (canvasGroup != null) canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
    }

    // ── Hover ──────────────────────────────────────────────────────────────────

    void UpdateHover()
    {
        Vector2 delta = GetSelectionDelta();
        int newHovered;

        if (delta.magnitude < innerDeadzone)
        {
            newHovered = -1;
        }
        else
        {
            // Clockwise from the top of the screen
            float angle = Mathf.Atan2(delta.x, delta.y) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            newHovered = Mathf.FloorToInt(angle / (360f / _skillCount)) % _skillCount;
        }

        if (newHovered == _hoveredIndex) return;

        _hoveredIndex = newHovered;
        RefreshSegments();
        ShowCenterInfo(_hoveredIndex >= 0 ? _hoveredIndex : _activeIndex);
    }

    // Returns a 2D direction vector for hover detection.
    // Mouse: offset from screen center in pixels.
    // Gamepad: right stick, scaled to pixel-equivalent range (×200).
    Vector2 GetSelectionDelta()
    {
        // Gamepad right stick takes priority when it has significant input
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            if (stick.magnitude > 0.25f)
                return stick * 200f;
        }

        if (Mouse.current == null) return Vector2.zero;

        Vector2 screenCenter = new(Screen.width * 0.5f, Screen.height * 0.5f);
        return Mouse.current.position.ReadValue() - screenCenter;
    }

    // ── UI Helpers ─────────────────────────────────────────────────────────────

    void RefreshSegments()
    {
        for (int i = 0; i < segmentImages.Length && i < _skillCount; i++)
        {
            if (segmentImages[i] == null) continue;

            segmentImages[i].color = i == _hoveredIndex ? hoverColor
                                   : i == _activeIndex ? activeColor
                                   : normalColor;
        }
    }

    void ShowCenterInfo(int index)
    {
        if (index < 0 || index >= skills.Length) return;

        var e = skills[index];
        if (skillNameText != null) skillNameText.text = e.displayName;
        if (skillDescText != null) skillDescText.text = e.description;
        if (centerIcon != null)
        {
            centerIcon.sprite = e.icon;
            centerIcon.enabled = e.icon != null;
        }
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void ForceClose() => Close(commit: false);
}
