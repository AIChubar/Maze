using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeGenerator))]
public class MazeGeneratorEditor : Editor
{
    private static readonly Type[] AlgorithmTypes = { typeof(DfsAlgorithm), typeof(WilsonAlgorithm) };
    private static readonly string[] AlgorithmNames = { "DFS (Recursive Backtracker)", "Wilson's (Loop-Erased Random Walk)" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty algorithmProp = serializedObject.FindProperty("algorithm");

        int currentIndex = Array.FindIndex(AlgorithmTypes, t =>
            algorithmProp.managedReferenceValue?.GetType() == t);
        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup("Generation Algorithm", currentIndex, AlgorithmNames);
        if (EditorGUI.EndChangeCheck() || algorithmProp.managedReferenceValue == null)
            algorithmProp.managedReferenceValue = Activator.CreateInstance(AlgorithmTypes[newIndex]);

        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, "algorithm", "m_Script");

        serializedObject.ApplyModifiedProperties();
    }
}
