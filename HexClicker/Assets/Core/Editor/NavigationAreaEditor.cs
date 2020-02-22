using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavigationArea))]
public class NavigationAreaEditor : Editor
{
    SerializedProperty pointsArray;
    //SerializedProperty useXZ;

    private void OnEnable()
    {
        pointsArray = serializedObject.FindProperty("points");
        //useXZ = serializedObject.FindProperty("useXZ");
    }

    public void OnSceneGUI()
    {
        Transform transform = ((NavigationArea)target).transform;

        for (int i = 0; i < pointsArray.arraySize; i++)
        {
            SerializedProperty element = pointsArray.GetArrayElementAtIndex(i);
            //if (useXZ.boolValue)
                element.vector2Value = transform.InverseTransformPoint(Handles.PositionHandle(transform.TransformPoint(element.vector2Value.x0z()), transform.rotation)).xz();
            //else 
            //    element.vector2Value = transform.InverseTransformPoint(Handles.PositionHandle(transform.TransformPoint(element.vector2Value), transform.rotation));

        }
        serializedObject.ApplyModifiedProperties();
    }
}
