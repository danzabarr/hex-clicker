using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace HexClicker.Navigation
{
    public static class PathFinding
    {
        public static readonly int PathFindingThreads = 16;

        private static Thread[] threads;
        private static BlockingCollection<Request>[] queues;
        private static Dictionary<Node, NodeData>[] nodeDataMaps;
        private static List<Node>[] openLists;
        private static List<Node>[] visitedLists;

        public struct NodeData
        {
            public int index;
            public int parent;
            public float hCost;
            public float gCost;
            public bool open;
        }
        public readonly struct Point
        {
            public Point(Node node, NodeData data)
            {
                Node = node;
                Data = data;
            }
            public Node Node { get; }
            public NodeData Data { get; }
        }
        public class Request
        {
            public readonly Vector3 start, end;
            public readonly float maxCost;
            public readonly int maxTries;
            public readonly bool raycastModifier;
            public bool Queued { get; private set; }
            public bool Started { get; private set; }
            public bool Cancelled { get; private set; }
            public bool Completed { get; private set; }
            public List<Point> Path { get; private set; }
            public Result Result { get; private set; }
            public Request(Vector3 start, Vector3 end, float maxCost, int maxTries, bool raycastModifier)
            {
                this.start = start;
                this.end = end;
                this.maxCost = maxCost;
                this.maxTries = maxTries;
                this.raycastModifier = raycastModifier;
            }
            private static void StartThreads(int amount)
            {
                threads = new Thread[amount];
                queues = new BlockingCollection<Request>[amount];
                nodeDataMaps = new Dictionary<Node, NodeData>[amount];
                openLists = new List<Node>[amount];
                visitedLists = new List<Node>[amount];

                for (int i = 0; i < amount; i++)
                {
                    queues[i] = new BlockingCollection<Request>();
                    nodeDataMaps[i] = new Dictionary<Node, NodeData>();
                    openLists[i] = new List<Node>();
                    visitedLists[i] = new List<Node>();
                    threads[i] = new Thread(ProcessQueue);
                    threads[i].Start(i);
                }
            }
            private static void ProcessQueue(object queue)
            {
                int i = (int)queue;
                while (true)
                {
                    foreach (Request request in queues[i].GetConsumingEnumerable())
                    {
                        if (request.Cancelled)
                            continue;
                        request.Execute(i);
                    }
                }
            }
            public void Queue()
            {
                if (Queued)
                    return;

                if (queues == null)
                    StartThreads(PathFindingThreads);

                int Shortest()
                {
                    int shortestLength = int.MaxValue;
                    int shortestIndex = 0;
                    for (int i = 0; i < threads.Length; i++)
                    {
                        int length = queues[i].Count;
                        if (length == 0)
                            return i;
                        if (length < shortestLength)
                        {
                            shortestLength = length;
                            shortestIndex = i;
                        }
                    }
                    return shortestIndex;
                }
                queues[Shortest()].Add(this);
                Queued = true;
            }
            public void Cancel() { if (!Started) Cancelled = true; }
            private void Execute(int thread)
            {
                Started = true;
                Result = PathFind(start, end, maxCost, maxTries, out List<Point> path, thread);
                Path = path;

                if (raycastModifier)
                    RaycastModifier(path);
                Completed = true;
            }
        }
        public enum Result
        {
            Success,
            AtDestination,
            FailureNoPath,
            FailureTooManyTries,
            FailureTooFar,
        }

        public static void DrawPath(List<Point> path, bool drawSpheres = false, bool labelNodes = false, bool labelEdges = false)
        {
            if (path == null)
                return;
#if UNITY_EDITOR

            for (int i = 0; i < path.Count - 1; i++)
                Gizmos.DrawLine(path[i].Node.Position, path[i + 1].Node.Position);

            if (labelEdges)
                for (int i = 0; i < path.Count - 1; i++)
                {
                    Vector3 midPoint = (path[i].Node.Position + path[i + 1].Node.Position) / 2f;
                    float length = Node.Distance(path[i].Node, path[i + 1].Node);
                    Handles.Label(midPoint, length + "");
                }
            if (drawSpheres)
                foreach (Point pp in path)
                    Gizmos.DrawSphere(pp.Node.Position, 0.02f);

            if (labelNodes)
                foreach (Point pp in path)
                    Handles.Label(pp.Node.Position, pp.Node.Position + "");
#endif
        }

        public static Result PathFind(Vector3 start, Vector3 end, float maxDistance, int maxTries, bool raycastModifier, out List<Point> path)
        {
            Result result = PathFind(start, end, maxDistance, maxTries, out path, -1);
            if (result == Result.Success && raycastModifier)
                RaycastModifier(path);
            return result;
        }
        public static Result PathFind(Node start, Node end, float maxDistance, int maxTries, bool raycastModifier, out List<Point> path)
        {
            Result result = PathFind(start, end, maxDistance, maxTries, out path, -1);
            if (result == Result.Success && raycastModifier)
                RaycastModifier(path);
            return result;
        }
        private static Result PathFind(Vector3 start, Vector3 end, float maxDistance, int maxTries, out List<Point> path, int thread = -1)
        {
            path = null;

            Node startNode = new Node(start);
            Node endNode = new Node(end);

            List<Node> startNeighbours = NavigationGraph.NearestSquareNodes(start);
            if (startNeighbours.Count <= 0)
                return Result.FailureNoPath;

            List<Node> endNeighbours = NavigationGraph.NearestSquareNodes(end);
            if (endNeighbours.Count <= 0)
                return Result.FailureNoPath;

            foreach (Node neighbour in startNeighbours)
                startNode.Neighbours.Add(new Node.Neighbour(neighbour, Node.Distance(startNode, neighbour)));

            foreach (Node neighbour in endNeighbours)
                Node.Connect(neighbour, endNode, false);

            Result result = PathFind(startNode, endNode, maxDistance, maxTries, out path, thread);

            foreach (Node neighbour in endNeighbours)
                neighbour.RemoveLastAddedNeighbour();

            return result;
        }
        private static Result PathFind(Node start, Node end, float maxDistance, int maxTries, out List<Point> path, int thread = -1)
        {
            path = null;

            if (start == null || end == null)
                return Result.FailureNoPath;

            if (!end.Accessible)
                return Result.FailureNoPath;

            if (start.Equals(end))
                return Result.AtDestination;

            float startToEndDistance = Node.Distance(start, end);

            if (startToEndDistance > maxDistance)
                return Result.FailureTooFar;

            Dictionary<Node, NodeData> nodeData;
            List<Node> visited;
            List<Node> open;

            if (thread < 0 || thread >= threads.Length)
            {
                nodeData = new Dictionary<Node, NodeData>();
                visited = new List<Node>();
                open = new List<Node>();
            }
            else
            {
                nodeData = nodeDataMaps[thread];
                visited = visitedLists[thread];
                open = openLists[thread];
            }

            void Clear()
            {
                nodeData.Clear();
                visited.Clear();
                open.Clear();
            }

            nodeData.Add(start, new NodeData
            {
                index = 0,
                parent = -1,
                gCost = 0,
                hCost = startToEndDistance,
                open = true
            });

            open.Add(start);
            visited.Add(start);

            int tries = 0;
            while (true)
            {
                tries++;
                if (tries > maxTries)
                {
                    Clear();
                    return Result.FailureTooManyTries;
                }


                if (open.Count == 0)
                {
                    Clear();
                    return Result.FailureNoPath;
                }

                Node currentNode = null;
                float currentCost = float.MaxValue;
                int currentIndex = 0;

                for (int i = 0; i < open.Count; i++)
                {
                    Node node = open[currentIndex];
                    NodeData data = nodeData[node];

                    float cost = data.gCost + data.hCost;
                    if (cost < currentCost)
                    {
                        currentIndex = i;
                        currentNode = node;
                        currentCost = cost;
                    }
                }

                if (currentNode.Equals(end))
                {
                    break;
                }

                NodeData currentData = nodeData[currentNode];
                if (currentData.gCost > maxDistance)
                {
                    Clear();
                    return Result.FailureTooFar;
                }

                open.RemoveAt(currentIndex);
                currentData.open = false;
                nodeData[currentNode] = currentData;

                for (int i = 0; i < currentNode.Neighbours.Count; i++)
                {
                    Node neighbour = currentNode.Neighbours[i].Node;

                    if (neighbour == null)
                        continue;

                    if (!neighbour.Accessible)
                        continue;

                    if (!currentNode.NeighbourAccessible(i))
                        continue;

                    float tentativeHCost = Node.Distance(neighbour, end);
                    float tentativeGCost = currentData.gCost + currentNode.NeighbourCost(i);
                    float tentativeCost = tentativeGCost + tentativeHCost;

                    bool neighbourExists = nodeData.TryGetValue(neighbour, out NodeData neighbourData);

                    if (!neighbourExists || tentativeCost < neighbourData.gCost + neighbourData.hCost)
                    {
                        if (!neighbourExists)
                            neighbourData = new NodeData();

                        neighbourData.parent = currentData.index;
                        neighbourData.gCost = tentativeGCost;
                        neighbourData.hCost = tentativeHCost;

                        if (!neighbourData.open)
                        {
                            neighbourData.open = true;
                            open.Add(neighbour);
                        }

                        if (neighbourData.index == -1 || !neighbourExists)
                        {
                            neighbourData.index = (ushort)visited.Count;
                            visited.Add(neighbour);
                        }
                        if (!neighbourExists)
                            nodeData.Add(neighbour, neighbourData);
                        else
                            nodeData[neighbour] = neighbourData;
                    }
                }
            }

            path = new List<Point>();
            Node n = end;
            NodeData d = nodeData[end];
            while (d.parent != -1)
            {
                path.Insert(0, new Point(n, d));
                n = visited[d.parent];
                d = nodeData[n];
            }
            path.Insert(0, new Point(start, nodeData[start]));

            Clear();
            return Result.Success;
        }
        private static void RaycastModifier(List<Point> path)
        {
            if (path == null)
                return;

            if (path.Count <= 2)
                return;

            HexMap map = HexMap.Instance;

            int startIndex = 0;

            List<Vector3> points = new List<Vector3>();

            while (path.Count > startIndex)
            {
                for (int i = path.Count - 1; i > startIndex; i--)
                {
                    //float pathDistance = path[i].Data.PathDistance - path[startIndex].Data.PathDistance;
                    float pathCost = path[i].Data.gCost - path[startIndex].Data.gCost;

                    bool valid = true;
                    points.Clear();
                    //float shortCutDistance = 0;
                    float shortCutCost = 0;

                    Vector2 p0 = path[startIndex].Node.Position.xz();
                    Vector2 p1 = path[i].Node.Position.xz();
                    float voxelSize = HexMap.TileSize / NavigationGraph.Resolution;
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

                    Vector3 lastIntersection = path[startIndex].Node.Position;

                    int steps = 0;
                    while (steps < 1000)
                    {
                        steps++;
                        Vector2Int square = Vector2Int.RoundToInt(p) + Vector2Int.one;

                        if (!NavigationGraph.TryGetNode(square, out Node node))
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
                            float finalDistance = Vector3.Distance(lastIntersection, path[i].Node.Position);
                            //shortCutDistance += finalDistance;
                            float finalCost = finalDistance * node.MovementCost;
                            shortCutCost += finalCost;

                            //Handles.Label(Vector3.Lerp(lastIntersection, path[i].Position, .5f), node.Hex + " " + finalDistance + " " + finalCost);

                            if (shortCutCost > pathCost)
                            {
                                valid = false;
                                break;
                            }
                            break;
                        }

                        Vector3 intersection = map.OnTerrain((p0 + next_t * rd + voxelOffset) * voxelSize);
                        float distance = Vector3.Distance(lastIntersection, intersection);
                        //shortCutDistance += distance;

                        float cost = distance * node.MovementCost;
                        shortCutCost += cost;

                        //Handles.Label(Vector3.Lerp(lastIntersection, intersection, .5f), node.Hex + " " + distance + " " + cost);

                        if (shortCutCost > pathCost)
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
                        path.RemoveRange(startIndex + 1, i - startIndex - 1);
                        foreach (Vector3 point in points)
                        {
                            startIndex++;
                            path.Insert(startIndex, new Point(new Node(point), default));
                        }
                        break;
                    }
                }
                startIndex++;
            }
        }
    }
}
