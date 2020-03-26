using HexClicker.Buildings;
using HexClicker.World;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HexClicker.Navigation
{
    public class Agent : MonoBehaviour
    {
        public enum Status
        {
            Failed,
            Started,
            Stopped,
            Obstructed,
            InvalidTarget,
            AtDestination,
        }

        [SerializeField] private float speed;
        [SerializeField] private bool raycastModifiedPaths;

        private PathFinding.Request pathRequest;
        private Callback callback;
        private List<PathFinding.Point> path;
        private PathIterator pathIterator;
        private Node[] firstNeighbour;
        public Building CurrentBuilding { get; private set; }
        //public Node CurrentNode { get; private set; }
        public Building DestinationBuilding { get; private set; }
        public Node DestinationNode => path?[path.Count - 1].Node;
        public bool Stopped => pathRequest == null && pathIterator == null && path == null;

        public delegate void Callback(Status status);

        public void Start()
        {
            transform.position = Map.Instance.OnTerrain(transform.position);
        }

        public void SetDestination(Vector3 position, float maxCost, float takeExistingPaths, float proximityToEnd, Callback pathCallback)
        {
            Stop();
            callback = pathCallback;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                startNode = CurrentBuilding?.Exit,
                end = position,
                addStartNeighbours = firstNeighbour,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths,
                proximityToEnd = proximityToEnd,
                callback = ProcessRequestResult
            };
            pathRequest.Queue();
        }

        public void SetDestination(Node node, float maxCost, float takeExistingPaths, float proximityToEnd, Callback pathCallback)
        {
            Stop();
            callback = pathCallback;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                startNode = CurrentBuilding?.Exit,
                endNode = node,
                addStartNeighbours = firstNeighbour,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths,
                proximityToEnd = proximityToEnd,
                callback = ProcessRequestResult
            };
            pathRequest.Queue();
        }

        public void SetDestination(Building building, float maxCost, float takeExistingPaths, float proximityToEnd, Callback callback)
        {
            Stop();
            this.callback = callback;
            DestinationBuilding = building;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                startNode = CurrentBuilding?.Exit,
                endNode = building.Enter,
                addStartNeighbours = firstNeighbour,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths,
                proximityToEnd = proximityToEnd,
                callback = ProcessRequestResult
            };
            pathRequest.Queue();
        }

        public void LookFor(PathFinding.Match match, bool allowInaccessibleEnd, float maxCost, float takeExistingPaths, float proximityToEnd, Callback callback)
        {
            Stop();
            this.callback = callback;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                startNode = CurrentBuilding?.Exit,
                match = match,
                addStartNeighbours = firstNeighbour,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths,
                proximityToEnd = proximityToEnd,
                allowInaccessibleEnd = allowInaccessibleEnd,
                callback = ProcessRequestResult
            };
            pathRequest.Queue();
        }

        public void LookFor(PathFinding.Match match, Vector3 towards, bool allowInaccessibleEnd, float maxCost, float takeExistingPaths, float proximityToEnd, Callback callback)
        {
            Stop();
            this.callback = callback;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                startNode = CurrentBuilding?.Exit,
                end = towards,
                match = match,
                addStartNeighbours = firstNeighbour,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths,
                proximityToEnd = proximityToEnd,
                allowInaccessibleEnd = allowInaccessibleEnd,
                matchTowardsEnd = true,
                callback = ProcessRequestResult
            };
            pathRequest.Queue();
        }

        public void LookForTree(float maxCost, float takeExistingPaths, float proximityToEnd, Callback callback)
        {
            LookFor((Node node) => Map.Instance.TryGetTree(node.Vertex, out Trees.Tree tree) && !tree.tagged, true, maxCost, takeExistingPaths, proximityToEnd, callback);
        }

        public void LookForTree(Vector3 towards, float maxCost, float takeExistingPaths, float proximityToEnd, Callback callback)
        {
            LookFor((Node node) => Map.Instance.TryGetTree(node.Vertex, out Trees.Tree tree) && !tree.tagged, towards, true, maxCost, takeExistingPaths, proximityToEnd, callback);
        }

        public void Stop()
        {
            if (pathIterator != null)
                firstNeighbour = new Node[] { pathIterator.NodeBehind, pathIterator.NodeInfront };
            pathIterator = null;
            pathRequest = null;
            path = null;
            DestinationBuilding = null;
            callback?.Invoke(Status.Stopped);
        }

        private void ProcessRequestResult()
        {
            if (pathRequest != null && pathRequest.Completed)
            {
                if (pathRequest.Result == PathFinding.Result.Success)
                {
                    path = pathRequest.Path;
                    pathIterator = new PathIterator(path, pathRequest.proximityToEnd);
                    firstNeighbour = null;
                    CurrentBuilding = null;
                    if (pathIterator.Last is BuildingNode)
                        DestinationBuilding = (pathIterator.Last as BuildingNode).Building;
                    if (pathIterator.Last is StorageNode)
                        DestinationBuilding = (pathIterator.Last as StorageNode).Building;
                    if (pathIterator.Last is WorkNode)
                        DestinationBuilding = (pathIterator.Last as WorkNode).Building;

                    callback?.Invoke(Status.Started);
                }
                else if (pathRequest.Result == PathFinding.Result.AtDestination)
                {
                    path = null;
                    pathIterator = null;
                    DestinationBuilding = null;
                    callback?.Invoke(Status.AtDestination);
                    callback = null;
                }
                else
                {
                    Debug.Log("Path was unsuccessful: " + pathRequest.Result);
                    path = null;
                    pathIterator = null;
                    DestinationBuilding = null;
                    callback?.Invoke(Status.Failed);
                    callback = null;
                }
                pathRequest = null;
            }
        }

        public void Update()
        {
            //Create a new path iterator when a path has been found

            if (pathIterator != null)
            {
                pathIterator.AdvanceDistance(speed * Time.deltaTime);
                Vector3 forward = (pathIterator.CurrentPosition - transform.position);
                forward.Scale(new Vector3(1, 0, 1));
                forward.Normalize();
                transform.position = pathIterator.CurrentPosition;
                if (pathIterator.AtEnd)
                {
                    if (pathIterator.Last is BuildingNode)
                        CurrentBuilding = (pathIterator.Last as BuildingNode).Building;
                    //if (pathIterator.Last is StorageNode)
                    //    CurrentBuilding = (pathIterator.Last as StorageNode).Building;
                    //if (pathIterator.Last is WorkNode)
                    //    CurrentBuilding = (pathIterator.Last as WorkNode).Building;
                    firstNeighbour = null;
                    pathIterator = null;
                    path = null;
                    callback?.Invoke(Status.AtDestination);
                    callback = null;
                }
                else
                    transform.forward = forward;
            }
            else
            {
                callback?.Invoke(Status.Failed);
                callback = null;
            }
        }

        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            PathFinding.DrawPath(path, true, false, false);

            if (pathIterator != null && pathIterator.NodeInfront != null)
                Gizmos.DrawSphere(pathIterator.NodeInfront.Position, 0.05f);

            /*
            Gizmos.color = Color.yellow;
            if (PathFinding.PathFind(transform.position, default, null, null, null, (Node node) => { return Map.Instance.TryGetTree(node.Vertex, out _); }, false, true, 500, 1, out List<PathFinding.Point> p) == PathFinding.Result.Success)
                PathFinding.DrawPath(p);
            */
            Gizmos.color = Color.blue;
            foreach (Node node in NavigationGraph.NearestSquareNodes(transform.position, true))
                Gizmos.DrawLine(transform.position, node.Position);
        }
    }
}
