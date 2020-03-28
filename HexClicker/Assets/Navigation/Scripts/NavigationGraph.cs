using HexClicker.World;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public static class NavigationGraph
    {
        public static readonly int Resolution = 64;
        public static readonly float MinHeight = 0.0f;
        public static readonly float MaxHeight = 1.25f;
        public static float NodesPerUpdate { get; private set; }

        private static Dictionary<Vector2Int, Node> nodes;
        public static Dictionary<Vector2Int, Node>.ValueCollection Nodes => nodes.Values;
        public static bool TryGetNode(Vector2Int vertex, out Node node) => nodes.TryGetValue(vertex, out node);
        public static void Generate(Map map)
        {
            int res = Resolution;
            float size = Map.TileSize;

            nodes = new Dictionary<Vector2Int, Node>();

            int w1 = Mathf.FloorToInt((map.Width - 1) / 2f);
            int w2 = Mathf.CeilToInt((map.Width - 1) / 2f);
            int h1 = Mathf.FloorToInt((map.Height - 1) / 2f);
            int h2 = Mathf.CeilToInt((map.Height - 1) / 2f);

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
                if (nodes.TryGetValue(node.Vertex + new Vector2Int(-1, -1), out Node n1))
                    Node.Connect(node, n1, false);

                if (nodes.TryGetValue(node.Vertex + new Vector2Int(0, -1), out Node n2))
                    Node.Connect(node, n2, false);

                if (nodes.TryGetValue(node.Vertex + new Vector2Int(1, -1), out Node n3))
                    Node.Connect(node, n3, false);

                if (nodes.TryGetValue(node.Vertex + new Vector2Int(1, 0), out Node n4))
                    Node.Connect(node, n4, false);
            }

            NodesPerUpdate = nodes.Count / Map.GrassRegrowthInterval / 60f;
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

        public static void Clear()
        {
            nodes = null;
        }

        public static Vector2Int NodePos(Vector3 worldPos)
        {
            worldPos *= Resolution / World.Map.TileSize;
            return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.z));
        }

        public static bool NearestSquareNode(Vector3 position, out Node node)
        {
            node = default;
            if (nodes == null)
                return false;

            return nodes.TryGetValue(NodePos(position), out node);
        }

        public static List<Node> NearestSquareNodes(Vector3 position, bool onlyAccessibleNodes)
        {
            List<Node> nearest = new List<Node>();

            if (nodes == null)
                return nearest;

            position *= Resolution / World.Map.TileSize;

            int floorX = Mathf.FloorToInt(position.x);
            int ceilX = Mathf.CeilToInt(position.x);
            if (ceilX == floorX) ceilX++;

            int floorZ = Mathf.FloorToInt(position.z);
            int ceilZ = Mathf.CeilToInt(position.z);
            if (ceilZ == floorZ) ceilZ++;

            if (nodes.TryGetValue(new Vector2Int(floorX, floorZ), out Node n0) && (!onlyAccessibleNodes || n0.Accessible))
                nearest.Add(n0);

            if (nodes.TryGetValue(new Vector2Int(floorX, ceilZ), out Node n1) && (!onlyAccessibleNodes || n1.Accessible))
                nearest.Add(n1);

            if (nodes.TryGetValue(new Vector2Int(ceilX, floorZ), out Node n2) && (!onlyAccessibleNodes || n2.Accessible))
                nearest.Add(n2);

            if (nodes.TryGetValue(new Vector2Int(ceilX, ceilZ), out Node n3) && (!onlyAccessibleNodes || n3.Accessible))
                nearest.Add(n3);

            return nearest;
        }

        public static Node NearestXYZ(Vector3 position, bool accessibleOnly)
        {
            float shortestDistance = float.MaxValue;
            Node nearest = null;

            foreach(Node node in nodes.Values)
            {
                if (accessibleOnly && !node.Accessible)
                    continue;

                float sqDist = (node.Position - position).sqrMagnitude;
                if (sqDist < shortestDistance)
                    nearest = node;
            }
            return nearest;
        }

        public static Node NearestXZ(Vector3 position, bool accessibleOnly)
        {
            if (NearestSquareNode(position, out Node nearest) && (!accessibleOnly || nearest.Accessible))
                return nearest;

            float shortestDistance = float.MaxValue;
            foreach (Node node in nodes.Values)
            {
                if (accessibleOnly && !node.Accessible)
                    continue;

                float sqDist = (node.Position.xz() - position.xz()).sqrMagnitude;
                if (sqDist < shortestDistance)
                    nearest = node;
            }
            return nearest;
        }

        public static void GrassRegrowth(float amount)
        {
            foreach (Node node in nodes.Values)
                node.DesirePathCost += amount;
        }

        public static void OnDrawGizmos()
        {
            if (nodes != null)
            {
                Vector3 size = Vector3.one * .25f * World.Map.TileSize / Resolution;
                foreach (Node node in nodes.Values)
                {
                    if (!node.Accessible)
                        Gizmos.color = Color.red;
                    else
                        Gizmos.color = Color.Lerp(Color.green, Color.white, Mathf.Clamp(node.MovementCost / Node.MaxDesirePathCost, 0, 1));
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
    }
    #region Unused
    /*
    
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
    */

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
