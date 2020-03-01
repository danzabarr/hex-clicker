using HexClicker.UI.Notifications;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NotificationSystem))]
public class NotificationSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Test Post"))
            ((NotificationSystem)target).TestPost();

    }
}
