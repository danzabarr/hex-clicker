using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace HexClicker.Behaviour
{
    [CustomNodeEditor(typeof(Node))]
    public class NodeEditor
    {
        private static Dictionary<Type, Type> types;
        private static Dictionary<Node, NodeEditor> editors = new Dictionary<Node, NodeEditor>();
        public static Type[] EditorTypes => editorTypes == null ? editorTypes = GetDerivedTypes(typeof(NodeEditor)) : editorTypes;
        public static Type[] NodeTypes => nodeTypes == null ? nodeTypes = GetDerivedTypes(typeof(Node)) : nodeTypes;

        private static Type[] nodeTypes;
        private static Type[] editorTypes;

        private static GUIStyle nodeStyle;
        private static GUIStyle headerStyle;
        private static GUIStyle buttonStyle;

        private static Texture2D nodeBackground;
        private static Texture2D nodeBackgroundActive;
        private static Texture2D nodeBackgroundFocused;
        private static Texture2D buttonBackground;


        public GraphEditorWindow window;
        public Node target;
        public SerializedObject serializedObject;

        public static NodeEditor GetEditor(Node target, GraphEditorWindow window)
        {
            if (target == null) return null;
            if (!editors.TryGetValue(target, out NodeEditor editor))
            {
                Type type = target.GetType();
                Type editorType = GetEditorType(type);
                editor = Activator.CreateInstance(editorType) as NodeEditor;
                editor.target = target;
                editor.serializedObject = new SerializedObject(target);
                editor.window = window;
                editor.OnCreate();
                editors.Add(target, editor);
            }
            if (editor.target == null) editor.target = target;
            if (editor.window != window) editor.window = window;
            if (editor.serializedObject == null) editor.serializedObject = new SerializedObject(target);
            return editor;
        }

        private static Type GetEditorType(Type type)
        {
            if (type == null) return null;
            if (types == null) CacheCustomEditors();
            if (types.TryGetValue(type, out Type result)) return result;
            //If type isn't found, try base type
            return typeof(NodeEditor);
        }

        private static void CacheCustomEditors()
        {
            types = new Dictionary<Type, Type>();

            //Get all classes deriving from NodeEditor via reflection
            Type[] nodeEditors = EditorTypes;
            for (int i = 0; i < nodeEditors.Length; i++)
            {
                if (nodeEditors[i].IsAbstract) continue;
                var attribs = nodeEditors[i].GetCustomAttributes(typeof(CustomNodeEditorAttribute), false);
                if (attribs == null || attribs.Length == 0) continue;
                CustomNodeEditorAttribute attrib = attribs[0] as CustomNodeEditorAttribute;
                types.Add(attrib.GetInspectedType(), nodeEditors[i]);
            }
        }
        public static Type[] GetDerivedTypes(Type baseType)
        {
            List<Type> types = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes().Where(t => !t.IsAbstract && baseType.IsAssignableFrom(t)).ToArray());
                }
                catch (ReflectionTypeLoadException) { }
            }
            return types.ToArray();
        }

        public virtual void OnCreate() { }

        public delegate void ButtonClicked();

        public void OnGUI(Vector2 panOffset, bool active, ButtonClicked connectionButtonClicked)
        {
            //nodeStyle = null;
            //headerStyle = null;
            //buttonStyle = null;

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle()
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fontStyle = FontStyle.Bold,
                };
            }

            if (nodeBackground == null)
                nodeBackground = EditorGUIUtility.Load("builtin skins/darkskin/images/node0.png") as Texture2D;

            if (nodeBackgroundFocused == null)
                nodeBackgroundFocused = EditorGUIUtility.Load("builtin skins/darkskin/images/node0 on.png") as Texture2D;

            if (nodeStyle == null)
            {
                nodeStyle = new GUIStyle("label")
                {

                    border = new RectOffset(10, 10, 10, 10),
                    padding = new RectOffset(-20, -20, -30, -50),
                };

                nodeStyle.normal.background = nodeBackground;
                nodeStyle.active.background = nodeBackgroundActive;
                nodeStyle.focused.background = nodeBackgroundFocused;
                
            }

            if (buttonStyle == null)
            {
                buttonStyle = new GUIStyle("button")
                {
                    stretchHeight = true
                };
            }

            serializedObject.Update();
            SerializedProperty prop = serializedObject.GetIterator();

            bool enterChildren = true;

            target.rect.size = new Vector2(250, 250);
            Rect rect = target.rect;
            rect.position += panOffset;

            if (active)
                GUI.color = new Color(.6f, 1f, .7f);
            if (Event.current.type == EventType.Repaint)
                nodeStyle.Draw(rect, false, false, false, Selection.instanceIDs.Contains(target.GetInstanceID()));
            GUI.color = Color.white;

            Rect inner = nodeStyle.padding.Add(rect);

            GUILayout.BeginArea(inner);

            GUILayout.BeginVertical();

            
            GUILayout.Label(target.name, headerStyle);
            GUILayout.Space(10);
            

            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 100;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (prop.name == "m_Script") continue;
                if (prop.name == "graph") continue;
                if (prop.name == "rect") continue;
                if (prop.name == "connections") continue;
                if (target is AnyNode || target is EntryNode)
                {
                    if (prop.name == "mode") continue;
                    if (prop.name == "exclude") continue;
                }

                EditorGUILayout.PropertyField(prop);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
                serializedObject.ApplyModifiedProperties();
            }
            GUILayout.EndVertical();
            GUILayout.EndArea();

            Rect button = target.Button;
            button.position += panOffset;

            GUILayout.BeginArea(button);
            if (GUILayout.Button("", buttonStyle))
            {
                connectionButtonClicked();
            }
            GUILayout.EndArea();
        }

        public interface INodeEditorAttrib
        {
            Type GetInspectedType();
        }

        [AttributeUsage(AttributeTargets.Class)]
        public class CustomNodeEditorAttribute : Attribute, INodeEditorAttrib
        {
            private Type inspectedType;
            public CustomNodeEditorAttribute(Type inspectedType)
            {
                this.inspectedType = inspectedType;
            }

            public Type GetInspectedType()
            {
                return inspectedType;
            }
        }
    }
}
