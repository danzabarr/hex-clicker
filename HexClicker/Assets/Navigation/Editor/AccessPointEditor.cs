using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HexClicker.Navigation
{
    [CustomEditor(typeof(AccessPoint))]
    public class AccessPointEditor : Editor
    {
        SerializedProperty inside;
        SerializedProperty outside;
        SerializedProperty showHandles;

        private void OnEnable()
        {
            inside = serializedObject.FindProperty("inside");
            outside = serializedObject.FindProperty("outside");
            showHandles = serializedObject.FindProperty("showHandles");
        }

        public void OnSceneGUI()
        {
            if (showHandles.boolValue)
            {
                AccessPoint access = (AccessPoint)target;
                Transform transform = access.transform;

                Vector3 insideWorld = transform.TransformPoint(inside.vector3Value);
                Vector3 outsideWorld = transform.TransformPoint(outside.vector3Value);

                inside.vector3Value = transform.InverseTransformPoint(Handles.PositionHandle(insideWorld, transform.rotation));
                outside.vector3Value = transform.InverseTransformPoint(Handles.PositionHandle(outsideWorld, transform.rotation));

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
