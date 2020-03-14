using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HexClicker.Behaviour
{
    [CustomEditor(typeof(Connection))]
    public class ConnectionEditor : Editor
    {
        private SerializedProperty condition;

        public void OnEnable()
        {
            condition = serializedObject.FindProperty("condition");
        }

        public override void OnInspectorGUI()
        {
            Connection connection = (Connection)target;

            //DrawDefaultInspector();

            

            EditorGUILayout.LabelField("Order", connection.index + "");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(" ");
            if (connection.index <= 0)
                GUI.enabled = false;
            if (GUILayout.Button("-"))
            {
                connection.from.connections.RemoveAt(connection.index);
                connection.index--;
                connection.from.connections.Insert(connection.index, connection);
                GraphEditorWindow.current?.Repaint();
            }
            GUI.enabled = true;

            if (connection.index >= connection.from.connections.Count - 1)
                GUI.enabled = false;
            if (GUILayout.Button("+"))
            {
                connection.from.connections.RemoveAt(connection.index);
                connection.index++;
                connection.from.connections.Insert(connection.index, connection);
                GraphEditorWindow.current?.Repaint();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();


            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(condition);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
