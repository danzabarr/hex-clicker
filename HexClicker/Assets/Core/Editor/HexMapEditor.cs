using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HexMap))]
public class HexMapEditor : Editor
{
    private HexMap map;
    private void OnEnable()
    {
        map = (HexMap)target;
    }

    public override void OnInspectorGUI()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Clear"))
            map.Clear();

        if (GUILayout.Button("Generate Map"))
            map.Generate();

        if (GUILayout.Button("Generate Navigation Graph"))
            map.GenerateNavigationGraph();

        GUILayout.EndHorizontal();

        DrawDefaultInspector();
    }
}
