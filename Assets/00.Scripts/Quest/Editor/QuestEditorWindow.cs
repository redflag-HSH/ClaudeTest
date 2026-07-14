using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Quest creation and browsing tool.
/// Open via: Tools > Quest Editor
/// </summary>
public class QuestEditorWindow : EditorWindow
{
    // ── Create form state ─────────────────────────────────────────────────────

    string _fileName = "NewQuest";
    string _questId = "";
    string _text = "";
    Vector2 _pinPosition = Vector2.zero;
    int _dialogID = 0;
    bool _repeatable = false;

    // ── Browser state ─────────────────────────────────────────────────────────

    List<QuestData> _allQuests = new();
    QuestData _selected = null;
    Vector2 _listScroll;
    Vector2 _formScroll;
    bool _browseMode = true;

    // ── Colors ────────────────────────────────────────────────────────────────

    static readonly Color SelectedColor = new(0.20f, 0.40f, 0.70f, 0.35f);

    // ── Window ────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Quest Editor")]
    public static void Open()
    {
        var win = GetWindow<QuestEditorWindow>("Quest Editor");
        win.minSize = new Vector2(540, 280);
    }

    void OnEnable() => RefreshList();

    void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        DrawListPanel();
        DrawSeparator();
        DrawRightPanel();
        EditorGUILayout.EndHorizontal();
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64)))
            RefreshList();

        GUILayout.FlexibleSpace();

        if (GUILayout.Toggle(!_browseMode, "New Quest", EditorStyles.toolbarButton, GUILayout.Width(80)))
            _browseMode = false;
        if (GUILayout.Toggle(_browseMode, "Browse", EditorStyles.toolbarButton, GUILayout.Width(64)))
            _browseMode = true;

        EditorGUILayout.EndHorizontal();
    }

    // ── Left: asset list ──────────────────────────────────────────────────────

    void DrawListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(180), GUILayout.ExpandHeight(true));
        EditorGUILayout.LabelField("Quests", EditorStyles.boldLabel);

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);

        foreach (var quest in _allQuests)
        {
            bool isSelected = quest == _selected;
            var rect = EditorGUILayout.BeginVertical(GUI.skin.box);

            if (isSelected) EditorGUI.DrawRect(rect, SelectedColor);

            string preview = string.IsNullOrEmpty(quest.text) ? "(no text)" : quest.text;
            if (preview.Length > 28) preview = preview[..28] + "…";
            EditorGUILayout.LabelField(preview, EditorStyles.miniLabel);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selected = quest;
                _browseMode = true;
                Repaint();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1);
        }

        if (_allQuests.Count == 0)
            EditorGUILayout.HelpBox("No quest assets found.", MessageType.None);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.3f));
    }

    // ── Right: info or create form ────────────────────────────────────────────

    void DrawRightPanel()
    {
        _formScroll = EditorGUILayout.BeginScrollView(_formScroll, GUILayout.ExpandWidth(true));

        if (_browseMode) DrawInfo();
        else DrawCreateForm();

        EditorGUILayout.EndScrollView();
    }

    void DrawInfo()
    {
        if (_selected == null)
        {
            EditorGUILayout.HelpBox("Select a quest on the left.", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField($"ID: {_selected.questId}", EditorStyles.miniLabel);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(_selected.text, EditorStyles.wordWrappedLabel);
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Pin Position: {_selected.pinPosition}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Dialog ID: {_selected.dialogID}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField(_selected.repeatable ? "Repeatable: yes" : "Repeatable: no (one-time)", EditorStyles.miniLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Select in Project", GUILayout.Height(26)))
        {
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = _selected;
        }

        if (GUILayout.Button("Delete", GUILayout.Height(26)))
        {
            if (EditorUtility.DisplayDialog("Quest Editor", $"Delete '{_selected.name}'?", "Delete", "Cancel"))
            {
                string path = AssetDatabase.GetAssetPath(_selected);
                AssetDatabase.DeleteAsset(path);
                _selected = null;
                RefreshList();
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    void DrawCreateForm()
    {
        EditorGUILayout.LabelField("New Quest", EditorStyles.boldLabel);
        _fileName = EditorGUILayout.TextField("File Name", _fileName);
        EditorGUILayout.Space(4);
        _questId = EditorGUILayout.TextField("Quest ID", _questId);
        EditorGUILayout.LabelField("Quest Text");
        _text = EditorGUILayout.TextArea(_text, GUILayout.MinHeight(60));
        EditorGUILayout.Space(4);
        _pinPosition = EditorGUILayout.Vector2Field("Pin Position", _pinPosition);
        _dialogID = EditorGUILayout.IntField("Dialog ID", _dialogID);
        _repeatable = EditorGUILayout.Toggle(
            new GUIContent("Repeatable", "If on, the quest can be acquired again after completion. Off = once only."),
            _repeatable);

        EditorGUILayout.Space(12);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear", GUILayout.Height(28)))
            ResetForm();

        if (GUILayout.Button("Create Quest Asset", GUILayout.Height(28)))
            CreateAsset();

        EditorGUILayout.EndHorizontal();
    }

    // ── Logic ─────────────────────────────────────────────────────────────────

    void RefreshList()
    {
        _allQuests.Clear();
        foreach (var guid in AssetDatabase.FindAssets("t:QuestData"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<QuestData>(path);
            if (asset != null) _allQuests.Add(asset);
        }
        _allQuests.Sort((a, b) => string.Compare(a.questId, b.questId, System.StringComparison.Ordinal));
        Repaint();
    }

    void CreateAsset()
    {
        if (string.IsNullOrWhiteSpace(_fileName))
        {
            EditorUtility.DisplayDialog("Quest Editor", "File name cannot be empty.", "OK");
            return;
        }

        string folder = "Assets/05.Quests";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "05.Quests");

        string path = $"{folder}/{_fileName}.asset";

        // Overwrite = update the existing asset in place. Recreating it with
        // CreateAsset destroys the loaded object mid-save (asserts) and breaks
        // references to it (QuestManager registry, dialog quest actions).
        QuestData existing = AssetDatabase.LoadAssetAtPath<QuestData>(path);
        if (existing != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Quest Editor", $"'{path}' already exists. Overwrite?", "Overwrite", "Cancel");
            if (!overwrite) return;
        }

        QuestData asset = existing != null ? existing : CreateInstance<QuestData>();
        asset.questId = _questId;
        asset.text = _text;
        asset.pinPosition = _pinPosition;
        asset.dialogID = _dialogID;
        asset.repeatable = _repeatable;

        if (existing != null)
            EditorUtility.SetDirty(asset);
        else
            AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        Debug.Log($"[QuestEditor] {(existing != null ? "Updated" : "Created")} '{path}'");

        RefreshList();
        _selected = asset;
        _browseMode = true;
    }

    void ResetForm()
    {
        _fileName = "NewQuest";
        _questId = "";
        _text = "";
        _pinPosition = Vector2.zero;
        _dialogID = 0;
        _repeatable = false;
    }
}
