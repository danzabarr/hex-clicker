using UnityEditor;
using UnityEngine;

namespace HexClicker.Navigation
{
    [CustomEditor(typeof(Area))]
    public class AreaEditor : Editor
    {
        SerializedProperty points;
        SerializedProperty showHandles;
        //SerializedProperty useXZ;

        private void OnEnable()
        {
            points = serializedObject.FindProperty("points");
            showHandles = serializedObject.FindProperty("showHandles");
            //useXZ = serializedObject.FindProperty("useXZ");
        }

        public void OnSceneGUI()
        {
            if (!Selection.activeGameObject == target)
                return;

            Area area = target as Area;
            Transform transform = area.transform;

            if (showHandles.boolValue)
            {

                for (int i = 0; i < points.arraySize; i++)
                {
                    SerializedProperty element = points.GetArrayElementAtIndex(i);
                    //if (useXZ.boolValue)
                    //element.vector2Value = transform.InverseTransformPoint(Handles.PositionHandle(transform.TransformPoint(element.vector2Value.x0z()), transform.rotation)).xz();
                    element.vector2Value = transform.InverseTransformPoint(Handles.PositionHandle(transform.TransformPoint(element.vector2Value.x0z()), Quaternion.identity)).xz();
                }
                serializedObject.ApplyModifiedProperties();
            }
            
           
        }
    }
}
