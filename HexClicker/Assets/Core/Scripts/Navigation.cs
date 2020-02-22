using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class Navigation
{
    public static readonly int Resolution = 96;
    public static readonly float MinHeight = 0.0f;
    public static readonly float MaxHeight = 1.25f;
    public static readonly float MaxDesirePathCost = 20;

    public class Node : PathFinding.INode
    {
        private float desirePathCost = MaxDesirePathCost;
        public float DesirePathCost
        {
            get => desirePathCost;
            set => desirePathCost = Mathf.Clamp(value, 0, MaxDesirePathCost);
        }
        public float MovementCost => DesirePathCost;// + roads + other stuff;
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
        public float NeighbourCost(int i) => Neighbours[i].Distance * (MovementCost + Neighbours[i].Node.MovementCost) / 2;
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
        public Edge(Node n1, Node n2, float precalculatedLength)
        {
            Node1 = n1;
            Node2 = n2;
            Length = precalculatedLength;
        }
    }
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
    [System.Serializable]
    
    public class PathRequest
    {
        private readonly Thread thread;
        private readonly Vector3 start, end;
        private readonly float maxDistance;
        private readonly int maxTries;
        private readonly PathFinding.CostFunction costFunction;
        private readonly bool raycastModifier;
        public bool Completed { get; private set; }
        public bool Cancelled { get; private set; }
        public PathFinding.Path<Node> Path { get; private set; }
        public List<Node> Visited { get; private set; }
        public PathFinding.Result Result { get; private set; }

        public PathRequest(Vector3 start, Vector3 end, float maxDistance, int maxTries, PathFinding.CostFunction costFunction, bool raycastModifier)
        {
            this.start = start;
            this.end = end;
            this.maxDistance = maxDistance;
            this.maxTries = maxTries;
            this.costFunction = costFunction;
            this.raycastModifier = raycastModifier;
        }

        public void Queue()
        {
            Enqueue(this);
        }

        public void Cancel()
        {
            Cancelled = true;
        }

        public void Execute()
        {
            Result = PathFind(start, end, maxDistance, maxTries, costFunction, out PathFinding.Path<Node> path, out List<Node> visited, !raycastModifier);
            Path = path;
            Visited = visited;

            if (raycastModifier)
            {
                RaycastModifier(path);

                foreach (Node node in visited)
                    PathFinding.ClearPathFindingData(node);
            }

            Completed = true;
        }
    }

    private static List<Edge> edges;
    private static Dictionary<Vector2Int, Node> nodes;
    private static Thread thread;
    private static BlockingCollection<PathRequest> queue = new BlockingCollection<PathRequest>();
    public static bool Working { get; private set; }
    public static bool TryGetNode(Vector2Int vertex, out Node node) => nodes.TryGetValue(vertex, out node);
    public static void GenerateNavigationGraph(HexMap map)
    {
        int res = Resolution;
        float size = HexMap.TileSize;

        edges = new List<Edge>();
        nodes = new Dictionary<Vector2Int, Node>();

        int w1 = Mathf.FloorToInt((map.Width - 1) / 2f);
        int w2 = Mathf.CeilToInt((map.Width - 1) / 2f);
        int h1 = Mathf.FloorToInt((map.Height - 1) / 2f);
        int h2 = Mathf.CeilToInt((map.Height - 1) / 2f);

       // int minX = -(int)((1.5f * w2 + 1) * res) - 1;// (int)(res / 2 * HexUtils.SQRT_3 * -map.Width * 2);
       // int maxX = (int)((1.5f * w1 + 1) * res) + 1;// res;
       // int minZ = -(int)(HexUtils.SQRT_3 * (h2 + 1f) * res) - 1;// -(int)(res * HexUtils.SQRT_3 * map.Width);
       // int maxZ = (int)(HexUtils.SQRT_3 * (h1 + .5f) * res) + 1;//(int)(res / 2f * HexUtils.SQRT_3 + 1);

        int minX = -(int)((1.5f * w2 + 1) * res) - 1;
        int maxX = +(int)((1.5f * w1 + 1) * res) + 1;
        int minZ = -(int)(HexUtils.SQRT_3 * (h2 + 1.0f) * res) - 1;
        int maxZ = +(int)(HexUtils.SQRT_3 * (h1 + 0.5f) * res) + 1;

        for (int x = minX; x <= maxX; x++)
            for (int z = minZ; z <= maxZ; z++)
            {
                float nX = x * size / res;
                float nZ = z * size / res;

                if (!map.SampleTile(nX, nZ))
                    continue;

                Vector3 world = map.OnTerrain(nX, nZ);

                if (world.y < MinHeight || world.y > MaxHeight)
                    continue;
                Vector2Int key = new Vector2Int(x, z);

                nodes.Add(key, new Node(key, world));
            }

        foreach (Node node in nodes.Values)
        {
            if (node == null)
                continue;
            if (nodes.TryGetValue(node.Hex + new Vector2Int(-1, -1), out Node n1))
                if (Connect(node, n1, out Edge e, false))
                    edges.Add(e);

            if (nodes.TryGetValue(node.Hex + new Vector2Int(0, -1), out Node n2))
                if (Connect(node, n2, out Edge e, false))
                    edges.Add(e);

            if (nodes.TryGetValue(node.Hex + new Vector2Int(1, -1), out Node n3))
                if (Connect(node, n3, out Edge e, false))
                    edges.Add(e);

            if (nodes.TryGetValue(node.Hex + new Vector2Int(1, 0), out Node n4))
                if (Connect(node, n4, out Edge e, false))
                    edges.Add(e);
        }

        /*
        int i = 0;
        Profiler.BeginSample("Generate Nodes");


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

                        if (world.y < MinHeight || world.y > MaxHeight)
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
        */
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
    public static void Clear()
    {
        nodes = null;
        edges = null;
    }
    public static void OnDrawGizmos()
    {
        if (edges != null)
        {
            Gizmos.color = new Color(1, 1, 1, .2f);
            foreach (Edge edge in edges)
            {
                Gizmos.DrawLine(edge.Node1.Position, edge.Node2.Position);
            }
        }
        if (nodes != null)
        {
            Vector3 size = new Vector3(.5f, .5f, .5f) * HexMap.TileSize / Resolution;
            foreach (Node node in nodes.Values)
            {
                if (!node.Accessible)
                    Gizmos.color = Color.red;
                else
                    Gizmos.color = Color.Lerp(Color.green, Color.white, Mathf.Clamp(node.MovementCost / MaxDesirePathCost, 0, 1));
                Gizmos.DrawCube(node.Position, size);
                //Handles.Label(node.Position, node.Hex + "");
            }

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
    public static bool NearestSquareNode(Vector3 position, out Node node)
    {
        node = default;
        if (nodes == null)
            return false;
        position *= Resolution / HexMap.TileSize;

        return nodes.TryGetValue(new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)), out node);
    }
    public static List<Node> NearestSquareNodes(Vector3 position)
    {

        List<Node> nearest = new List<Node>();

        if (nodes == null)
            return nearest;

        position *= Resolution / HexMap.TileSize;

        int ceilX = Mathf.CeilToInt(position.x);
        int floorX = Mathf.FloorToInt(position.x);

        int ceilZ = Mathf.CeilToInt(position.z);
        int floorZ = Mathf.FloorToInt(position.z);

        if (nodes.TryGetValue(new Vector2Int(floorX, floorZ), out Node n0))
            nearest.Add(n0);

        if (nodes.TryGetValue(new Vector2Int(floorX, ceilZ), out Node n1))
            nearest.Add(n1);

        if (nodes.TryGetValue(new Vector2Int(ceilX, floorZ), out Node n2))
            nearest.Add(n2);

        if (nodes.TryGetValue(new Vector2Int(ceilX, ceilZ), out Node n3))
            nearest.Add(n3);

        return nearest;
    }
    public static bool NearestHexNode(Vector3 position, out Node node)
    {
        Vector2Int vertex = HexUtils.NearestVertex(position, HexMap.TileSize, Resolution);
        return nodes.TryGetValue(vertex, out node);
    }
    public static List<Node> NearestHexNodes(Vector3 position)
    {
        List<Node> nearest = new List<Node>();
        if (nodes == null)
            return nearest;
        Vector2Int[] vertices = HexUtils.NearestThreeVertices(position, HexMap.TileSize, Resolution);

        for (int i = 0; i < 3; i++)
            if (nodes.TryGetValue(vertices[i], out Node node))
                nearest.Add(node);

        return nearest;
    }
    public static void SnapToHexNode(Transform transform)
    {
        if (NearestHexNode(transform.position, out Node node))
            transform.transform.position = node.Position;
    }
    public static void FadeOutPaths(float amount)
    {
        foreach (Node node in nodes.Values)
            node.DesirePathCost += amount;
    }
    public static void Enqueue(PathRequest request)
    {
        queue.Add(request);
        //Debug.Log(queue.Count);
        if (thread == null)
        {
            thread = new Thread(ProcessRequests);
            thread.Start();
        }
    }
    private static void ProcessRequests()
    {
        while(true)
        {
            
            foreach(PathRequest current in queue.GetConsumingEnumerable())
            {
                if (current.Cancelled)
                    continue;

                Working = true;
                current.Execute();
                Working = false;
            }
        }
    }
    public static PathFinding.Result PathFind(Vector3 start, Vector3 end, float maxDistance, int maxTries, PathFinding.CostFunction costFunction, out PathFinding.Path<Node> path, out List<Node> visited, bool cleanUpOnSuccess = true)
    {
        path = null;
        visited = new List<Node>();

        float size = HexMap.TileSize;

        Node startNode = new Node(start);
        Node endNode = new Node(end);

        List<Node> startNeighbours = NearestSquareNodes(start);//NearestHexNodes(start);
        if (startNeighbours.Count <= 0)
        {
            return PathFinding.Result.FailureNoPath;
        }

        List<Node> endNeighbours = NearestSquareNodes(end);//NearestHexNodes(end);
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
    public static void RaycastModifier(PathFinding.Path<Node> path)
    {
        if (path == null)
            return;

        if (path.Count <= 2)
            return;

        HexMap map = HexMap.Instance;

        int startIndex = 0;

        while (path.Count > startIndex)
        {
            for (int i = path.Count - 1; i > startIndex; i--)
            {
                float pathDistance = path[i].PathDistance - path[startIndex].PathDistance;
                float pathCost = path[i].PathCost - path[startIndex].PathCost;

                bool valid = true;
                List<Vector3> points = new List<Vector3>();
                float shortCutDistance = 0;
                float shortCutCost = 0;

                Vector2 p0 = path[startIndex].Position.xz();
                Vector2 p1 = path[i].Position.xz();
                float voxelSize = HexMap.TileSize / Resolution;
                Vector2 voxelOffset = Vector2.one * .5f;// .5f;

                float Step(float x, float y) => y >= x ? 1 : 0;
                Vector2 Vector2Abs(Vector2 a) => new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));

                p0 /= voxelSize;
                p1 /= voxelSize;

                p0 -= voxelOffset;
                p1 -= voxelOffset;

                Vector2 rd = p1 - p0;
                Vector2 p = new Vector2(Mathf.Floor(p0.x), Mathf.Floor(p0.y));
                Vector2 rdinv = Vector2.one / rd;
                Vector2 stp = new Vector2(Mathf.Sign(rd.x), Mathf.Sign(rd.y));
                Vector2 delta = Vector2.Min(rdinv * stp, Vector2.one);
                Vector2 t_max = Vector2Abs((p + Vector2.Max(stp, Vector2.zero) - p0) * rdinv);

                Vector3 lastIntersection = path[startIndex].Position;

                int steps = 0;
                while (steps < 1000)
                {
                    steps++;
                    Vector2Int square = Vector2Int.RoundToInt(p) + Vector2Int.one;

                    if (!TryGetNode(square, out Node node))
                    {
                        valid = false;
                        break;
                    }

                    if (!node.Accessible)
                    {
                        valid = false;
                        break;
                    }

                    //Gizmos.DrawCube(node.Position, new Vector3(voxelSize, .05f, voxelSize));

                    float next_t = Mathf.Min(t_max.x, t_max.y);
                    if (next_t > 1.0)
                    {
                        float finalDistance = Vector3.Distance(lastIntersection, path[i].Position);
                        shortCutDistance += finalDistance;
                        float finalCost = finalDistance * node.MovementCost;
                        shortCutCost += finalCost;

                        //Handles.Label(Vector3.Lerp(lastIntersection, path[i].Position, .5f), node.Hex + " " + finalDistance + " " + finalCost);

                        if (shortCutDistance + shortCutCost > pathDistance + pathCost)
                        {
                            valid = false;
                            break;
                        }
                        break;
                    }
                    
                    Vector3 intersection = map.OnTerrain((p0 + next_t * rd + voxelOffset) * voxelSize);
                    float distance = Vector3.Distance(lastIntersection, intersection);
                    shortCutDistance += distance;

                    float cost = distance * node.MovementCost;
                    shortCutCost += cost;

                    //Handles.Label(Vector3.Lerp(lastIntersection, intersection, .5f), node.Hex + " " + distance + " " + cost);

                    if (shortCutDistance + shortCutCost > pathDistance + pathCost)
                    {
                        valid = false;
                        break;
                    }

                    points.Add(intersection);

                    lastIntersection = intersection;

                    Vector2 cmp = new Vector2(Step(t_max.x, t_max.y), Step(t_max.y, t_max.x));
                    t_max += delta * cmp;
                    p += stp * cmp;
                }
                
                if (valid)
                {
                    //Debug.Log("(" + startIndex + "-" + i + ") Path distance: " + pathDistance + " Shortcut distance: " + shortCutDistance);
                    path.Nodes.RemoveRange(startIndex + 1, i - startIndex - 1);
                    foreach (Vector3 point in points)
                    {
                        startIndex++;
                        path.Nodes.Insert(startIndex, new Node(point));
                    }
                    break;
                }
            }
            startIndex++;
        }
    }

    #region Unused
    /*
    public static void RaycastModifierRegularIntervals(PathFinding.Path<Node> path)
    {
        if (path == null)
            return;

        if (path.Count <= 2)
            return;

        HexMap map = HexMap.Instance;

        int startIndex = 0;

        float sampleFrequency = HexMap.TileSize / Resolution;

        while (path.Count > startIndex)
        {
            for (int i = path.Count - 1; i > startIndex; i--)
            {
                float pathDistance = path[i].PathDistance - path[startIndex].PathDistance;

                bool valid = true;
                List<Vector3> points = new List<Vector3>();
                float shortCutDistance = 0;
                Vector3 lastPoint = path[startIndex].Position;

                float xzDistance = Vector2.Distance(path[startIndex].Position.xz(), path[i].Position.xz());
                for (float s = sampleFrequency; s < xzDistance; s += sampleFrequency)
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

                    if (onTerrain.y < MinHeight || onTerrain.y > MaxHeight)
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

    public static void VoxelTraverse(Vector2 p0, Vector2 p1, float voxelSize)
    {

        float Step(float x, float y) => y > x ? 1 : 0;
        Vector2 Vector2Abs(Vector2 a) => new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));

        p0 /= voxelSize;
        p1 /= voxelSize;

        Vector2 rd = p1 - p0;
        Vector2 p = new Vector2(Mathf.Floor(p0.x), Mathf.Floor(p0.y));
        Vector2 rdinv = Vector2.one / rd;
        Vector2 stp = new Vector2(Mathf.Sign(rd.x), Mathf.Sign(rd.y));
        Vector2 delta = Vector2.Min(rdinv * stp, Vector2.one);
        Vector2 t_max = Vector2Abs((p + Vector2.Max(stp, Vector2.zero) - p0) * rdinv);
        int i = 0;
        while (true)
        {
            i++;
            Vector2Int square = Vector2Int.RoundToInt(p);
            Handles.Label(new Vector3(square.x + .5f, 0, square.y + .5f) * voxelSize, "" + i);

            float next_t = Mathf.Min(t_max.x, t_max.y);
            if (next_t > 1.0) break;
            Vector2 intersection = p0 + next_t * rd;
            Gizmos.DrawSphere(new Vector3(intersection.x, 0, intersection.y) * voxelSize, .05f);

            Vector2 cmp = new Vector2(Step(t_max.x, t_max.y), Step(t_max.y, t_max.x));
            t_max += delta * cmp;
            p += stp * cmp;
        }
    }

    
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
    #endregion
}