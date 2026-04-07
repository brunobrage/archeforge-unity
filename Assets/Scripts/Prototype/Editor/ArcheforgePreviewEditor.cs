#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Archeforge.UnityPort;

[CustomEditor(typeof(ArcheforgePrototypeController))]
public class ArcheforgePreviewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var script = (ArcheforgePrototypeController)target;

        GUILayout.Space(10);

        if (GUILayout.Button("🔥 Build Preview"))
        {
            script.BuildEditorPreview();
        }

        if (GUILayout.Button("🧹 Clear Preview"))
        {
            script.ClearPreview();
        }
    }
}
#endif