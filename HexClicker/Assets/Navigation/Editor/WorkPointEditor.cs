using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HexClicker.Navigation
{
    [CustomEditor(typeof(WorkPoint), true)]
    public class WorkPointEditor : Editor
    {
        SerializedProperty position;
        SerializedProperty rotation;
        SerializedProperty showHandles;

        private void OnEnable()
        {
            position = serializedObject.FindProperty("position");
            rotation = serializedObject.FindProperty("rotation");
            showHandles = serializedObject.FindProperty("showHandles");
        }

        public void OnSceneGUI()
        {
            if (showHandles.boolValue)
            {
                WorkPoint access = (WorkPoint)target;
                Transform transform = access.transform;

                Vector3 worldPos = transform.TransformPoint(position.vector3Value);
                Quaternion worldRotation = transform.rotation * Quaternion.AngleAxis(rotation.floatValue, Vector3.up);

                Handles.TransformHandle(ref worldPos, ref worldRotation);

                position.vector3Value = transform.InverseTransformPoint(worldPos);
                rotation.floatValue = (worldRotation * Quaternion.Inverse(transform.rotation)).eulerAngles.y;

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
