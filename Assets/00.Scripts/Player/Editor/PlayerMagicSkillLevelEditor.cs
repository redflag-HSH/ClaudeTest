using UnityEditor;
using UnityEngine;

/// <summary>
/// Magic skill level manager for PlayerMagicSkill components.
/// Open via: Tools > Magic Skill Level Editor
/// </summary>
public class PlayerMagicSkillLevelEditor : EditorWindow
{
    PlayerMagicSkill    _target;
    MagicSkillLevelData _data;
    Vector2             _scroll;

    static readonly Color HeaderColor  = new(0.15f, 0.15f, 0.15f, 0.8f);
    static readonly Color UpgradeColor = new(0.20f, 0.55f, 0.20f, 1f);
    static readonly Color DownColor    = new(0.55f, 0.20f, 0.20f, 1f);
    static readonly Color DiffColor    = new(1f,    0.85f, 0.20f, 1f);

    [MenuItem("Tools/Magic Skill Level Editor")]
    public static void Open()
    {
        var win = GetWindow<PlayerMagicSkillLevelEditor>("Magic Skill Level Editor");
        win.minSize = new Vector2(560, 500);
    }

    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);

        _target = (PlayerMagicSkill)EditorGUILayout.ObjectField("Player Magic Skill", _target, typeof(PlayerMagicSkill), true);
        _data   = (MagicSkillLevelData)EditorGUILayout.ObjectField("Level Data", _data, typeof(MagicSkillLevelData), false);

        if (_target != null && _target.magicSkillLevelData != null && _data == null)
            _data = _target.magicSkillLevelData;

        EditorGUILayout.Space(6);

        if (_target == null)
        {
            EditorGUILayout.HelpBox("Drag a PlayerMagicSkill component from the scene.", MessageType.Info);
            return;
        }
        if (_data == null)
        {
            EditorGUILayout.HelpBox("Assign a MagicSkillLevelData asset (create one via Assets > Create > Player > Magic Skill Level Data).", MessageType.Warning);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        // Row 1 — implemented magic skills
        EditorGUILayout.BeginHorizontal();
        DrawBloodSpearPanel();
        GUILayout.Space(6);
        DrawDrainPanel();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Row 2
        EditorGUILayout.BeginHorizontal();
        DrawHedgehogPanel();
        GUILayout.Space(6);
        DrawBlankPanel("Eclipse",
            () => _target.eclipseLevel,
            v  => _target.ApplyEclipseLevel(v),
            _data.MaxEclipseLevel);
        GUILayout.Space(6);
        DrawBlankPanel("Daughter of Dragon",
            () => _target.daughterOfDragonLevel,
            v  => _target.ApplyDaughterOfDragonLevel(v),
            _data.MaxDaughterOfDragonLevel);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // Row 3
        EditorGUILayout.BeginHorizontal();
        DrawBlankPanel("Steel Blood",
            () => _target.steelBloodLevel,
            v  => _target.ApplySteelBloodLevel(v),
            _data.MaxSteelBloodLevel);
        GUILayout.Space(6);
        DrawBlankPanel("Heat Blood",
            () => _target.heatBloodLevel,
            v  => _target.ApplyHeatBloodLevel(v),
            _data.MaxHeatBloodLevel);
        GUILayout.Space(6);
        DrawBlankPanel("Cold Blood",
            () => _target.coldBloodLevel,
            v  => _target.ApplyColdBloodLevel(v),
            _data.MaxColdBloodLevel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);

        if (GUILayout.Button("Apply All Levels to Component", GUILayout.Height(30)))
        {
            Undo.RecordObject(_target, "Apply All Magic Skill Levels");
            _target.magicSkillLevelData = _data;
            _target.ApplyMagicLevels();
            EditorUtility.SetDirty(_target);
        }
    }

    // ── Implemented magic skill panels ────────────────────────────────────────

    void DrawBloodSpearPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        int max = _data.MaxBloodSpearLevel;
        int cur = Mathf.Clamp(_target.bloodSpearLevel, 0, max);
        DrawPanelHeader($"Blood Spear  —  Lv {cur + 1} / {max + 1}");

        var c = _data.GetBloodSpear(cur);
        var n = cur < max ? _data.GetBloodSpear(cur + 1) : null;

        DrawStatRow("Spear Count",      c.spearCount,        n?.spearCount,        "0");
        DrawStatRow("Hover Radius",     c.hoverRadius,       n?.hoverRadius,       "0.00");
        DrawStatRow("Damage",           c.spearDamage,       n?.spearDamage,       "0.0");
        DrawStatRow("Blood Cost/Spear", c.bloodCostPerSpear, n?.bloodCostPerSpear, "0.0");
        DrawStatRow("Interval (s)",     c.spearInterval,     n?.spearInterval,     "0.00");
        DrawStatRow("Max Spears",       c.maxSpearCount,     n?.maxSpearCount,     "0");

        EditorGUILayout.Space(6);
        DrawLevelButtons(cur, max, "Blood Spear", level =>
        {
            Undo.RecordObject(_target, "Change Blood Spear Level");
            _target.magicSkillLevelData = _data;
            _target.ApplyBloodSpearLevel(level);
            EditorUtility.SetDirty(_target);
        });

        EditorGUILayout.EndVertical();
    }

    void DrawDrainPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        int max = _data.MaxDrainLevel;
        int cur = Mathf.Clamp(_target.drainLevel, 0, max);
        DrawPanelHeader($"Drain  —  Lv {cur + 1} / {max + 1}");

        var c = _data.GetDrain(cur);
        var n = cur < max ? _data.GetDrain(cur + 1) : null;

        DrawStatRow("Range",        c.drainRange,       n?.drainRange,       "0.0");
        DrawStatRow("Damage",       c.drainDamage,      n?.drainDamage,      "0.0");
        DrawStatRow("Heal Ratio",   c.drainHealRatio,   n?.drainHealRatio,   "0.00");
        DrawStatRow("Blood Cost",   c.drainBloodCost,   n?.drainBloodCost,   "0.0");
        DrawStatRow("Cooldown (s)", c.drainCooldown,    n?.drainCooldown,    "0.0");
        DrawStatRow("Targets",      c.drainTargetCount, n?.drainTargetCount, "0");

        EditorGUILayout.Space(6);
        DrawLevelButtons(cur, max, "Drain", level =>
        {
            Undo.RecordObject(_target, "Change Drain Level");
            _target.magicSkillLevelData = _data;
            _target.ApplyDrainLevel(level);
            EditorUtility.SetDirty(_target);
        });

        EditorGUILayout.EndVertical();
    }

    void DrawHedgehogPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        int max = _data.MaxHedgehogLevel;
        int cur = Mathf.Clamp(_target.hedgehogLevel, 0, max);
        DrawPanelHeader($"Hedgehog  —  Lv {cur + 1} / {max + 1}");

        var c = _data.GetHedgehog(cur);
        var n = cur < max ? _data.GetHedgehog(cur + 1) : null;

        DrawStatRow("Range",          c.hedgehogRange,         n?.hedgehogRange,         "0.0");
        DrawStatRow("Damage",         c.hedgehogDamage,        n?.hedgehogDamage,        "0.0");
        DrawStatRow("Blood Cost",     c.hedgehogBloodCost,     n?.hedgehogBloodCost,     "0.0");
        DrawStatRow("Cooldown (s)",   c.hedgehogCooldown,      n?.hedgehogCooldown,      "0.0");
        DrawStatRow("Spike Speed",    c.hedgehogSpikeSpeed,    n?.hedgehogSpikeSpeed,    "0.0");
        DrawStatRow("Spike Lifetime", c.hedgehogSpikeLifetime, n?.hedgehogSpikeLifetime, "0.0");

        EditorGUILayout.Space(6);
        DrawLevelButtons(cur, max, "Hedgehog", level =>
        {
            Undo.RecordObject(_target, "Change Hedgehog Level");
            _target.magicSkillLevelData = _data;
            _target.ApplyHedgehogLevel(level);
            EditorUtility.SetDirty(_target);
        });

        EditorGUILayout.EndVertical();
    }

    // ── Blank panel (stats TBD) ───────────────────────────────────────────────

    void DrawBlankPanel(string title, System.Func<int> getLevel, System.Action<int> applyLevel, int max)
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        int cur = Mathf.Clamp(getLevel(), 0, max);
        DrawPanelHeader($"{title}  —  Lv {cur + 1} / {max + 1}");

        var prev = GUI.color;
        GUI.color = Color.gray;
        EditorGUILayout.LabelField("— stats not yet defined —", EditorStyles.centeredGreyMiniLabel);
        GUI.color = prev;

        EditorGUILayout.Space(4);
        DrawLevelButtons(cur, max, title, level =>
        {
            Undo.RecordObject(_target, $"Change {title} Level");
            _target.magicSkillLevelData = _data;
            applyLevel(level);
            EditorUtility.SetDirty(_target);
        });

        EditorGUILayout.EndVertical();
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Magic Skill Level Editor", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
            Repaint();
        EditorGUILayout.EndHorizontal();
    }

    void DrawPanelHeader(string title)
    {
        var rect = EditorGUILayout.GetControlRect(false, 22f);
        EditorGUI.DrawRect(rect, HeaderColor);
        rect.x += 4;
        EditorGUI.LabelField(rect, title, EditorStyles.boldLabel);
        EditorGUILayout.Space(2);
    }

    void DrawStatRow(string label, float cur, float? next, string fmt)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        EditorGUILayout.LabelField(cur.ToString(fmt), EditorStyles.boldLabel, GUILayout.Width(60));

        if (next.HasValue)
        {
            float diff     = next.Value - cur;
            string diffStr = (diff >= 0 ? "+" : "") + diff.ToString(fmt);
            var prev = GUI.color;
            GUI.color = diff > 0f ? DiffColor : diff < 0f ? new Color(0.7f, 0.9f, 1f) : Color.gray;
            EditorGUILayout.LabelField($"→ {next.Value.ToString(fmt)}  ({diffStr})", GUILayout.ExpandWidth(true));
            GUI.color = prev;
        }
        else
        {
            var prev = GUI.color;
            GUI.color = Color.gray;
            EditorGUILayout.LabelField("(max)", GUILayout.ExpandWidth(true));
            GUI.color = prev;
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawStatRow(string label, int cur, int? next, string fmt)
        => DrawStatRow(label, cur, (float?)next, fmt);

    void DrawLevelButtons(int current, int max, string label, System.Action<int> apply)
    {
        EditorGUILayout.BeginHorizontal();

        var prevBg = GUI.backgroundColor;

        GUI.backgroundColor = current > 0 ? DownColor : Color.gray;
        if (GUILayout.Button("▼ Downgrade", GUILayout.Height(26)) && current > 0)
            apply(current - 1);

        GUI.backgroundColor = current < max ? UpgradeColor : Color.gray;
        if (GUILayout.Button("▲ Upgrade", GUILayout.Height(26)) && current < max)
            apply(current + 1);

        GUI.backgroundColor = prevBg;
        EditorGUILayout.EndHorizontal();

        if (current >= max)
        {
            var prev = GUI.color;
            GUI.color = Color.gray;
            EditorGUILayout.LabelField($"{label} is at max level.", EditorStyles.centeredGreyMiniLabel);
            GUI.color = prev;
        }
    }
}
