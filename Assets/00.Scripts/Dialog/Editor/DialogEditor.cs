using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom editor window for creating Blue-Archive-style Dialog assets.
/// Open via: Tools > Dialog Editor
/// </summary>
public class DialogEditor : EditorWindow
{
    // ── Data ──────────────────────────────────────────────────────────────────

    string fileName = "NewDialog";

    class ChoiceData
    {
        public string text = "...";
        public Dialog nextDialog = null;
    }

    class LineData
    {
        public string speakerName = "";
        public Color namePlateColor = new Color(0.18f, 0.44f, 0.80f, 1f);
        public Sprite portrait = null;
        public DialogSide side = DialogSide.Left;
        public string text = "";
        public bool choicesFoldout = false;
        public List<ChoiceData> choices = new();
    }

    List<LineData> lines = new() { new LineData() };
    Vector2 scroll;

    static readonly Color CardColor    = new(0.18f, 0.18f, 0.18f, 0.30f);
    static readonly Color ChoiceColor  = new(0.12f, 0.22f, 0.12f, 0.40f);

    // ── Window ────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Dialog Editor")]
    public static void Open()
    {
        var win = GetWindow<DialogEditor>("Dialog Editor");
        win.minSize = new Vector2(500, 400);
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawHeader();
        EditorGUILayout.Space(8);
        DrawLines();
        EditorGUILayout.Space(12);
        DrawFooter();

        EditorGUILayout.EndScrollView();
    }

    // ── Sections ──────────────────────────────────────────────────────────────

    void DrawHeader()
    {
        EditorGUILayout.LabelField("Dialog Asset", EditorStyles.boldLabel);
        fileName = EditorGUILayout.TextField("File Name", fileName);
    }

    void DrawLines()
    {
        EditorGUILayout.LabelField("Lines", EditorStyles.boldLabel);

        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            var rect = EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.DrawRect(rect, CardColor);

            // ── Header row ──
            EditorGUILayout.BeginHorizontal();

            // Move up / down
            GUI.enabled = i > 0;
            if (GUILayout.Button("▲", GUILayout.Width(22)))
            { (lines[i - 1], lines[i]) = (lines[i], lines[i - 1]); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            GUI.enabled = i < lines.Count - 1;
            if (GUILayout.Button("▼", GUILayout.Width(22)))
            { (lines[i + 1], lines[i]) = (lines[i], lines[i + 1]); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            GUI.enabled = true;

            EditorGUILayout.LabelField($"Line {i + 1}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            GUI.enabled = lines.Count > 1;
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                lines.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
                return;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // ── Speaker ──
            EditorGUILayout.BeginHorizontal();
            line.speakerName = EditorGUILayout.TextField("Speaker", line.speakerName, GUILayout.ExpandWidth(true));
            line.namePlateColor = EditorGUILayout.ColorField(GUIContent.none, line.namePlateColor, GUILayout.Width(48));
            EditorGUILayout.EndHorizontal();

            // ── Portrait + side ──
            EditorGUILayout.BeginHorizontal();
            line.portrait = (Sprite)EditorGUILayout.ObjectField("Portrait", line.portrait, typeof(Sprite), false);
            line.side = (DialogSide)EditorGUILayout.EnumPopup(GUIContent.none, line.side, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            // ── Text ──
            EditorGUILayout.LabelField("Text");
            line.text = EditorGUILayout.TextArea(line.text, GUILayout.MinHeight(52));

            EditorGUILayout.Space(4);

            // ── Choices foldout ──
            DrawChoices(line);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        if (GUILayout.Button("+ Add Line", GUILayout.Height(26)))
        {
            var prev = lines[lines.Count - 1];
            lines.Add(new LineData
            {
                speakerName = prev.speakerName,
                namePlateColor = prev.namePlateColor,
                side = prev.side
            });
        }
    }

    void DrawChoices(LineData line)
    {
        line.choicesFoldout = EditorGUILayout.Foldout(
            line.choicesFoldout,
            line.choices.Count > 0 ? $"Choices  ({line.choices.Count})" : "Choices",
            true);

        if (!line.choicesFoldout) return;

        EditorGUI.indentLevel++;

        for (int c = 0; c < line.choices.Count; c++)
        {
            var choice = line.choices[c];

            var rect = EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.DrawRect(rect, ChoiceColor);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Choice {c + 1}", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("✕", GUILayout.Width(22)))
            {
                line.choices.RemoveAt(c);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                break;
            }
            EditorGUILayout.EndHorizontal();

            choice.text = EditorGUILayout.TextField("Label", choice.text);
            choice.nextDialog = (Dialog)EditorGUILayout.ObjectField(
                new GUIContent("Next Dialog", "Dialog to open when picked. Leave empty to close."),
                choice.nextDialog, typeof(Dialog), false);

            if (choice.nextDialog == null)
            {
                EditorGUILayout.HelpBox("No Next Dialog → closes the conversation.", MessageType.None);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(EditorGUI.indentLevel * 15 + 4);
        if (GUILayout.Button("+ Add Choice", GUILayout.Height(22)))
            line.choices.Add(new ChoiceData());
        EditorGUILayout.EndHorizontal();
    }

    void DrawFooter()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear", GUILayout.Height(28)))
            ResetForm();

        if (GUILayout.Button("Create Dialog", GUILayout.Height(28)))
            CreateAsset();

        EditorGUILayout.EndHorizontal();
    }

    // ── Logic ─────────────────────────────────────────────────────────────────

    void CreateAsset()
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            EditorUtility.DisplayDialog("Dialog Editor", "File name cannot be empty.", "OK");
            return;
        }

        string folder = "Assets/05.Dialogs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "05.Dialogs");

        string path = $"{folder}/{fileName}.asset";

        if (File.Exists(Path.Combine(Application.dataPath, $"../{path}")))
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Dialog Editor",
                $"'{path}' already exists. Overwrite?",
                "Overwrite", "Cancel");
            if (!overwrite) return;
        }

        var asset = CreateInstance<Dialog>();
        asset.lines = new DialogLine[lines.Count];

        for (int i = 0; i < lines.Count; i++)
        {
            var src = lines[i];
            var dlg = new DialogLine
            {
                speakerName    = src.speakerName,
                namePlateColor = src.namePlateColor,
                portrait       = src.portrait,
                side           = src.side,
                text           = src.text,
                choices        = new DialogChoice[src.choices.Count]
            };

            for (int c = 0; c < src.choices.Count; c++)
            {
                dlg.choices[c] = new DialogChoice
                {
                    text       = src.choices[c].text,
                    nextDialog = src.choices[c].nextDialog
                };
            }

            asset.lines[i] = dlg;
        }

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        Debug.Log($"[DialogEditor] Created '{path}'");
    }

    void ResetForm()
    {
        fileName = "NewDialog";
        lines = new List<LineData> { new LineData() };
    }
}
