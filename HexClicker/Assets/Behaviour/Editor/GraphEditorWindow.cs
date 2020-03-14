using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using System.Linq;

namespace HexClicker.Behaviour
{
    public class GraphEditorWindow : EditorWindow
    {
        public static GraphEditorWindow current;

        private Graph graph;
        private Agent agent;

        private Vector2 panOffset;
        private float zoom = 1;
        private bool autoSave = true;

        private List<Node> draggedNode;
        private List<Vector2> dragStart;
        private Node connectingNode;
        private Rect selectionRect;
        private bool selecting;

        private static Texture2D connectionButton;
        private static Texture2D connectionButtonFocused;

        public static readonly float ConnectionSelectionDistance = 50f;
        public static readonly float MinZoom = 1f;
        public static readonly float MaxZoom = 4f;
        public static readonly float ZoomSensitivity = .1f;

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

        private void OnSelectionChange()
        {
            Repaint();
        }

        private int topPadding => isDocked() ? 19 : 22;
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

        public float ClosestPointToCubicBezier(Vector2 p, int slices, int iterations, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return ClosestPointToCubicBezier(iterations, p, 0, 1f, slices, p0, p1, p2, p3);
        }

        private float ClosestPointToCubicBezier(int iterations, Vector2 p, float start, float end, int slices, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            if (iterations <= 0)
                return (start + end) / 2;
            float tick = (end - start) / (float)slices;
            Vector2 test, delta;
            float best = 0;
            float bestDistance = float.PositiveInfinity;
            float currentDistance;
            float t = start;
            while (t <= end)
            {
                //B(t) = (1-t)**3 p0 + 3(1 - t)**2 t P1 + 3(1-t)t**2 P2 + t**3 P3
                //x = (1 - t) * (1 - t) * (1 - t) * p0.x + 3 * (1 - t) * (1 - t) * t * p1.x + 3 * (1 - t) * t * t * p2.x + t * t * t * p3.x;
                //y = (1 - t) * (1 - t) * (1 - t) * p0.y + 3 * (1 - t) * (1 - t) * t * p1.y + 3 * (1 - t) * t * t * p2.y + t * t * t * p3.y;

                test = Bezier(t, p0, p1, p2, p3);
                delta = test - p;

                delta *= delta;

                currentDistance = delta.x + delta.y;
                if (currentDistance < bestDistance)
                {
                    bestDistance = currentDistance;
                    best = t;
                }
                t += tick;
            }
            return ClosestPointToCubicBezier(iterations - 1, p, Mathf.Max(best - tick, 0f), Mathf.Min(best + tick, 1f), slices, p0, p1, p2, p3);
        }

        public static Vector2 Bezier(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
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

        private static void DrawConnection(Vector2 start, Vector2 end, out Vector2 startTangent, out Vector2 endTangent, Color color)
        {
            startTangent = new Vector2(start.x, Mathf.Max(end.y, start.y + (start.y - end.y) + 50));
            endTangent = new Vector2(end.x, Mathf.Min(start.y, end.y + (end.y - start.y) - 50));

            float deltaX = Mathf.Abs(start.x - end.x);
            float minSpace = 250;


            if (start.y > end.y && deltaX < minSpace)
            {
                if (start.x < end.x)
                {
                    startTangent.x += minSpace - deltaX;
                    endTangent.x += minSpace - deltaX;
                }
                else
                {
                    startTangent.x -= minSpace - deltaX;
                    endTangent.x -= minSpace - deltaX;
                }
            }

            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, 8);
        }

        private static GUIStyle connectionButtonStyle;

        private void OnInspectorUpdate()
        {
            if (agent != null)
                Repaint();
        }

        private void OnGUI()
        {
            EditorGUI.DrawRect(new Rect(Vector2.zero, position.size), new Color(.375f, .375f, .375f));


            zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);

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


            agent = Selection.activeGameObject?.GetComponent<Agent>();

            Vector2 mouse = Event.current.mousePosition;// + new Vector2(0, topPadding);
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


            float connectionSelectionDistance = 50;

            Connection mouseConnection = null;
            float closestConnectionSq = connectionSelectionDistance * connectionSelectionDistance;

            Color selectedColor = new Color(.4f, .7f, 1f);
            Color nextColor = new Color(1f, .9f, .2f);

            foreach (Node node in graph)
            {
                if (node == null)
                    continue;
                if (node.connections == null)
                    continue;

                int count = node.connections.Count;

                Connection next = node.NextConnection(agent);

                for (int i = 0; i < count; i++)
                {
                    Connection c = node.connections[i];
                    if (c == null)
                        continue;

                    c.index = i;

                    //Vector2 start = node.Button.center + panOffset * zoom;
                    Vector2 start = new Vector2(node.rect.x + node.rect.width / 4 + node.rect.width / 2 / (count) * (i + 0.5f), node.rect.yMax) + panOffset * zoom;
                    Vector2 end = c.to.InPoint + panOffset * zoom;


                    Color color = Color.white;
                    if (Selection.Contains(c.GetInstanceID()))
                        color = selectedColor;
                    else if (c == next)
                        color = nextColor;

                    DrawConnection(start, end, out Vector2 startTangent, out Vector2 endTangent, color);

                    Vector2 connectionButtonSize = new Vector2(50, 50);

                    Vector2 middle = Bezier(.5f, start, startTangent, endTangent, end);

                    float closestT = ClosestPointToCubicBezier(mouse + panOffset * zoom, 10, 3, start, startTangent, endTangent, end);

                    Vector2 closestPoint = Bezier(closestT, start, startTangent, endTangent, end);
                    float distSq = (closestPoint - (mouse + panOffset * zoom)).sqrMagnitude;

                    if (distSq < closestConnectionSq)
                    {
                        closestConnectionSq = distSq;
                        mouseConnection = c;
                    }
                        

                    c.button = new Rect(middle - connectionButtonSize / 2, connectionButtonSize);

                    if (c.condition != null)
                    {
                        GUI.DrawTexture(c.button, connectionButton);
                    }
                }
            }

            #endregion

            #region Draw Nodes

            foreach (Node node in graph)
            {
                NodeEditor.GetEditor(node, this).OnGUI(panOffset * zoom, agent != null && node == agent.State, () => SetConnectingNode(node));
            }

            void SetConnectingNode(Node node)
            {
                if (connectingNode != null)
                {
                    connectingNode = null;
                    wantsMouseMove = false;
                }
                else
                {
                    connectingNode = node;
                    wantsMouseMove = true;
                }
            }

            #endregion

            int controlID = GetInstanceID();

            switch (Event.current.type)
            {
                case EventType.MouseDown:
                    {
                        if (Event.current.button == 0)
                        {

                            if (connectingNode != null)
                            {
                                Node get = GetNode(mouse);
                                if (get != null)
                                {
                                    if (connectingNode.Connect(get, out Connection connection))
                                    {
                                        connection.name = "Connection (" + connectingNode.name + " -> " + get.name + ")";
                                        AssetDatabase.AddObjectToAsset(connection, graph);
                                        if (autoSave) AssetDatabase.SaveAssets();
                                    }
                                }
                                connectingNode = null;
                                Repaint();
                            }
                            else
                            {
                                Node get = GetNode(mouse);
                                if (get != null)
                                {
                                    if (Selection.Contains(get))
                                    {
                                        draggedNode = (from UnityEngine.Object obj in Selection.objects
                                                       where obj is Node && (obj as Node).graph == graph
                                                       select obj as Node).ToList();

                                        dragStart = (from Node node in draggedNode
                                                     select mouse - node.rect.position).ToList();
                                    }
                                    else
                                    {
                                        draggedNode = new List<Node> { get };
                                        dragStart = new List<Vector2> { mouse - get.rect.position };

                                        Selection.activeInstanceID = get.GetInstanceID();
                                        EditorGUIUtility.PingObject(get);
                                    }

                                    Repaint();
                                }
                                else if (mouseConnection != null)
                                {
                                    Selection.activeInstanceID = mouseConnection.GetInstanceID();
                                    EditorGUIUtility.PingObject(mouseConnection);
                                    Repaint();
                                }
                                else
                                {
                                    Selection.activeObject = null;
                                    Repaint();
                                }
                            }

                            if (draggedNode == null)
                            {
                                selectionRect = new Rect(mouse, Vector2.zero);
                                selecting = true;
                                GUIUtility.hotControl = controlID;
                                Repaint();
                            }
                        }
                    }
                    break;
                case EventType.MouseUp:
                    {
                        if (Event.current.button == 0)
                        {
                            draggedNode = null;
                            selectionRect = default;
                            selecting = false;
                            Repaint();
                        }
                    }
                    break;
                case EventType.MouseMove:
                    {
                        if (connectingNode != null)
                            Repaint();
                        else
                            wantsMouseMove = false;
                    }
                    break;
                case EventType.MouseDrag:
                    {
                        if (Event.current.button == 0)
                        {
                            if (draggedNode != null)
                            {
                                for (int i = 0; i < draggedNode.Count; i++)
                                {
                                    Node node = draggedNode[i];
                                    node.rect.position = mouse - dragStart[i];
                                    node.rect.position = Snap(node.rect.position, 25);
                                }

                                Repaint();
                            }

                            else if (selecting)
                            {
                                selectionRect.size = mouse - selectionRect.position;
                                Selection.instanceIDs = (from Node node in graph where selectionRect.Overlaps(node.rect, true) select node.GetInstanceID()).ToArray();
                                Repaint();
                            }
                        }

                        else if (Event.current.button == 2)
                        {
                            panOffset += Event.current.delta / zoom;
                            Repaint();
                        }
                    }
                    break;
                case EventType.KeyDown:
                    {
                        if (Event.current.keyCode == KeyCode.Backspace)
                        {
                            foreach(UnityEngine.Object obj in Selection.objects)
                            {
                                if (obj is Node)
                                {
                                    Node node = obj as Node;
                                    if (node.graph != graph)
                                        continue;

                                    DeleteNode(node);
                                }
                                else if (obj is Connection)
                                {
                                    Connection connection = obj as Connection;
                                    if (connection.graph != graph)
                                        continue;

                                    DeleteConnection(connection);
                                }
                            }
                        }
                    }
                    break;
                case EventType.KeyUp:
                    break;
                case EventType.ScrollWheel:
                    {
                        float scrollDelta = Event.current.delta.y * ZoomSensitivity;
                        scrollDelta = Mathf.Clamp(zoom + scrollDelta, MinZoom, MaxZoom) - zoom;
                        if (Mathf.Abs(scrollDelta) > 0.0001f)
                        {
                            panOffset += mouse / zoom;
                            zoom += scrollDelta;
                            panOffset -= mouse / zoom;
                            Repaint();
                        }
                    }
                    break;
                case EventType.Repaint:
                    {
                        if (connectingNode != null)
                            DrawConnection(connectingNode.Button.center + panOffset * zoom, mouse + panOffset * zoom, out _, out _, Color.white);

                        DrawSelectionBox();
                    }
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
                    {
                        if (GUIUtility.hotControl == controlID && Event.current.rawType == EventType.MouseUp)
                        {
                            if (Event.current.button == 0)
                            {
                                draggedNode = null;
                                selectionRect = default;
                                selecting = false;
                                Repaint();
                            }
                        }
                    }
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
                            menu.AddItem(new GUIContent("Delete"), false, () => DeleteNode(get));
                        }
                        else
                        {

                            foreach (Type type in NodeEditor.NodeTypes)
                            {
                                if (type == typeof(EntryNode))
                                    continue;
                                if (type == typeof(AnyNode))
                                    continue;
                                string path = GetNodeMenuName(type);

                                if (string.IsNullOrEmpty(path))
                                    continue;

                                menu.AddItem(new GUIContent("Add Node/" + path), false, () => CreateNode(type, mouse));
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

        private void DrawSelectionBox()
        {
            Matrix4x4 m = GUI.matrix;
            GUI.matrix = prevGuiMatrix;
            Rect sr = selectionRect;

            if (sr.width * sr.height != 0)
            {
                sr.min /= zoom;
                sr.max /= zoom;
                sr.min += panOffset;
                sr.max += panOffset;
                sr.min += new Vector2(0, topPadding - topPadding * zoom);
                sr.max += new Vector2(0, topPadding - topPadding * zoom);
                sr = new Rect(Mathf.Min(sr.xMin, sr.xMax), Mathf.Min(sr.yMin, sr.yMax), Mathf.Abs(sr.width), Mathf.Abs(sr.height));
                GUI.Box(sr, "", new GUIStyle("selectionRect"));
            }

            GUI.matrix = m;
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
            if (node == null)
                return;

            foreach(Connection c in node.connections)
                AssetDatabase.RemoveObjectFromAsset(c);

            foreach(Node n in graph)
                foreach(Connection c in n.connections)
                    if (c.to == node)
                        AssetDatabase.RemoveObjectFromAsset(c);

            graph.RemoveNode(node);

            AssetDatabase.RemoveObjectFromAsset(node);
            if (autoSave) AssetDatabase.SaveAssets();
            Repaint();
        }

        public void DeleteConnection(Connection connection)
        {
            if (connection == null)
                return;

            graph.RemoveConnection(connection);

            AssetDatabase.RemoveObjectFromAsset(connection);
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
