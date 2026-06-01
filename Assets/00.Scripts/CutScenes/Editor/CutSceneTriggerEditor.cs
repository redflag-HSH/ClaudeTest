// 경로: Assets/Editor/CutSceneTriggerEditor.cs
// Editor 폴더 안에 넣어주세요.

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CutSceneTrigger))]
public class CutSceneTriggerEditor : Editor
{
    // 씬뷰 기즈모 색상
    private static readonly Color _rangeColor = new Color(0.2f, 0.9f, 0.4f, 0.15f);
    private static readonly Color _rangeOutline = new Color(0.2f, 0.9f, 0.4f, 0.8f);

    public override void OnInspectorGUI()
    {
        CutSceneTrigger t = (CutSceneTrigger)target;

        // ── 헤더 ─────────────────────────────────────────────
        EditorGUILayout.Space(4);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
        EditorGUILayout.LabelField("🎬 CutScene Trigger", titleStyle);
        EditorGUILayout.Space(4);

        // ── Settings ─────────────────────────────────────────
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

        serializedObject.Update();

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("mode"),
            new GUIContent("트리거 모드", "Auto: 진입 즉시 재생 / Interact: 상호작용키 입력 시 재생"));

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("playOnce"),
            new GUIContent("한 번만 재생", "체크 시 최초 1회만 재생"));

        EditorGUILayout.Space(6);

        // ── References ───────────────────────────────────────
        EditorGUILayout.LabelField("References", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("director"),
            new GUIContent("Playable Director", "재생할 Timeline의 PlayableDirector"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8);

        // ── 콜라이더 정보 표시 ───────────────────────────────
        CircleCollider2D col = t.GetComponent<CircleCollider2D>();
        if (col != null)
        {
            EditorGUI.BeginChangeCheck();
            float newRadius = EditorGUILayout.FloatField(
                new GUIContent("트리거 반경", "CircleCollider2D의 radius 값"),
                col.radius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(col, "Change Trigger Radius");
                col.radius = Mathf.Max(0.1f, newRadius);
            }

            if (!col.isTrigger)
            {
                EditorGUILayout.HelpBox(
                    "CircleCollider2D의 Is Trigger가 꺼져 있습니다. 켜주세요.",
                    MessageType.Warning);
                if (GUILayout.Button("Is Trigger 켜기"))
                {
                    Undo.RecordObject(col, "Enable Is Trigger");
                    col.isTrigger = true;
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox(
                "CircleCollider2D가 없습니다. 아래 버튼으로 추가하세요.",
                MessageType.Warning);

            GUI.backgroundColor = new Color(1f, 0.85f, 0.3f);
            if (GUILayout.Button("CircleCollider2D 추가"))
            {
                Undo.AddComponent<CircleCollider2D>(t.gameObject).isTrigger = true;
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space(6);

        // ── Director 없을 때 경고 ────────────────────────────
        if (t.director == null)
        {
            EditorGUILayout.HelpBox(
                "Playable Director가 연결되지 않았습니다.",
                MessageType.Warning);
        }

        EditorGUILayout.Space(4);

        // ── 테스트 버튼 (플레이 모드 전용) ──────────────────
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
        if (GUILayout.Button("▶  TryPlay() 테스트", GUILayout.Height(28)))
            t.TryPlay();
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("테스트 버튼은 플레이 모드에서만 동작합니다.", MessageType.None);
    }

    // ── 씬뷰 기즈모 (선택 시: 드래그 핸들 포함) ─────────────
    private void OnSceneGUI()
    {
        CutSceneTrigger t = (CutSceneTrigger)target;
        CircleCollider2D col = t.GetComponent<CircleCollider2D>();
        if (col == null) return;

        Vector3 center = t.transform.position;
        float radius = col.radius * Mathf.Max(
            Mathf.Abs(t.transform.lossyScale.x),
            Mathf.Abs(t.transform.lossyScale.y));

        // 반경 드래그 핸들
        EditorGUI.BeginChangeCheck();
        float newRadius = Handles.RadiusHandle(
            Quaternion.identity, center, radius);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(col, "Drag Trigger Radius");
            col.radius = Mathf.Max(0.1f, newRadius /
                Mathf.Max(Mathf.Abs(t.transform.lossyScale.x),
                          Mathf.Abs(t.transform.lossyScale.y)));
        }
    }

    // ── 항상 표시되는 기즈모 (비선택 포함) ──────────────────
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Active)]
    private static void DrawAlwaysGizmo(CutSceneTrigger t, GizmoType gizmoType)
    {
        CircleCollider2D col = t.GetComponent<CircleCollider2D>();
        if (col == null) return;

        Vector3 center = t.transform.position;
        float radius = col.radius * Mathf.Max(
            Mathf.Abs(t.transform.lossyScale.x),
            Mathf.Abs(t.transform.lossyScale.y));

        bool selected = (gizmoType & GizmoType.Selected) != 0;

        // 채우기 원 (선택 시 더 불투명)
        Handles.color = selected
            ? _rangeColor
            : new Color(_rangeColor.r, _rangeColor.g, _rangeColor.b, 0.06f);
        Handles.DrawSolidDisc(center, Vector3.forward, radius);

        // 외곽선 (선택 시 더 불투명)
        Handles.color = selected
            ? _rangeOutline
            : new Color(_rangeOutline.r, _rangeOutline.g, _rangeOutline.b, 0.4f);
        Handles.DrawWireDisc(center, Vector3.forward, radius);

        // 라벨
        GUIStyle labelStyle = new GUIStyle
        {
            normal = { textColor = selected ? _rangeOutline : new Color(_rangeOutline.r, _rangeOutline.g, _rangeOutline.b, 0.4f) },
            fontStyle = FontStyle.Bold,
            fontSize = 11
        };
        Handles.Label(center + Vector3.right * radius + Vector3.up * 0.15f,
            $"r = {col.radius:F2}", labelStyle);
    }
}
#endif
