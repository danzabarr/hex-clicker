using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace HexClicker.Behaviour
{
    public class GraphEditorWindow : EditorWindow
    {
        public static GraphEditorWindow current;

        private Graph graph;

        private Vector2 panOffset;
        private float zoom = 1;
        private bool autoSave = true;

        private Node draggedNode;
        private Vector2 dragStart;
        private Node connectingNode;

        private static Texture2D connectionButton;
        private static Texture2D connectionButtonFocused;

        public void OnFocus()
        {
            current = this;
        }

        [OnOpenAsset(0)]
        public static bool OnOpen(int instanceID, int line)
        {
            Graph nodeGraph = EditorUtility.InstanceIDToObject(instanceID) as Graph;
            if (nodeGraph != null)
            {
                Open(nodeGraph);
                return true;
            }
            return false;
        }

        public static void Open(Graph graph)
        {
            if (!graph)
                return;

            GraphEditorWindow w = GetWindow(typeof(GraphEditorWindow), false, "Behaviour Graph", true) as GraphEditorWindow;
            w.wantsMouseMove = true;
            w.graph = graph;
        }

        private int topPadding { get { return isDocked() ? 19 : 22; } }
        private Func<bool> isDocked
        {
            get
            {
                if (_isDocked == null)
                {
                    BindingFlags fullBinding = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
                    MethodInfo isDockedMethod = typeof(GraphEditorWindow).GetProperty("docked", fullBinding).GetGetMethod(true);
                    _isDocked = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), this, isDockedMethod);
                }
                return _isDocked;
            }
        }

        private Func<bool> _isDocked;

        public static Rect ScaleSizeBy(Rect rect, float scale, Vector2 pivotPoint)
        {
            Rect result = rect;
            result.x -= pivotPoint.x;
            result.y -= pivotPoint.y;
            result.xMin *= scale;
            result.xMax *= scale;
            result.yMin *= scale;
            result.yMax *= scale;
            result.x += pivotPoint.x;
            result.y += pivotPoint.y;
            return result;
        }

        private Matrix4x4 prevGuiMatrix;

        public void BeginZoomed()
        {
            GUI.EndGroup();

            Rect position = new Rect(this.position);
            position.x = 0;
            position.y = topPadding;

            Vector2 topLeft = new Vector2(position.xMin, position.yMin - topPadding);
            Rect clippedArea = ScaleSizeBy(position, zoom, topLeft);
            GUI.BeginGroup(clippedArea);

            prevGuiMatrix = GUI.matrix;
            Matrix4x4 translation = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            Matrix4x4 scale = Matrix4x4.Scale(new Vector3(1.0f / zoom, 1.0f / zoom, 1.0f));
            GUI.matrix = translation * scale * translation.inverse;
        }

        public void EndZoomed()
        {
            GUI.matrix = prevGuiMatrix;
            GUI.EndGroup();
            GUI.BeginGroup(new Rect(0, topPadding - (topPadding * zoom), Screen.width, Screen.height));
        }

        public Node GetNode(Vector2 mouse)
        {
            foreach (Node node in graph)
                if (node.rect.Contains(mouse))
                    return node;
            return null;
        }

        public static Vector2 Bezier(float t, Vector2 start, Vector2 end, Vector2 startTangent, Vector2 endTangent)
        {
            Vector2 p0 = start;
            Vector2 p1 = startTangent;
            Vector2 p2 = endTangent;
            Vector2 p3 = end;

            return (1 - t) * (1 - t) * (1 - t) * p0
                + 3 * (1 - t) * (1 - t) * t * p1
                + 3 * (1 - t) * t * t * p2
                + t * t * t * p3;
        }

        private void DrawGrid(float size, Color color)
        {
            Color old = Handles.color;
            Handles.color = color;
            float xStart = (panOffset.x * zoom - Mathf.Floor(panOffset.x / size * zoom) * size) - size;
            float yStart = (panOffset.y * zoom - Mathf.Floor(panOffset.y / size * zoom) * size) - size;
            float xEnd = Screen.width * zoom;
            float yEnd = (Screen.height) * zoom;

            for (float x = xStart; x < xEnd; x += size)
                Handles.DrawLine(new Vector2(x, yStart), new Vector2(x, yEnd));
            for (float y = yStart; y < yEnd; y += size)
                Handles.DrawLine(new Vector2(xStart, y), new Vector2(xEnd, y));
            Handles.color = old;
        }

        private Vector2 Snap(Vector2 position, float res)
        {
            float x = Mathf.Round(position.x / res) * res;
            float y = Mathf.Round(position.y / res) * res;
            return new Vector2(x, y);
        }

        private static void DrawConnection(Vector2 start, Vector2 end, out Vector2 middle)
        {
            middle = default;
            Vector2 startTangent = new Vector2(start.x, Mathf.Max(end.y, start.y + (start.y - end.y) + 50));
            Vector2 endTangent = new Vector2(end.x, Mathf.Min(start.y, end.y + (end.y - start.y) - 50));

            float deltaX = Mathf.Abs(start.x - end.x);
            float minSpace = 250;

            Vector2 center = (start + end) / 2;

            if (start.y > end.y && deltaX < minSpace)
            {
                if (start.x < end.x)
                {
                    startTangent.x += minSpace - deltaX;
                    endTangent.x += minSpace - deltaX;
                    center.x += (minSpace - deltaX) / 2;
                }
                else
                {
                    startTangent.x -= minSpace - deltaX;
                    endTangent.x -= minSpace - deltaX;
                    center.x -= (minSpace - deltaX) / 2;
                }
            }

            Handles.DrawBezier(start, end, startTangent, endTangent, Color.yellow, null, 8);

            middle = Bezier(.5f, start, end, startTangent, endTangent);
        }

        private static GUIStyle connectionButtonStyle;

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUI.DrawRect(new Rect(Vector2.zero, position.size), new Color(.375f, .375f, .375f));

            float minZoom = 1f;
            float maxZoom = 4f;
            float zoomSensitivity = .1f;
            zoom = Mathf.Clamp(zoom, minZoom, maxZoom);

            BeginZoomed();

            #region Draw Grid

            DrawGrid(25, new Color(.325f, .325f, .325f));
            DrawGrid(250, new Color(.25f, .25f, .25f));

            #endregion

            if (graph == null)
            {
                EndZoomed();
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50 + topPadding, 200, 100), "No Graph Selected", new GUIStyle("label") { alignment = TextAnchor.MiddleCenter, fontSize = 15 });
                return;
            }

           
            if (graph.entry == null)
                graph.entry = CreateNode(typeof(EntryNode), new Vector2(0, 0)) as EntryNode;
        
            if (graph.any == null)
                graph.any = CreateNode(typeof(AnyNode), new Vector2(500, 0)) as AnyNode;


            Agent agent = Selection.activeGameObject?.GetComponent<Agent>();

            Vector2 mouse = Event.current.mousePosition + new Vector2(0, topPadding);
            mouse -= panOffset * zoom;

            #region Draw Connections


            if (connectionButton == null)
                connectionButton = EditorGUIUtility.Load("builtin skins/darkskin/images/node0 hex.png") as Texture2D;

            if (connectionButtonFocused == null)
                connectionButtonFocused = EditorGUIUtility.Load("builtin skins/darkskin/images/node0 hex on.png") as Texture2D;

            connectionButtonStyle = null;
            if (connectionButtonStyle == null)
            {
                connectionButtonStyle = new GUIStyle("button")
                {
                    stretchHeight = true,
                    
                };

                connectionButtonStyle.normal.background = connectionButton;
                connectionButtonStyle.active.background = connectionButton;
                connectionButtonStyle.focused.background = connectionButtonFocused;
            }

            foreach (Node node in graph)
            {
                if (node == null)
                    continue;
                if (node.connections == null)
                    continue;

                foreach (Connection c in node.connections)
                {
                    if (c == null)
                        continue;

                    DrawConnection(node.Button.center + panOffset * zoom, c.node.InPoint + panOffset * zoom, out Vector2 middle);

                    Vector2 connectionButtonSize = new Vector2(50, 50);

                    Rect connectionButtonRect = new Rect(middle - connectionButtonSize / 2, connectionButtonSize);
                    GUILayout.BeginArea(connectionButtonRect);
                    if (GUILayout.Button("", connectionButtonStyle))
                    {
                    }
                    GUILayout.EndArea();
                }
            }

            #endregion

            #region Draw Nodes

            foreach (Node node in graph)
            {
                NodeEditor.GetEditor(node, this).OnGUI(panOffset * zoom, agent != null && node == agent.State, ConnectionButtonClicked);

                void ConnectionButtonClicked()
                {
                    if (connectingNode != null)
                        connectingNode = null;
                    else
                        connectingNode = node;
                }
            }
            #endregion

            #region Draw Current Connection

            if (connectingNode != null)
            {

                DrawConnection(connectingNode.Button.center + panOffset * zoom, mouse + panOffset * zoom, out _);
                Repaint();
            }

            #endregion

            #region Draw Selection Box

            #endregion

            

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    if (Event.current.button == 0)
                    {
                        Node get = GetNode(mouse);
                        
                        if (get != null)
                        {
                            if (connectingNode != null)
                            { 
                                if (connectingNode.Connect(get, out Connection connection))
                                {
                                    connection.name = "Connection (" + connectingNode.name + " -> " + get.name + ")";
                                    AssetDatabase.AddObjectToAsset(connection, graph);
                                    if (autoSave) AssetDatabase.SaveAssets();
                                    connectingNode = null;
                                    Repaint();
                                }
                            }
                            else
                            {
                                draggedNode = get;
                                Selection.activeInstanceID = draggedNode ? draggedNode.GetInstanceID() : -1;
                                dragStart = mouse - draggedNode.rect.position;
                                EditorGUIUtility.PingObject(draggedNode);
                                Repaint();
                            }
                        }
                    }

                    break;
                case EventType.MouseUp:
                    if (Event.current.button == 0)
                    {
                        draggedNode = null;
                    }
                    break;
                case EventType.MouseMove:
                    break;
                case EventType.MouseDrag:

                    if (Event.current.button == 0)
                    {
                        if (draggedNode != null)
                        {
                            draggedNode.rect.position = mouse - dragStart;//+= Event.current.delta;
                            draggedNode.rect.position = Snap(draggedNode.rect.position, 25);
                            Repaint();
                        }
                    }

                    else if (Event.current.button == 2)
                    {
                        panOffset += Event.current.delta / zoom;
                        Repaint();
                    }

                    break;

                case EventType.KeyDown:
                    break;
                case EventType.KeyUp:
                    break;
                case EventType.ScrollWheel:

                    float scrollDelta = Event.current.delta.y * zoomSensitivity;
                    scrollDelta = Mathf.Clamp(zoom + scrollDelta, minZoom, maxZoom) - zoom;
                    if (Mathf.Abs(scrollDelta) > 0.0001f)
                    {
                        panOffset += mouse / zoom;
                        zoom += scrollDelta;
                        panOffset -= mouse / zoom;
                        Repaint();
                    }

                    break;
                case EventType.Repaint:
                    break;
                case EventType.Layout:
                    break;
                case EventType.DragUpdated:
                    break;
                case EventType.DragPerform:
                    break;
                case EventType.DragExited:
                    break;
                case EventType.Ignore:
                    break;
                case EventType.Used:
                    break;
                case EventType.ValidateCommand:
                    break;
                case EventType.ExecuteCommand:
                    break;
                case EventType.ContextClick:
                    {
                        GenericMenu menu = new GenericMenu();


                        Node get = GetNode(mouse);

                        if (get != null)
                        {
                            menu.AddItem(new GUIContent("Delete"), false, () =>
                            {
                                DeleteNode(get);
                            });
                        }
                        else
                        {
                            foreach (Type type in NodeEditor.NodeTypes)
                            {
                                if (type == typeof(EntryNode))
                                    continue;
                                if (type == typeof(AnyNode))
                                    continue;
                                string path = "Add Node/" + GetNodeMenuName(type);
                                if (string.IsNullOrEmpty(path)) continue;
                                menu.AddItem(new GUIContent(path), false, () =>
                                {
                                    CreateNode(type, mouse);
                                });

                            }
                        }
                        Matrix4x4 m4 = GUI.matrix;
                        GUI.matrix = Matrix4x4.identity;

                        menu.ShowAsContext();

                        GUI.matrix = m4;
                    }

                    break;
                case EventType.MouseEnterWindow:
                    break;
                case EventType.MouseLeaveWindow:
                    break;
            }

            EndZoomed();
        }

        public Node CreateNode(Type type, Vector2 position)
        {
            if (graph == null)
                return null;
            Node node = graph.AddNode(type);
            node.rect.position = Snap(position, 25);
            if (string.IsNullOrEmpty(node.name))
            {
                string typeName = type.Name;
                if (typeName.EndsWith("Node")) typeName = typeName.Substring(0, typeName.Length - 4);
                node.name = ObjectNames.NicifyVariableName(typeName);

            }
            AssetDatabase.AddObjectToAsset(node, graph);
            if (autoSave) AssetDatabase.SaveAssets();
            Repaint();
            return node;
        }

        public void DeleteNode(Node node)
        {
            foreach(Connection c in node.connections)
                AssetDatabase.RemoveObjectFromAsset(c);
            foreach(Node n in graph)
                foreach(Connection c in n.connections)
                    if (c.node == node)
                        AssetDatabase.RemoveObjectFromAsset(c);

            graph.RemoveNode(node);

            AssetDatabase.RemoveObjectFromAsset(node);
            if (autoSave) AssetDatabase.SaveAssets();
            Repaint();
        }

        public static FieldInfo GetFieldInfo(Type type, string fieldName)
        {
            // If we can't find field in the first run, it's probably a private field in a base class.
            FieldInfo field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Search base classes for private fields only. Public fields are found above
            while (field == null && (type = type.BaseType) != typeof(Node)) field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field;
        }

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
        public class CreateNodeMenuAttribute : Attribute
        {
            public string menuName;
            /// <summary> Manually supply node class with a context menu path </summary>
            /// <param name="menuName"> Path to this node in the context menu. Null or empty hides it. </param>
            public CreateNodeMenuAttribute(string menuName)
            {
                this.menuName = menuName;
            }
        }

        public static string GetNodeMenuName(Type type)
        {
            //Check if type has the CreateNodeMenuAttribute
            if (GetAttrib(type, out CreateNodeMenuAttribute attrib)) // Return custom path
                return attrib.menuName;
            else // Return generated path
                return ObjectNames.NicifyVariableName(type.Name.Replace('.', '/'));
        }

        public static bool GetAttrib<T>(Type classType, out T attribOut) where T : Attribute
        {
            object[] attribs = classType.GetCustomAttributes(typeof(T), false);
            return GetAttrib(attribs, out attribOut);
        }

        public static bool GetAttrib<T>(object[] attribs, out T attribOut) where T : Attribute
        {
            for (int i = 0; i < attribs.Length; i++)
            {
                if (attribs[i] is T)
                {
                    attribOut = attribs[i] as T;
                    return true;
                }
            }
            attribOut = null;
            return false;
        }

        public static bool GetAttrib<T>(Type classType, string fieldName, out T attribOut) where T : Attribute
        {
            // If we can't find field in the first run, it's probably a private field in a base class.
            FieldInfo field = GetFieldInfo(classType, fieldName);
            // This shouldn't happen. Ever.
            if (field == null)
            {
                Debug.LogWarning("Field " + fieldName + " couldnt be found");
                attribOut = null;
                return false;
            }
            object[] attribs = field.GetCustomAttributes(typeof(T), true);
            return GetAttrib(attribs, out attribOut);
        }

        public static bool HasAttrib<T>(object[] attribs) where T : Attribute
        {
            for (int i = 0; i < attribs.Length; i++)
            {
                if (attribs[i].GetType() == typeof(T))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
