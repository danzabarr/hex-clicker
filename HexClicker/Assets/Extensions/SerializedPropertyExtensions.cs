using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class SerializedPropertyExtensions
{
#if UNITY_EDITOR
    // Gets value from SerializedProperty - even if value is nested
    public static T GetValue<T>(this UnityEditor.SerializedProperty property)
    {
        object obj = property.serializedObject.targetObject;
        foreach (string path in property.propertyPath.Split('.'))
        {
            var type = obj.GetType();
            FieldInfo field = type.GetField(path, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            obj = field.GetValue(obj);
        }
        return (T)obj;
    }

    // Sets value from SerializedProperty - even if value is nested
    public static void SetValue(this UnityEditor.SerializedProperty property, object val)
    {
        object obj = property.serializedObject.targetObject;

        List<KeyValuePair<FieldInfo, object>> list = new List<KeyValuePair<FieldInfo, object>>();

        foreach (string path in property.propertyPath.Split('.'))
        {
            System.Type type = obj.GetType();
            FieldInfo field = type.GetField(path, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            list.Add(new KeyValuePair<FieldInfo, object>(field, obj));
            obj = field.GetValue(obj);
        }

        // Now set values of all objects, from child to parent
        for (int i = list.Count - 1; i >= 0; --i)
        {
            list[i].Key.SetValue(list[i].Value, val);
            // New 'val' object will be parent of current 'val' object
            val = list[i].Value;
        }
    }
#endif // UNITY_EDITOR
}