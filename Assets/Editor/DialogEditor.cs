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

    class LineData
    {
        public string      speakerName    = "";
        public Color       namePlateColor = new Color(0.18f, 0.44f, 0.80f, 1f);
        public Sprite      portrait       = null;
        public DialogSide  side           = DialogSide.Left;
        public string      text           = "";
    }

    List<LineData> lines = new() { new LineData() };
    Vector2 scroll;

    // ── Window ────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Dialog Editor")]
    public static void Open()
    {
        var win = GetWindow<DialogEditor>("Dialog Editor");
        win.minSize = new Vector2(480, 360);
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

            // Card background
            var rect = EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f, 0.3f));

            // ── Row: index label + remove button ──
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Line {i + 1}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            GUI.enabled = lines.Count > 1;
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                lines.RemoveAt(i);
                GUIUtility.ExitGUI();
                return;
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);

            // ── Speaker row ──
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

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        if (GUILayout.Button("+ Add Line", GUILayout.Height(26)))
        {
            // Inherit last line's speaker/color/side as default
            var prev = lines[lines.Count - 1];
            lines.Add(new LineData
            {
                speakerName    = prev.speakerName,
                namePlateColor = prev.namePlateColor,
                side           = prev.side
            });
        }
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

        string folder = "Assets/Dialogs";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets", "Dialogs");

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
            asset.lines[i] = new DialogLine
            {
                speakerName    = lines[i].speakerName,
                namePlateColor = lines[i].namePlateColor,
                portrait       = lines[i].portrait,
                side           = lines[i].side,
                text           = lines[i].text
            };
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
        lines    = new List<LineData> { new LineData() };
    }
}
