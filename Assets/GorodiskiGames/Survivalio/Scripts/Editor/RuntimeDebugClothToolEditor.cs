#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RuntimeDebugClothTool))]
public sealed class RuntimeDebugClothToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var tool = (RuntimeDebugClothTool)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime Debug Controls", EditorStyles.boldLabel);

        tool.itemInput = EditorGUILayout.TextField("Item Input", tool.itemInput);

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Cloths"))
                tool.AddCloths(RuntimeDebugClothTool.ParseItemList(tool.itemInput));

            if (GUILayout.Button("Remove Cloths"))
                tool.RemoveCloths(RuntimeDebugClothTool.ParseItemList(tool.itemInput));

            EditorGUILayout.EndHorizontal();
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("Play 모드에서만 Add/Remove 버튼이 동작합니다.", MessageType.Info);

        if (GUI.changed)
            EditorUtility.SetDirty(tool);
    }
}
#endif
