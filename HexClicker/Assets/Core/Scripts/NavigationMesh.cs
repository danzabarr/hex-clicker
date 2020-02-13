using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class NavigationMesh
{
    public static void GenerateMesh(HexMap map, out Dictionary<Vector2Int,Node> nodes, out List<Edge> edges)
    {

        
        edges = new List<Edge>();
        nodes = new Dictionary<Vector2Int, Node>();

        int res = map.Resolution;// HexMap.Instance.Resolution;

        //Vector2Int neighbourX = new Vector2Int(Position.x - 1, Position.y + 1);
        //Vector2Int neighbourY = new Vector2Int(Position.x, Position.y - 1);
        //Vector2Int neighbourZ = new Vector2Int(Position.x - 1, Position.y);

        int i = 0;

        //Create initial node graph

        Profiler.BeginSample("Generate Nodes");

        foreach(HexTile tile in map)
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

                        Gizmos.color = Color.white;

                        if (z < -res + 1 && neighbourZ != null)
                        {
                            continue;
                        }

                        else if (x >= res && neighbourX != null)
                        {
                            continue;
                        }

                        else if (y >= res && neighbourY != null)
                        {
                            continue;
                        }

                        else
                        {
                            Vector2Int hex = new Vector2Int(x + (-tile.Position.x + tile.Position.y) * res, y + (-tile.Position.x - tile.Position.y * 2) * res);
                            Vector3 world = map.OnTerrain(tile.transform.position.x + (-x + -y / 2f) / res, tile.transform.position.z + (-y * HexUtils.SQRT_3 / 2f) / res);
                            
                            i++;

                            Node node = new Node(hex, world);

                            
                            nodes.Add(hex, node);

                            continue;
                        }
                    }
        }

        Profiler.EndSample();

        Profiler.BeginSample("Remove Unreachables");
        RemoveNodesOutsideHeightRange(nodes, edges, 0.0f, 0.3f);
        Profiler.EndSample();

        Profiler.BeginSample("Join Nodes");
        foreach(Node node in nodes.Values)
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

        

        RemoveNodesMinNeighbours(nodes, edges, 1);
        //RemoveNodesMaxNeighbours(nodes, edges, 5);
       
        //RemoveNodesColinear(nodes, edges);

        /*
        RemoveNodesColinearNeighbours(nodes, edges);
        RemoveNodesColinear(nodes, edges);
        */

        //RemoveNodesMinNeighbours(nodes, edges, 2);
        //AddConnections(map, nodes, edges, 0, .3f, .25f);

    }

    private static void AddConnections(HexMap map, Dictionary<Vector2Int, Node> nodes, List<Edge> edges, float minHeight, float maxHeight, float sampleResolution)
    {
        foreach(Node n0 in nodes.Values)
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
        while(nodes.Count > 0)
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

    private static void RemoveNodesMaxNeighbours(Dictionary<Vector2Int, Node> nodes, List<Edge> edges, int maxNeighbours)
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

    private static void RemoveNodesColinear(Dictionary<Vector2Int, Node> nodes, List<Edge> edges)
    {
        while(nodes.Count > 0)
        {
            bool changed = false;
            List<Node> toDelete = new List<Node>();

            foreach(Node node in nodes.Values)
            {
                if (node.Neighbours.Count != 2)
                    continue;
                Neighbour n0 = node.Neighbour(0);
                Neighbour n1 = node.Neighbour(1);
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

            /*

            for (int i = nodes.Count - 1; i >= 1; i--)
            {
                if (nodes[i].Neighbours.Count != 2)
                    continue;

                Neighbour n0 = nodes[i].Neighbour(0);
                Neighbour n1 = nodes[i].Neighbour(1);

                if (IsColinearXZ(n0.Node, n1.Node) && IsColinearXZ(n0.Node, nodes[i]))
                {
                    if (Connect(n0.Node, n1.Node, out Edge e, true))
                    {
                        RemoveNode(nodes, edges, nodes[i]);
                        edges.Add(e);
                        changed = true;
                    }
                }
            }

            if (!changed)
                return;

            */
        }
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

                Neighbour n0 = nodes[i].Neighbour(0);
                Neighbour n1 = nodes[i].Neighbour(1);

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
                Neighbour nn = neighbour.Node.Neighbour(n);
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
                Neighbour nn = neighbour.Node.Neighbour(n);
                if (nn.Node == node)
                {
                    neighbour.Node.Neighbours.RemoveAt(n);
                    edges.Remove(neighbour.Edge);
                    break;
                }
            }
        }

        node.flagDelete = true;
        //nodes.Remove(node.Hex);
    }

    public class Node : PathFinding.INode
    {
        public bool flagDelete;
        public Vector2Int Hex { get; private set; }
        public Vector3 Position { get; private set; }
        public List<Neighbour> Neighbours { get; private set; } = new List<Neighbour>();
        public Neighbour Neighbour(int index) => Neighbours[index];

        public Node(Vector3 position)
        {
            Position = position;
        }

        public Node(Vector2Int hex, Vector3 position)
        {
            Hex = hex;
            Position = position;
        }

        public void ClearNeighbours()
        {
            for (int i = 0; i < Neighbours.Count; i++)
            {
                Neighbour n = Neighbours[i];
                for (int j = 0; j < n.Node.Neighbours.Count; j++)
                {
                    if (n.Node.Neighbours[j].Node == this)
                    {
                        n.Node.Neighbours.RemoveAt(j);
                        break;
                    }
                }
            }
            Neighbours = new List<Neighbour>();
        }

       
        public PathFinding.INode PathParent { get; set; }
        public float PathDistance { get; set; }
        public float PathCrowFliesDistance { get; set; }
        public float PathCost { get; set; }
        public int PathSteps { get; set; }
        public int PathTurns { get; set; }
        public int PathEndDirection { get; set; }
        public bool Accessible { get; set; }
        public int NeighboursCount => Neighbours.Count;

        PathFinding.INode PathFinding.INode.Neighbour(int neighbourIndex) => Neighbour(neighbourIndex).Node;
        public float NeighbourDistance(int neighbourIndex) => Neighbour(neighbourIndex).Distance;
        public float NeighbourCost(int neighbourIndex) => 0;
        public bool NeighbourAccessible(int neighbourIndex) => true;
        public float Distance(PathFinding.INode node) => CalculateDistance(this, node as Node);
    }

    public class Edge
    {
        public Node Node1 { get; private set; }
        public Node Node2 { get; private set; }
        public float Length { get; private set; }
        public Edge(Node n1, Node n2)
        {
            Node1 = n1;
            Node2 = n2;
            Length = CalculateDistance(n1, n2);
        }
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
        public Edge Edge {get; private set;}
        public float Distance => Edge.Length;
        public Neighbour(Node node, Edge edge)
        {
            Node = node;
            Edge = edge;
        }
    }

    public static bool Connect(Node n1, Node n2, out Edge edge, bool check = true)
    {
        edge = null;

        if (n1 == null || n2 == null)
            return false;

        if (check && Connected(n1, n2, out _))
            return false;

        edge = new Edge(n1, n2);
        n1.Neighbours.Add(new Neighbour(n2, edge));
        n2.Neighbours.Add(new Neighbour(n1, edge));
        return true;
    }

    public static bool Connect(Node n1, Node n2, float precalculatedLength, out Edge edge, bool check = true)
    {
        edge = null;

        if (n1 == null || n2 == null)
            return false;

        if (check && Connected(n1, n2, out _))
            return false;

        edge = new Edge(n1, n2, precalculatedLength);
        n1.Neighbours.Add(new Neighbour(n2, edge));
        n2.Neighbours.Add(new Neighbour(n1, edge));
        return true;
    }

    public static void Disconnect(Node n1, Node n2)
    {
        for (int i = 0; i < n1.Neighbours.Count; i++)
            if (n1.Neighbours[i].Node == n2)
            {
                n1.Neighbours.RemoveAt(i);
                break;
            }

        for (int i = 0; i < n2.Neighbours.Count; i++)
            if (n2.Neighbours[i].Node == n1)
            {
                n2.Neighbours.RemoveAt(i);
                break;
            }
    }

    public static bool Connected(Node n1, Node n2, out Edge edge)
    {
        edge = default;
        foreach (Neighbour n in n1.Neighbours)
            if (n.Node == n2)
            {
                edge = n.Edge;
                return true;
            }
        return false;
    }

    public static float CalculateDistance(Node n1, Node n2) => Vector3.Distance(n1.Position, n2.Position);

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

    private static bool PathIntersectsEdge(Node start, Node end, List<Edge> edges, out Vector2 intersection, out Edge edge)
    {
        intersection = default;
        edge = default;

        Vector2 startPos = start.Position.xz();
        Vector2 endPos = end.Position.xz();

        foreach(Edge e in edges)
        {
            if (e.Node1 == start || e.Node1 == end || e.Node1 == start || e.Node1 == end)
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
}
