using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HexClicker.World
{
    [CustomEditor(typeof(Map))]
    public class MapEditor : Editor
    {
        private Map map;
        private void OnEnable()
        {
            map = (Map)target;
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
}
