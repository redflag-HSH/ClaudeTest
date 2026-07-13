using UnityEditor;
using UnityEngine;

/// <summary>
/// Slash skill level manager for PlayerSlashSkill components.
/// Open via: Tools > Slash Skill Level Editor
/// </summary>
public class PlayerSlashSkillLevelEditor : EditorWindow
{
    PlayerSlashSkill    _target;
    SlashSkillLevelData _data;
    Vector2             _scroll;
    bool                _editMode;
    SerializedObject    _dataSO;

    static readonly Color HeaderColor  = new(0.15f, 0.15f, 0.15f, 0.8f);
    static readonly Color UpgradeColor = new(0.20f, 0.55f, 0.20f, 1f);
    static readonly Color DownColor    = new(0.55f, 0.20f, 0.20f, 1f);
    static readonly Color DiffColor    = new(1f,    0.85f, 0.20f, 1f);

    [MenuItem("Tools/Slash Skill Level Editor")]
    public static void Open()
    {
        var win = GetWindow<PlayerSlashSkillLevelEditor>("Slash Skill Level Editor");
        win.minSize = new Vector2(560, 500);
    }

    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);

        _target = (PlayerSlashSkill)EditorGUILayout.ObjectField("Player Slash Skill", _target, typeof(PlayerSlashSkill), true);
        _data   = (SlashSkillLevelData)EditorGUILayout.ObjectField("Level Data", _data, typeof(SlashSkillLevelData), false);

        if (_target != null && _target.slashSkillLevelData != null && _data == null)
            _data = _target.slashSkillLevelData;

        EditorGUILayout.Space(6);

        if (_target == null)
        {
            EditorGUILayout.HelpBox("Drag a PlayerSlashSkill component from the scene.", MessageType.Info);
            return;
        }
        if (_data == null)
        {
            EditorGUILayout.HelpBox("Assign a SlashSkillLevelData asset (create one via Assets > Create > Player > Slash Skill Level Data).", MessageType.Warning);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        if (_editMode)
        {
            DrawEditLevelsMode();
        }
        else
        {
            // Row 1
            EditorGUILayout.BeginHorizontal();
            DrawQuickdrawPanel();
            GUILayout.Space(6);
            DrawRisingAttackPanel();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Row 2
            EditorGUILayout.BeginHorizontal();
            DrawSmashdownPanel();
            GUILayout.Space(6);
            DrawBodySlamPanel();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Row 3
            EditorGUILayout.BeginHorizontal();
            DrawGrabThrowPanel();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);

        if (GUILayout.Button("Apply All Levels to Component", GUILayout.Height(30)))
        {
            Undo.RecordObject(_target, "Apply All Slash Skill Levels");
            _target.slashSkillLevelData = _data;
            _target.ApplySlashLevels();
            EditorUtility.SetDirty(_target);
        }
    }

    // ── Edit Levels mode (raw array editing) ──────────────────────────────────

    void DrawEditLevelsMode()
    {
        if (_dataSO == null || _dataSO.targetObject != _data)
            _dataSO = new SerializedObject(_data);

        _dataSO.Update();

        DrawLevelArrayEditor("quickdrawLevels",    "Quickdraw");
        DrawLevelArrayEditor("risingAttackLevels", "Rising Attack");
        DrawLevelArrayEditor("smashdownLevels",    "Smashdown");
        DrawLevelArrayEditor("bodySlamLevels",     "Body Slam");
        DrawLevelArrayEditor("grabThrowLevels",    "Grab Throw");

        if (_dataSO.ApplyModifiedProperties())
            EditorUtility.SetDirty(_data);
    }

    void DrawLevelArrayEditor(string propertyName, string title)
    {
        var prop = _dataSO.FindProperty(propertyName);
        if (prop == null) return;

        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUILayout.PropertyField(prop, new GUIContent($"{title} Levels"), true);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    // ── Skill panels ──────────────────────────────────────────────────────────

    void DrawQuickdrawPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        int max = _data.MaxQuickdrawLevel;
        int cur = Mathf.Clamp(_target.quickdrawLevel, 0, max);
        DrawPanelHeader($"Quickdraw  —  Lv {cur + 1} / {max + 1}");

        var c = _data.GetQuickdraw(cur);
        var n = cur < max ? _data.GetQuickdraw(cur + 1) : null;

        DrawStatRow("Range",        c.range,       n?.range,       "0.0");
        DrawStatRow("Damage",       c.damage,      n?.damage,      "0.0");
        DrawStatRow("Stamina Cost", c.staminaCost, n?.staminaCost, "0.0");
        DrawStatRow("Cooldown (s)", c.cooldown,    n?.cooldown,    "0.00");

        EditorGUILayout.Space(6);
        DrawLevelButtons(cur, max, "Quickdraw", level =>
        {
            Undo.RecordObject(_target, "Change Quickdraw Level");
            _target.slashSkillLevelData = _data;
            _target.ApplyQuickdrawLevel(level);
            EditorUtility.SetDirty(_target);
        });

        EditorGUILayout.EndVertical();
    }

    void DrawRisingAttackPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        int max = _data.MaxRisingAttackLevel;
        int cur = Mathf.Clamp(_target.risingAttackLevel, 0, max);
        DrawPanelHeader($"Rising Attack  —  Lv {cur + 1} / {max + 1}");

        var c = _data.GetRisingAttack(cur);
        var n = cur < max ? _data.GetRisingAttack(cur + 1) : null;

        DrawStatRow("Damage",       c.damage,      n?.damage,      "0.0");
        DrawStatRow("Range",        c.range,       n?.range,       "0.0");
        DrawStatRow("Knockback",    c.knockback,   n?.knockback,   "0.0");
        DrawStatRow("Stamina Cost", c.staminaCost, n?.staminaCost, "0.0");
        DrawStatRow("Startup (s)",  c.startup,     n?.startup,     "0.00");
        DrawStatRow("Recovery (s)", c.recovery,    n?.recovery,    "0.00");

        EditorGUILayout.Space(6);
        DrawLevelButtons(cur, max, "Rising Attack", level =>
        {
            Undo.RecordObject(_target, "Change Rising Attack Level");
            _target.slashSkillLevelData = _data;
            _target.ApplyRisingAttackLevel(level);
            EditorUtility.SetDirty(_target);
        });

        EditorGUILayout.EndVertical();
    }

    void DrawSmashdownPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        int max = _data.MaxSmashdownLevel;
        int cur = Mathf.Clamp(_target.smashdownLevel, 0, max);
        DrawPanelHeader($"Smashdown  —  Lv {cur + 1} / {max + 1}");

        var c = _data.GetSmashdown(cur);
        var n = cur < max ? _data.GetSmashdown(cur + 1) : null;

        DrawStatRow("Damage",       c.damage,      n?.damage,      "0.0");
        DrawStatRow("Range",        c.range,       n?.range,       "0.0");
        DrawStatRow("Knockback",    c.knockback,   n?.knockback,   "0.0");
        DrawStatRow("Stamina Cost", c.staminaCost, n?.staminaCost, "0.0");
        DrawStatRow("Startup (s)",  c.startup,     n?.startup,     "0.00");
        DrawStatRow("Recovery (s)", c.recovery,    n?.recovery,    "0.00");
        DrawStatRow("Cooldown (s)", c.cooldown,    n?.cooldown,    "0.00");

        EditorGUILayout.Space(6);
        DrawLevelButtons(cur, max, "Smashdown", level =>
        {
            Undo.RecordObject(_target, "Change Smashdown Level");
            _target.slashSkillLevelData = _data;
            _target.ApplySmashdownLevel(level);
            EditorUtility.SetDirty(_target);
        });

        EditorGUILayout.EndVertical();
    }

    void DrawBodySlamPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        int max = _data.MaxBodySlamLevel;
        int cur = Mathf.Clamp(_target.bodySlamLevel, 0, max);
        DrawPanelHeader($"Body Slam  —  Lv {cur + 1} / {max + 1}");

        var c = _data.GetBodySlam(cur);
        var n = cur < max ? _data.GetBodySlam(cur + 1) : null;

        DrawStatRow("Stamina Cost",    c.staminaCost,   n?.staminaCost,   "0.0");
        DrawStatRow("Slip Speed",      c.slipSpeed,     n?.slipSpeed,     "0.0");
        DrawStatRow("Duration (s)",    c.duration,      n?.duration,      "0.00");
        DrawStatRow("Hit Radius",      c.hitRadius,     n?.hitRadius,     "0.00");
        DrawStatRow("Damage",          c.damage,        n?.damage,        "0.0");
        DrawStatRow("Knockback",       c.knockback,     n?.knockback,     "0.0");
        DrawStatRow("Self Damage",     c.selfDamage,    n?.selfDamage,    "0.0");
        DrawStatRow("Self Knockback",  c.selfKnockback, n?.selfKnockback, "0.0");
        DrawStatRow("Cooldown (s)",    c.cooldown,      n?.cooldown,      "0.00");

        EditorGUILayout.Space(6);
        DrawLevelButtons(cur, max, "Body Slam", level =>
        {
            Undo.RecordObject(_target, "Change Body Slam Level");
            _target.slashSkillLevelData = _data;
            _target.ApplyBodySlamLevel(level);
            EditorUtility.SetDirty(_target);
        });

        EditorGUILayout.EndVertical();
    }

    void DrawGrabThrowPanel()
    {
        EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true));

        int max = _data.MaxGrabThrowLevel;
        int cur = Mathf.Clamp(_target.grabThrowLevel, 0, max);
        DrawPanelHeader($"Grab Throw  —  Lv {cur + 1} / {max + 1}");

        var c = _data.GetGrabThrow(cur);
        var n = cur < max ? _data.GetGrabThrow(cur + 1) : null;

        DrawStatRow("Stamina Cost", c.staminaCost, n?.staminaCost, "0.0");
        DrawStatRow("Cooldown (s)", c.cooldown,    n?.cooldown,    "0.00");

        EditorGUILayout.Space(6);
        DrawLevelButtons(cur, max, "Grab Throw", level =>
        {
            Undo.RecordObject(_target, "Change Grab Throw Level");
            _target.slashSkillLevelData = _data;
            _target.ApplyGrabThrowLevel(level);
            EditorUtility.SetDirty(_target);
        });

        EditorGUILayout.EndVertical();
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("Slash Skill Level Editor", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        _editMode = GUILayout.Toggle(_editMode, _editMode ? "Mode: Edit Levels" : "Mode: Level Up/Down",
            EditorStyles.toolbarButton, GUILayout.Width(140));
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
