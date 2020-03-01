using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HexClicker.Navigation
{
    [CustomEditor(typeof(Access))]
    public class AccessEditor : Editor
    {
        SerializedProperty paths;
        SerializedProperty showHandles;

        private void OnEnable()
        {
            paths = serializedObject.FindProperty("paths");
            showHandles = serializedObject.FindProperty("showHandles");
        }

        public void OnSceneGUI()
        {
            if (showHandles.boolValue)
            {
                Access access = (Access)target;
                Transform transform = access.transform;


                Access.Path[] paths = this.paths.GetValue<Access.Path[]>();

                for (int i = 0; i < paths.Length; i++)
                {
                    //Entrance entrance = area.GetEntrance(i);

                    Vector3 insideWorld = transform.TransformPoint(paths[i].inside);
                    Vector3 outsideWorld = transform.TransformPoint(paths[i].outside);

                    paths[i].outside = transform.InverseTransformPoint(Handles.PositionHandle(outsideWorld, transform.rotation));
                    paths[i].inside = transform.InverseTransformPoint(Handles.PositionHandle(insideWorld, transform.rotation));
                }

                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}