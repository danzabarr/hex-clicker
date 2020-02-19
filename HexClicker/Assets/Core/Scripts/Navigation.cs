using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class Navigation
{
    private static List<Edge> edges;
    private static Dictionary<Vector2Int, Node> nodes;

    public static void GenerateNavigationGraph()
    {
        int res = HexMap.NavigationResolution;
        float size = HexMap.TileSize;

        edges = new List<Edge>();
        nodes = new Dictionary<Vector2Int, Node>();

        int i = 0;

        Profiler.BeginSample("Generate Nodes");

        HexMap map = HexMap.Instance;

        foreach (HexTile tile in map)
        {
            HexTile neighbourX = tile.Neighbours[2];
            HexTile neighbourZ = tile.Neighbours[3];
            HexTile neighbourY = tile.Neighbours[4];

            for (int x = -res; x <= res; x++)
                for (int y = -res; y <= res; y++)
                    for (int z = -res; z <= res; z++)
                    {
                        if (x + y + z != 0)
                            continue;

                        if (z < -res + 1 && neighbourZ != null)
                            continue;

                        if (x >= res && neighbourX != null)
                            continue;

                        if (y >= res && neighbourY != null)
                            continue;

                        Vector3 world = map.OnTerrain(tile.transform.position.x + (-x + -y / 2f) * size / res, tile.transform.position.z + (-y * HexUtils.SQRT_3 / 2f) * size / res);

                        if (world.y < HexMap.NavigationMinHeight || world.y > HexMap.NavigationMaxHeight)
                            continue;

                        Vector2Int hex = new Vector2Int(-x + (tile.Position.x - tile.Position.y) * res, -y + (tile.Position.x + tile.Position.y * 2) * res);
                        i++;

                        nodes.Add(hex, new Node(hex, world));
                    }
        }

        Profiler.EndSample();

        Profiler.BeginSample("Join Nodes");
        foreach (Node node in nodes.Values)
        {
            if (node == null)
                continue;
            if (nodes.TryGetValue(node.Hex + new Vector2Int(0, -1), out Node n1))
                if (Connect(node, n1, out Edge e, false))
                    edges.Add(e);

            if (nodes.TryGetValue(node.Hex + new Vector2Int(-1, 0), out Node n2))
                if (Connect(node, n2, out Edge e, false))
                    edges.Add(e);

            if (nodes.TryGetValue(node.Hex + new Vector2Int(-1, 1), out Node n3))
                if (Connect(node, n3, out Edge e, false))
                    edges.Add(e);
        }
        Profiler.EndSample();

        //RemoveNodesMinNeighbours(nodes, edges, 1);
        //RemoveNodesMaxNeighbours(nodes, edges, 5);

        //RemoveNodesColinear(nodes, edges);

        /*
        RemoveNodesColinearNeighbours(nodes, edges);
        RemoveNodesColinear(nodes, edges);
        */

        //RemoveNodesMinNeighbours(nodes, edges, 2);
        //AddConnections(map, nodes, edges, 0, .3f, .25f);
    }

    [System.Serializable]
    public class Node : PathFinding.INode
    {
        public Vector2Int Hex { get; private set; }
        public Vector3 Position { get; private set; }
        public List<Neighbour> Neighbours { get; private set; } = new List<Neighbour>();
        public int PathParent { get; set; } = -1;
        public int PathIndex { get; set; } = -1;
        public float PathDistance { get; set; }
        public float PathCrowFliesDistance { get; set; }
        public float PathCost { get; set; }
        public int PathSteps { get; set; }
        public int PathTurns { get; set; }
        public int PathEndDirection { get; set; }
        public bool Accessible { get; set; } = true;
        public bool Open { get; set; }
        public bool Closed { get; set; }
        public int NeighboursCount => Neighbours.Count;
        PathFinding.INode PathFinding.INode.Neighbour(int i) => Neighbours[i].Node;
        public float NeighbourDistance(int i) => Neighbours[i].Distance;
        public float NeighbourCost(int i) => Neighbours[i].Distance * Neighbours[i].Edge.CostMultiplier;
        public bool NeighbourAccessible(int i) => true;
        public float EuclideanDistance(PathFinding.INode node) => Vector3.Distance(Position, ((Node)node).Position);
        public float XZEuclideanDistance(PathFinding.INode node) => Vector2.Distance(Position.xz(), ((Node)node).Position.xz());

        public Node(Vector2Int hex, Vector3 position)
        {
            Hex = hex;
            Position = position;
        }

        public Node(Vector3 position)
        {
            Position = position;
        }

        public void RemoveLastAddedNeighbour()
        {
            if (Neighbours.Count > 0)
                Neighbours.RemoveAt(Neighbours.Count - 1);
        }
    }

    public class Edge
    {
        public Node Node1 { get; private set; }
        public Node Node2 { get; private set; }
        public float Length { get; private set; }
        public float CostMultiplier { get; set; }
        public Edge(Node n1, Node n2, float precalculatedLength)
        {
            Node1 = n1;
            Node2 = n2;
            Length = precalculatedLength;
        }
    }

    [System.Serializable]
    public class Neighbour
    {
        public Node Node { get; private set; }
        public Edge Edge { get; private set; }
        public float Distance => Edge.Length;
        public Neighbour(Node node, Edge edge)
        {
            Node = node;
            Edge = edge;
        }
    }

    public static bool Connect(Node n1, Node n2, out Edge edge, bool check = true)
    {
        edge = default;

        if (n1 == null || n2 == null)
            return false;

        if (check && Connected(n1, n2, out _))
            return false;

        float distance = n1.EuclideanDistance(n2);
        edge = new Edge(n1, n2, distance);
        n1.Neighbours.Add(new Neighbour(n2, edge));
        n2.Neighbours.Add(new Neighbour(n1, edge));
        return true;
    }

    public static bool Connect(Node n1, Node n2, float precalculatedLength, out Edge edge, bool check = true)
    {
        edge = default;

        if (n1 == null || n2 == null)
            return false;

        if (check && Connected(n1, n2, out _))
            return false;

        edge = new Edge(n1, n2, precalculatedLength);
        n1.Neighbours.Add(new Neighbour(n2, edge));
        n2.Neighbours.Add(new Neighbour(n1, edge));
        return true;
    }

    public static bool Connected(Node n1, Node n2, out Edge edge)
    {
        edge = null;
        foreach (Neighbour n in n1.Neighbours)
            if (n.Node == n2)
            {
                edge = n.Edge;
                return true;
            }
        return false;
    }

    public static void DrawPath(PathFinding.Path<Node> path, bool drawSpheres = false, bool labelNodes = false, bool labelEdges = false)
    {
        if (path == null)
            return;

        for (int i = 0; i < path.Nodes.Count - 1; i++)
            Gizmos.DrawLine(path.Nodes[i].Position, path.Nodes[i + 1].Position);

        if (labelEdges)
            for (int i = 0; i < path.Nodes.Count - 1; i++)
            {
                Vector3 midPoint = (path.Nodes[i].Position + path.Nodes[i + 1].Position) / 2f;
                float length = path.Nodes[i].EuclideanDistance(path.Nodes[i + 1]);
                Handles.Label(midPoint, length + "");
            }

        if (drawSpheres)
            foreach(Node node in path.Nodes)
                Gizmos.DrawSphere(node.Position, 0.02f);

        if (labelNodes)
            foreach (Node node in path.Nodes)
                Handles.Label(node.Position, node.Position + "");
    }

    /*
    private static void RemoveNodesOutsideHeightRange(Dictionary<Vector2Int, Node> nodes, List<Edge> edges, float min, float max)
    {
        List<Node> toDelete = new List<Node>();
        foreach (KeyValuePair<Vector2Int, Node> node in nodes.Where(pair => pair.Value.Position.y < min || pair.Value.Position.y > max).ToList())
        {
            RemoveNode(nodes, edges, node.Value);
            nodes.Remove(node.Key);
        }
    }

    private static void RemoveNodesMinNeighbours(Dictionary<Vector2Int, Node> nodes, List<Edge> edges, int minNeighbours)
    {
        while (nodes.Count > 0)
        {
            bool changed = false;
            foreach (KeyValuePair<Vector2Int, Node> node in nodes.Where(pair => pair.Value.Neighbours.Count < minNeighbours).ToList())
            {
                RemoveNode(nodes, edges, node.Value);
                changed = true;
                nodes.Remove(node.Key);
            }
            if (!changed)
                return;
        }
    }
    
    private static void AddConnections(HexMap map, Dictionary<Vector2Int, Node> nodes, List<Edge> edges, float minHeight, float maxHeight, float sampleResolution)
    {
        foreach (Node n0 in nodes.Values)
        {
            foreach (Node n1 in nodes.Values)
            {
                if (n0 == n1)
                    continue;

                if (Connected(n0, n1, out _))
                    continue;

                if (PathIntersectsEdge(n0, n1, edges, out _, out _))
                    continue;

                if (PathCrossesUntraversable(map, n0.Position.xz(), n1.Position.xz(), minHeight, maxHeight, sampleResolution, out float distance))
                    continue;

                Connect(n0, n1, distance, out _, false);
            }
        }
    }

    private static void RemoveNodesMaxNeighbours(Dictionary<Vector3Int, Node> nodes, List<Edge> edges, int maxNeighbours)
    {
        List<Node> toDelete = new List<Node>();
        foreach (Node node in nodes.Values)
            if (node.Neighbours.Count > maxNeighbours)
                toDelete.Add(node);

        foreach (Node node in toDelete)
        {
            RemoveNode(nodes, edges, node);
            nodes.Remove(node.Hex);
        }
    }

    private static void RemoveNodesColinear(Dictionary<Vector3Int, Node> nodes, List<Edge> edges)
    {
        while (nodes.Count > 0)
        {
            bool changed = false;
            List<Node> toDelete = new List<Node>();

            foreach (Node node in nodes.Values)
            {
                if (node.Neighbours.Count != 2)
                    continue;
                Neighbour n0 = node.Neighbours[0];
                Neighbour n1 = node.Neighbours[1];
                if (IsColinearXZ(n0.Node, n1.Node) && IsColinearXZ(n0.Node, node))
                {
                    if (Connect(n0.Node, n1.Node, out Edge e, true))
                    {
                        RemoveNode(nodes, edges, node);
                        toDelete.Add(node);
                        edges.Add(e);
                        changed = true;
                    }
                }
            }

            foreach (Node node in toDelete)
                nodes.Remove(node.Hex);

            if (!changed)
                return;
        }
    }

    public static bool IsColinearXZ(Node n1, Node n2)
    {
        Vector3Int cube1 = HexUtils.HexToCube(n1.Hex);
        Vector3Int cube2 = HexUtils.HexToCube(n2.Hex);

        if (cube1.x == cube2.x)
            return true;
        if (cube1.y == cube2.y)
            return true;
        if (cube1.z == cube2.z)
            return true;

        return false;
    }

    private static void RemoveNodesColinearNeighbours(List<Node> nodes, List<Edge> edges)
    {
        while (nodes.Count > 0)
        {
            bool changed = false;
            for (int i = nodes.Count - 1; i >= 1; i--)
            {
                if (nodes[i].Neighbours.Count != 2)
                    continue;

                Neighbour n0 = nodes[i].Neighbours[0];
                Neighbour n1 = nodes[i].Neighbours[1];

                if (IsColinearXZ(n0.Node, n1.Node) && Connected(n0.Node, n1.Node, out _))
                {
                    RemoveNode(nodes, edges, nodes[i]);
                    changed = true;
                }
            }

            if (!changed)
                return;
        }
    }

    private static void RemoveNode(List<Node> nodes, List<Edge> edges, Node node)
    {
        foreach (Neighbour neighbour in node.Neighbours)
        {
            for (int n = 0; n < neighbour.Node.Neighbours.Count; n++)
            {
                Neighbour nn = neighbour.Node.Neighbours[n];
                if (nn.Node == node)
                {
                    neighbour.Node.Neighbours.RemoveAt(n);
                    edges.Remove(neighbour.Edge);
                    break;
                }
            }
        }

        nodes.Remove(node);
    }
    
    private static void RemoveNode(Dictionary<Vector2Int, Node> nodes, List<Edge> edges, Node node)
    {
        foreach (Neighbour neighbour in node.Neighbours)
        {
            for (int n = 0; n < neighbour.Node.Neighbours.Count; n++)
            {
                Neighbour nn = neighbour.Node.Neighbours[n];
                if (nn.Node == node)
                {
                    neighbour.Node.Neighbours.RemoveAt(n);
                    edges.Remove(neighbour.Edge);
                    break;
                }
            }
        }
        //nodes.Remove(node.Hex);
    }
    */
    public static void Clear()
    {
        nodes = null;
        edges = null;
    }

    public static void OnDrawGizmos()
    {
        if (edges != null)
        {
            Gizmos.color = Color.red;
            foreach (Navigation.Edge edge in edges)
            {
                Gizmos.DrawLine(edge.Node1.Position, edge.Node2.Position);
            }
        }
        if (nodes != null)
        {
            //foreach(Node node in navigationNodes.Values)
            //{
            //    Handles.Label(node.Position, node.Hex + "");
            //}

            /*
            start.transform.position = OnTerrain(start.transform.position);
            end.transform.position = OnTerrain(end.transform.position);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(start.transform.position, end.transform.position);
            Vector2 startHex = HexUtils.CartesianToVertex(start.transform.position.x, start.transform.position.z, Size, navMeshResolution);
            Vector2 endHex = HexUtils.CartesianToVertex(end.transform.position.x, end.transform.position.z, Size, navMeshResolution);
            float hexDistance = Vector3.Distance(start.transform.position, end.transform.position);// HexUtils.HexDistance(startHex, endHex);

            for (float i = 0; i < hexDistance; i += Size / navMeshResolution)
            {
                Vector3 lerp = Vector3.Lerp(start.transform.position, end.transform.position, i / hexDistance);
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(lerp, 0.05f);
                Vector2 lerpHex = HexUtils.NearestVertex(lerp, Size, navMeshResolution);
                Vector3 cart = OnTerrain(HexUtils.VertexToCartesian(lerpHex.x, lerpHex.y, Size, navMeshResolution));
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(cart, 0.05f);

                List<Node> nearestLinks = NearestNodes(lerp);

                foreach (Node link in nearestLinks)
                    Gizmos.DrawLine(lerp, link.Position);
            }
            */
        }
    }

    /*
    private static bool PathIntersectsEdge(Node start, Node end, List<Edge> edges, out Vector2 intersection, out Edge edge)
    {
        intersection = default;
        edge = default;

        Vector2 startPos = start.Position.xz();
        Vector2 endPos = end.Position.xz();

        foreach (Edge e in edges)
        {
            if (e.Node1 == start.Index || e.Node1 == end.Index || e.Node1 == start.Index || e.Node1 == end.Index)
                continue;

            if (LineSegmentIntersection(startPos, endPos, e.Node1.Position.xz(), e.Node2.Position.xz(), out Vector2 i))
            {
                intersection = i;
                edge = e;
                return true;
            }
        }
        return false;
    }

    private static bool PathCrossesUntraversable(HexMap map, Vector2 start, Vector2 end, float minHeight, float maxHeight, float sampleResolution, out float distance)
    {
        distance = Vector2.Distance(start, end);
        int samples = Mathf.CeilToInt(1f / sampleResolution * distance);

        for (int i = 1; i < samples; i++)
        {
            Vector2 sample = Vector2.Lerp(start, end, i / (float)samples);
            float height = map.SampleHeight(sample.x, sample.y);
            if (height < minHeight || height > maxHeight)
                return true;

            if (!map.SampleTile(sample.x, sample.y, out _))
                return true;
        }
        return false;
    }

    public static bool LineRayIntersection(Vector2 rayOrigin, Vector2 rayDirection, Vector2 point1, Vector2 point2, out Vector2 intersection, out float distance)
    {
        Vector2 v1 = rayOrigin - point1;
        Vector2 v2 = point2 - point1;
        Vector2 v3 = new Vector2(-rayDirection.y, rayDirection.x);

        float dot = v2.Dot(v3);
        if (Mathf.Abs(dot) < 0.0000001f)
        {
            intersection = Vector2.zero;
            distance = -1;
            return false;
        }

        float t1 = v2.Cross(v1) / dot;
        float t2 = v1.Dot(v3) / dot;

        if (t1 >= 0.0 && (t2 >= 0.0 && t2 <= 1.0))
        {
            intersection = rayOrigin + rayDirection * t1;
            distance = t1;
            return true;
        }

        intersection = Vector2.zero;
        distance = -1;
        return false;
    }

    public static bool LineSegmentIntersection(Vector2 l1Start, Vector2 l1End, Vector2 l2Start, Vector2 l2End, out Vector2 intersection)
    {
        intersection = Vector3.zero;
        float deltaACy = l1Start.y - l2Start.y;
        float deltaDCx = l2End.x - l2Start.x;
        float deltaACx = l1Start.x - l2Start.x;
        float deltaDCy = l2End.y - l2Start.y;
        float deltaBAx = l1End.x - l1Start.x;
        float deltaBAy = l1End.y - l1Start.y;

        float denominator = deltaBAx * deltaDCy - deltaBAy * deltaDCx;
        float numerator = deltaACy * deltaDCx - deltaACx * deltaDCy;

        if (denominator == 0)
        {
            //return false;
            if (numerator == 0)
            {
                // collinear. Potentially infinite intersection points.
                // Check and return one of them.
                if (l1Start.x >= l2Start.x && l1Start.x <= l2End.x)
                {
                    intersection = l1Start;
                    return true;
                }
                else if (l2Start.x >= l1Start.x && l2Start.x <= l1End.x)
                {
                    intersection = l2Start;
                    return true;
                }
                else
                {
                    //    return false;
                }
            }
            else
            { // parallel
                return false;
            }
        }

        float r = numerator / denominator;
        if (r <= 0 || r >= 1)
        {
            return false;
        }

        float s = (deltaACy * deltaBAx - deltaACx * deltaBAy) / denominator;
        if (s <= 0 || s >= 1)
        {
            return false;
        }

        intersection = new Vector2((float)(l1Start.x + r * deltaBAx), (float)(l1Start.y + r * deltaBAy));
        return true;
    }
    */

    public static bool NearestNode(Vector3 position, float size, int resolution, out Node node)
    {
        Vector2Int vertex = HexUtils.NearestVertex(position, size, resolution);
        return nodes.TryGetValue(vertex, out node);
    }

    public static List<Node> NearestNodes(Vector3 position)
    {
        List<Node> nearest = new List<Node>();
        if (nodes == null)
            return nearest;
        Vector2Int[] vertices = HexUtils.NearestThreeVertices(position, HexMap.TileSize, HexMap.NavigationResolution);

        for (int i = 0; i < 3; i++)
            if (nodes.TryGetValue(vertices[i], out Node node))
                nearest.Add(node);

        return nearest;
    }

    public static void SnapToNode(Transform transform, float size, int resolution)
    {
        if (NearestNode(transform.position, size, resolution, out Node node))
            transform.transform.position = node.Position;
    }

    [System.Serializable]
    public class PathIterator
    {
        private Vector3[] points;
        private float[] distances;
        private int i;
        private float d0, d1;
        public float TotalDistance { get; private set; }
        public float CurrentDistance { get; private set; }
        public float T => Mathf.Clamp(CurrentDistance / TotalDistance, 0, 1);
        public Vector3 CurrentPosition { get; private set; }
        public PathIterator(PathFinding.Path<Node> path)
        {
            points = new Vector3[path.Count];
            for (int i = 0; i < path.Nodes.Count; i++)
                points[i] = path.Nodes[i].Position;

            distances = new float[path.Nodes.Count - 1];
            for (int i = 0; i < path.Nodes.Count - 1; i++)
            {
                distances[i] = Vector3.Distance(path.Nodes[i].Position, path.Nodes[i + 1].Position);
                TotalDistance += distances[i];
            }
            CurrentPosition = CalculatePosition(0);
            d0 = 0;
            if (distances.Length > 0)
                d1 = distances[0];
        }

        public void SetTime(float t) => SetDistance(t * TotalDistance);

        public void SetDistance(float distance)
        {
            if (distance == CurrentDistance)
                return;

            if (points.Length == 0)
            {
                i = 0;
                d0 = 0;
                d1 = 0;
                CurrentDistance = 0;
                CurrentPosition = Vector3.zero;
                return;
            }

            if (points.Length == 1 || distance < 0)
            {
                i = 0;
                d0 = 0;
                d1 = 0;
                CurrentDistance = 0;
                CurrentPosition = points[0];
                return;
            }

            if (distance >= TotalDistance)
            {
                i = points.Length - 1;
                d0 = TotalDistance - distances[distances.Length - 1];
                d1 = TotalDistance;
                CurrentDistance = TotalDistance;
                CurrentPosition = points[points.Length - 1];
                return;
            }

            CurrentDistance = distance;
            float sum = 0;
            for (int i = 0; i < points.Length - 1; i++)
            {
                float d = distances[i];
                d0 = sum;
                d1 = sum + d;
                if (distance < sum + d)
                {
                    float t = (distance - sum) / d;
                    CurrentPosition = Vector3.Lerp(points[i], points[i + 1], t);
                    this.i = i;
                    return;
                }
                sum += d;
            }

            CurrentPosition = points[points.Length - 1];
        }

        public void AdvanceDistance(float amount)
        {
            if (amount == 0)
                return;

            if (points.Length <= 1)
                return;

            float distance = Mathf.Clamp(CurrentDistance + amount, 0, TotalDistance);
            if (distance == CurrentDistance)
                return;
            CurrentDistance = distance;

            if (amount > 0)
            {
                float sum = d0;
                for (; i < distances.Length; i++)
                {
                    float d = distances[i];
                    d0 = sum;
                    d1 = sum + d;
                    if (distance < sum + d)
                    {
                        float t = (distance - sum) / d;
                        CurrentPosition = Vector3.Lerp(points[i], points[i + 1], t);
                        return;
                    }
                    sum += d;
                }
                i--;
                CurrentPosition = points[points.Length - 1];
            }

            else if (amount < 0)
            {
                float sum = d1;
                for (; i >= 0; i--)
                {
                    float d = distances[i];
                    d0 = sum - d;
                    d1 = sum;
                    if (distance >= sum - d)
                    {
                        float t = (sum - distance) / d;
                        CurrentPosition = Vector3.Lerp(points[i], points[i + 1], 1 - t);
                        return;
                    }
                    sum -= d;
                }
                CurrentPosition = points[0];
            }
        }

        public Vector3 CalculatePosition(float distance)
        {
            if (points.Length == 0)
                return Vector3.zero;

            if (points.Length == 1)
                return points[0];

            if (distance < 0)
                return points[0];

            if (distance >= TotalDistance)
                return points[points.Length - 1];

            float sum = 0;

            for (int i = 0; i < points.Length - 1; i++)
            {
                float d = distances[i];
                if (distance < sum + d)
                {
                    float t = (distance - sum) / d;

                    return Vector3.Lerp(points[i], points[i + 1], t);
                }
                sum += d;
            }

            return points[points.Length - 1];
        }
    }

    public class PathFindThreaded
    {
        private Thread thread;
        private Vector3 start, end;
        private float maxDistance;
        private int maxTries;
        private PathFinding.CostFunction costFunction;
        private bool raycastModifier;
        private float sampleFrequency;
        public bool Completed { get; private set; }
        public PathFinding.Path<Node> Path { get; private set; }
        public List<Node> Visited { get; private set; }
        public PathFinding.Result Result { get; private set; }
        public PathFindThreaded(Vector3 start, Vector3 end, float maxDistance, int maxTries, PathFinding.CostFunction costFunction)
        {
            this.start = start;
            this.end = end;
            this.maxDistance = maxDistance;
            this.maxTries = maxTries;
            this.costFunction = costFunction;

            thread = new Thread(Run);
            thread.Start();
        }

        public PathFindThreaded(Vector3 start, Vector3 end, float maxDistance, int maxTries, PathFinding.CostFunction costFunction, float sampleFrequency)
        {
            this.start = start;
            this.end = end;
            this.maxDistance = maxDistance;
            this.maxTries = maxTries;
            this.costFunction = costFunction;
            raycastModifier = true;
            this.sampleFrequency = sampleFrequency;

            thread = new Thread(Run);
            thread.Start();
        }

        void Run()
        {
            Result = PathFind(start, end, maxDistance, maxTries, costFunction, out PathFinding.Path<Node> path, out List<Node> visited, !raycastModifier);
            Path = path;
            Visited = visited;

            if (raycastModifier)
            {
                RaycastModifier(path, sampleFrequency);

                foreach (Node node in visited)
                    PathFinding.ClearPathFindingData(node);
            }

            Completed = true;
            thread.Abort();
        }

        public void Abort()
        {
            thread.Abort();
        }
    }

    public static PathFinding.Result PathFind(Vector3 start, Vector3 end, float maxDistance, int maxTries, PathFinding.CostFunction costFunction, out PathFinding.Path<Node> path, out List<Node> visited, bool cleanUpOnSuccess = true)
    {
        path = null;
        visited = new List<Node>();

        float size = HexMap.TileSize;

        Node startNode = new Node(start);
        Node endNode = new Node(end);

        List<Node> startNeighbours = NearestNodes(start);
        if (startNeighbours.Count <= 0)
        {
            return PathFinding.Result.FailureNoPath;
        }

        List<Node> endNeighbours = NearestNodes(end);
        if (endNeighbours.Count <= 0)
        {
            return PathFinding.Result.FailureNoPath;
        }

        foreach (Node neighbour in startNeighbours)
            startNode.Neighbours.Add(new Neighbour(neighbour, new Edge(startNode, neighbour, startNode.EuclideanDistance(neighbour))));

        foreach (Node neighbour in endNeighbours)
            Connect(neighbour, endNode, out _, false);

        PathFinding.Result result = PathFinding.PathFind(startNode, endNode, maxDistance, maxTries, costFunction, out path, out visited, cleanUpOnSuccess);

        foreach (Node neighbour in endNeighbours)
            neighbour.RemoveLastAddedNeighbour();

        return result;
    }

    public static void RaycastModifier(PathFinding.Path<Node> path, float sampleFrequency)
    {
        if (path == null)
            return;

        if (path.Count <= 2)
            return;

        HexMap map = HexMap.Instance;

        int startIndex = 0;

        float step = sampleFrequency * HexMap.TileSize / HexMap.NavigationResolution;

        while (path.Count > startIndex)
        {
            for (int i = path.Count - 1; i > startIndex; i--)
            {
                float pathDistance = path[i].PathDistance - path[startIndex].PathDistance;

                float xzDistance = Vector2.Distance(path[startIndex].Position.xz(), path[i].Position.xz());

                bool valid = true;

                List<Vector3> points = new List<Vector3>();
                float shortCutDistance = 0;
                Vector3 lastPoint = path[startIndex].Position;

                for (float s = step; s < xzDistance; s += step)
                {
                    Vector2 lerp = Vector2.Lerp(path[startIndex].Position.xz(), path[i].Position.xz(), s / xzDistance);
                    Vector3 onTerrain = new Vector3(lerp.x, map.SampleHeight(lerp.x, lerp.y), lerp.y);
                    points.Add(onTerrain);
                    shortCutDistance += Vector3.Distance(lastPoint, onTerrain);
                    lastPoint = onTerrain;

                    if (shortCutDistance > pathDistance)
                    {
                        valid = false;
                        break;
                    }

                    if (!map.SampleTile(lerp.x, lerp.y))
                    {
                        valid = false;
                        break;
                    }

                    if (onTerrain.y < HexMap.NavigationMinHeight || onTerrain.y > HexMap.NavigationMaxHeight)
                    {
                        valid = false;
                        break;
                    }
                }


                shortCutDistance += Vector3.Distance(lastPoint, path[i].Position);
                if (shortCutDistance > pathDistance)
                    valid = false;

                if (valid)
                {
                    //Debug.Log("(" + startIndex + "-" + i + ") Path distance: " + pathDistance + " Shortcut distance: " + shortCutDistance);
                    path.Nodes.RemoveRange(startIndex + 1, i - startIndex - 1);
                    foreach(Vector3 p in points)
                    {
                        startIndex++;
                        path.Nodes.Insert(startIndex, new Node(p));
                    }
                    break;
                }
            }
            startIndex++;
        }
    }
}