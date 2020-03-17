using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace HexClicker.Navigation
{
    public static class PathFinding
    {
        public static readonly int PathFindingThreads = 16;
        public static readonly int MaxTries = 10000;

        private static Thread[] threads;
        private static BlockingCollection<Request>[] queues;
        private static Dictionary<Node, Data>[] nodeDataMaps;
        private static List<Node>[] openLists;
        private static List<Node>[] visitedLists;

        public struct Data
        {
            public int index;
            public int parent;
            public float hCost;
            public float gCost;
            public bool open;
        }

        public delegate bool Match(Node node);

        public readonly struct Point
        {
            public Point(Node node, Data data)
            {
                Node = node;
                Data = data;
            }
            public Node Node { get; }
            public Data Data { get; }
        }

        public class Request
        {
            public delegate void Callback();

            public Vector3 start, end;
            public Node startNode, endNode;
            public Node[] addStartNeighbours;
            public Match match;
            public float maxCost;
            public float takeExistingPaths;
            public bool matchTowardsEnd;
            public bool allowInaccessibleEnd;
            public Callback callback;
            public bool Queued { get; private set; }
            public bool Started { get; private set; }
            public bool Cancelled { get; private set; }
            public bool Completed { get; private set; }
            public List<Point> Path { get; private set; }
            public Result Result { get; private set; }
            public Node First => Path[0].Node;
            public Node Last => Path[Path.Count - 1].Node;
            private static void StartThreads(int amount)
            {
                threads = new Thread[amount];
                queues = new BlockingCollection<Request>[amount];
                nodeDataMaps = new Dictionary<Node, Data>[amount];
                openLists = new List<Node>[amount];
                visitedLists = new List<Node>[amount];

                for (int i = 0; i < amount; i++)
                {
                    queues[i] = new BlockingCollection<Request>();
                    nodeDataMaps[i] = new Dictionary<Node, Data>();
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

            public void Cancel()
            {
                if (!Started && !Cancelled)
                {
                    Cancelled = true;
                    callback?.Invoke();
                }
            }

            private void Execute(int thread)
            {
                Started = true;
                Result = PathFind(start, end, startNode, endNode, addStartNeighbours, match, matchTowardsEnd, allowInaccessibleEnd, maxCost, takeExistingPaths, out List<Point> path, thread);
                Path = path;
                Completed = true;
                callback?.Invoke();
            }
        }

        public enum Result
        {
            Success,
            AtDestination,
            FailureNoPath,
            FailureTooManyTries,
            FailureTooFar,
            FailureStartObstructed,
            FailureEndObstructed,
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

        public static Result PathFind(Vector3 start, Vector3 end, Node startNode, Node endNode, Node[] addStartNeighbours, Match match, bool matchTowardsEnd, bool allowInaccessibleEnd, float maxCost, float takeExistingPaths, out List<Point> path, int thread = -1)
        {
            path = default;
            Node s = startNode;
            Node e = endNode;
            List<Node> endNeighbours = default;
            List<Node> startNeighbours = default;

            if (endNode != null && !endNode.Accessible && !allowInaccessibleEnd)
                return Result.FailureEndObstructed;

            if (startNode == null)
            {
                s = new Node(start);
                startNeighbours = NavigationGraph.NearestSquareNodes(start, true);

                if (addStartNeighbours != null)
                    for (int i = 0; i < addStartNeighbours.Length; i++)
                        if (addStartNeighbours[i] != null)
                            startNeighbours.Add(addStartNeighbours[i]);

                if (startNeighbours.Count <= 0)
                    return Result.FailureStartObstructed;
            }

            if (endNode == null)
            {
                e = new Node(end);
                endNeighbours = NavigationGraph.NearestSquareNodes(end, true);
                if (endNeighbours.Count <= 0)
                    return Result.FailureEndObstructed;
            }

            if (startNode == null)
            {
                foreach (Node neighbour in startNeighbours)
                    s.Neighbours.Add(neighbour, Node.Distance(s, neighbour));
            }

            if (endNode == null)
            {
                foreach (Node neighbour in endNeighbours)
                    neighbour.Neighbours.Add(e, Node.Distance(neighbour, e));
            }

            Result result;

            if (match != null)
                result = PathToMatch(s, match, end, matchTowardsEnd, allowInaccessibleEnd, maxCost, takeExistingPaths, out path, thread);
            else
                result = PathToNode(s, e, allowInaccessibleEnd, maxCost, takeExistingPaths, out path, thread);

            if (endNode == null)
            {
                foreach (Node neighbour in endNeighbours)
                    neighbour.Neighbours.Remove(e);
            }

            if (result == Result.Success)
                RaycastModifier(path, takeExistingPaths);

            return result;
        }

        private static Result PathToNode(Node start, Node end, bool allowInaccessibleEnd, float maxCost, float takeExistingPaths, out List<Point> path, int thread = -1)
        {
            path = null;
            if (start == null || end == null)
                return Result.FailureNoPath;

            if (!allowInaccessibleEnd && !end.Accessible)
                return Result.FailureNoPath;

            if (start.Equals(end))
                return Result.AtDestination;

            Dictionary<Node, Data> nodeData;
            List<Node> visited;
            List<Node> open;

            if (thread < 0 || thread >= threads.Length)
            {
                nodeData = new Dictionary<Node, Data>();
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

            nodeData.Add(start, new Data
            {
                index = 0,
                parent = -1,
                gCost = 0,
                hCost = Node.Distance(start, end),
                open = true
            });

            open.Add(start);
            visited.Add(start);

            int tries = 0;
            while (true)
            {
                tries++;
                if (tries > MaxTries)
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
                    Node node = open[i];
                    Data data = nodeData[node];

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

                Data currentData = nodeData[currentNode];
                if (currentData.gCost > maxCost)
                {
                    Clear();
                    return Result.FailureTooFar;
                }

                open.RemoveAt(currentIndex);
                currentData.open = false;
                nodeData[currentNode] = currentData;

                foreach(Node neighbour in currentNode.Neighbours.Keys)
                {
                    if (neighbour == null)
                        continue;

                    if (!neighbour.Accessible && (!allowInaccessibleEnd || !neighbour.Equals(end)))
                        continue;

                    //if (!currentNode.NeighbourAccessible(i))
                    //    continue;

                    float tentativeGCost = currentData.gCost + currentNode.Neighbours[neighbour] * Mathf.Lerp(1, (currentNode.MovementCost + neighbour.MovementCost) / 2, takeExistingPaths);
                    float tentativeHCost = Node.Distance(neighbour, end);
                    float tentativeCost = tentativeGCost + tentativeHCost;

                    bool neighbourExists = nodeData.TryGetValue(neighbour, out Data neighbourData);

                    if (!neighbourExists || tentativeCost < neighbourData.gCost + neighbourData.hCost)
                    {
                        if (!neighbourExists)
                            neighbourData = new Data();

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
                            neighbourData.index = visited.Count;
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
            Data d = nodeData[end];
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

        private static Result PathToMatch(Node start, Match match, Vector3 end, bool matchTowardsEnd, bool allowInaccessibleEnd, float maxCost, float takeExistingPaths, out List<Point> path, int thread = -1)
        {
            path = null;
            if (start == null)
                return Result.FailureNoPath;

            if (match(start))
                return Result.AtDestination;

            Dictionary<Node, Data> nodeData;
            List<Node> visited;
            List<Node> open;


            if (thread < 0 || thread >= threads.Length)
            {
                nodeData = new Dictionary<Node, Data>();
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

            nodeData.Add(start, new Data
            {
                index = 0,
                parent = -1,
                gCost = 0,
                hCost = matchTowardsEnd ? Vector3.Distance(start.Position, end) : 0,
                open = true
            });

            open.Add(start);
            visited.Add(start);
            Node currentNode;

            int tries = 0;
            while (true)
            {
                tries++;
                if (tries > MaxTries)
                {
                    Clear();
                    return Result.FailureTooManyTries;
                }

                if (open.Count == 0)
                {
                    Clear();
                    return Result.FailureNoPath;
                }

                currentNode = null;
                float currentCost = float.MaxValue;
                int currentIndex = 0;

                for (int i = 0; i < open.Count; i++)
                {
                    Node node = open[i];
                    Data data = nodeData[node];

                    float cost = data.gCost + data.hCost;
                    if (cost < currentCost)
                    {
                        currentIndex = i;
                        currentNode = node;
                        currentCost = cost;
                    }
                }

                if (match(currentNode))
                {
                    break;
                }

                Data currentData = nodeData[currentNode];
                if (currentData.gCost > maxCost)
                {
                    Clear();
                    return Result.FailureTooFar;
                }

                open.RemoveAt(currentIndex);
                currentData.open = false;
                nodeData[currentNode] = currentData;

                foreach(Node neighbour in currentNode.Neighbours.Keys)
                {
                    if (neighbour == null)
                        continue;

                    if (!neighbour.Accessible && (!allowInaccessibleEnd || !match(neighbour)))
                        continue;

                    //if (!currentNode.NeighbourAccessible(i))
                    //    continue;

                    float tentativeGCost = currentData.gCost + currentNode.Neighbours[neighbour] * Mathf.Lerp(1, (currentNode.MovementCost + neighbour.MovementCost) / 2, takeExistingPaths);
                    float tentativeHCost = matchTowardsEnd ? Vector3.Distance(neighbour.Position, end) : 0;
                    float tentativeCost = tentativeGCost + tentativeHCost;

                    bool neighbourExists = nodeData.TryGetValue(neighbour, out Data neighbourData);

                    if (!neighbourExists || tentativeCost < neighbourData.gCost + neighbourData.hCost)
                    {
                        if (!neighbourExists)
                            neighbourData = new Data();

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
                            neighbourData.index = visited.Count;
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
            Node n = currentNode;
            Data d = nodeData[n];
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

        private static void RaycastModifier(List<Point> path, float takeExistingPaths)
        {
            if (path == null)
                return;

            if (path.Count <= 2)
                return;

            World.Map map = World.Map.Instance;

            int startIndex = 0;

            List<Vector3> points = new List<Vector3>();

            while (path.Count > startIndex)
            {
                for (int i = path.Count - 1; i > startIndex; i--)
                {
                    //float pathDistance = path[i].Data.PathDistance - path[startIndex].Data.PathDistance;
                    float pathCost = path[i].Data.gCost - path[startIndex].Data.gCost;// path[i].Data.gCost - path[startIndex].Data.gCost;

                    bool valid = true;
                    points.Clear();
                    //float shortCutDistance = 0;
                    float shortCutCost = 0;

                    Vector2 p0 = path[startIndex].Node.Position.xz();
                    Vector2 p1 = path[i].Node.Position.xz();
                    float voxelSize = World.Map.TileSize / NavigationGraph.Resolution;
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
                            float finalCost = finalDistance * Mathf.Lerp(1, node.MovementCost, takeExistingPaths);
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

                        float cost = distance * Mathf.Lerp(1, node.MovementCost, takeExistingPaths);
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
