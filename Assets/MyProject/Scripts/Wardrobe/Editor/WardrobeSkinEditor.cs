using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WardrobeSkin))]
public class WardrobeSkinEditor : Editor
{
    private static readonly SkinColorSlot[] Slots =
    {
        SkinColorSlot.Skin,
        SkinColorSlot.Tail,
        SkinColorSlot.Paw,
        SkinColorSlot.Nose,
        SkinColorSlot.Ears,
        SkinColorSlot.Mouth,
        SkinColorSlot.Eyes
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "colors");

        SerializedProperty colors = serializedObject.FindProperty("colors");
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Skin material slots", EditorStyles.boldLabel);

        for (int i = 0; i < Slots.Length; i++)
        {
            if (i >= colors.arraySize)
                break;

            SerializedProperty element = colors.GetArrayElementAtIndex(i);
            EditorGUILayout.LabelField(ObjectNames.NicifyVariableName(Slots[i].ToString()), EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(element.FindPropertyRelative("color"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("metallic"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("smoothness"));
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
