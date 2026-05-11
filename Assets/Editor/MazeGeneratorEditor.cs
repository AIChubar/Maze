using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeGenerator))]
public class MazeGeneratorEditor : Editor
{
    private static readonly Type[] AlgorithmTypes = { typeof(DfsAlgorithm), typeof(WilsonAlgorithm), typeof(PrimsAlgorithm), typeof(KruskalsAlgorithm) };
    private static readonly string[] AlgorithmNames = { "DFS (Recursive Backtracker)", "Wilson's (Loop-Erased Random Walk)", "Prim's", "Kruskal's" };

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

        SerializedProperty child = algorithmProp.Copy();
        SerializedProperty end = algorithmProp.GetEndProperty();
        if (child.NextVisible(true))
        {
            EditorGUI.indentLevel++;
            while (!SerializedProperty.EqualContents(child, end))
            {
                EditorGUILayout.PropertyField(child, true);
                child.NextVisible(false);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, "algorithm", "m_Script");

        serializedObject.ApplyModifiedProperties();

        if (!Application.isPlaying) return;
        EditorGUILayout.Space();
        if (GUILayout.Button("Fill Walls"))
            ((MazeGenerator)target).FillWalls();
        if (GUILayout.Button("Regenerate Maze"))
            ((MazeGenerator)target).Regenerate();
    }
}
