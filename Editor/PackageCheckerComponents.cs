#nullable enable
using UnityEditor;
using UnityEngine;

public static class PackageCheckerComponents
{
    private const string CREDIT_URL = "https://linktr.ee/noppers";

    public static void CreditFooter()
    {
        EditorGUILayout.Space(8);

        // Separator line
        var separatorRect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(separatorRect, new Color(0.3f, 0.3f, 0.3f));

        EditorGUILayout.Space(8);

        // Center the link
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // Create link style
        var linkStyle = new GUIStyle(EditorStyles.label);
        linkStyle.fontSize = 11;
        linkStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
        linkStyle.alignment = TextAnchor.MiddleCenter;

        // Create clickable label
        var linkRect = GUILayoutUtility.GetRect(new GUIContent("Tool by NOPPERS"), linkStyle);
        EditorGUI.LabelField(linkRect, "Tool by NOPPERS", linkStyle);
        EditorGUIUtility.AddCursorRect(linkRect, MouseCursor.Link);

        if (Event.current.type == EventType.MouseDown && linkRect.Contains(Event.current.mousePosition))
        {
            Application.OpenURL(CREDIT_URL);
            Event.current.Use();
        }

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);
    }
}