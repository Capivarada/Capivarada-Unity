#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeadAccessoryDefinition))]
public sealed class HeadAccessoryDefinitionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        HeadAccessoryDefinition definition = (HeadAccessoryDefinition)target;
        if (definition.Prefab == null || !GUILayout.Button("Ler materiais do prefab")) return;
        SerializedProperty slots = serializedObject.FindProperty("materialColors");
        slots.ClearArray();
        foreach (Renderer renderer in definition.Prefab.GetComponentsInChildren<Renderer>(true))
        {
            string path = AnimationUtility.CalculateTransformPath(renderer.transform, definition.Prefab.transform);
            for (int index = 0; index < renderer.sharedMaterials.Length; index++)
            {
                int targetIndex = slots.arraySize; slots.InsertArrayElementAtIndex(targetIndex);
                SerializedProperty element = slots.GetArrayElementAtIndex(targetIndex);
                element.FindPropertyRelative("rendererPath").stringValue = path;
                element.FindPropertyRelative("materialIndex").intValue = index;
                Material material = renderer.sharedMaterials[index];
                element.FindPropertyRelative("color").colorValue = material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor") : material != null && material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            }
        }
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(definition);
    }
}
#endif
