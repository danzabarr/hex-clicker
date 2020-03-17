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
        [SerializeField] [Range(0, 1)] private float takeExistingPaths;

        private PathFinding.Request pathRequest;
        private Callback callback;
        private List<PathFinding.Point> path;
        private PathIterator pathIterator;
        private Node[] firstNeighbour;
        public Building CurrentBuilding { get; private set; }
        public Building DestinationBuilding { get; private set; }
        public Node DestinationNode => path?[path.Count - 1].Node;
        public bool Stopped => pathRequest == null && pathIterator == null && path == null;

        public delegate void Callback(Status status);

        public void Start()
        {
            transform.position = Map.Instance.OnTerrain(transform.position);
        }

        public void SetDestination(Vector3 position, float maxCost, Callback pathCallback)
        {
            Stop();
            callback = pathCallback;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                end = position,
                addStartNeighbours = firstNeighbour,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths,
                callback = ProcessRequestResult
            };
            if (CurrentBuilding != null)
                pathRequest.startNode = CurrentBuilding.Exit;
            pathRequest.Queue();
        }

        public void SetDestination(Building building, float maxCost, Callback callback)
        {
            Stop();
            this.callback = callback;
            DestinationBuilding = building;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                endNode = building.Enter,
                addStartNeighbours = firstNeighbour,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths,
                callback = ProcessRequestResult
            };
            if (CurrentBuilding != null)
                pathRequest.startNode = CurrentBuilding.Exit;
            pathRequest.Queue();
        }

        public void LookFor(PathFinding.Match match, bool allowInaccessibleEnd, float maxCost, Callback callback)
        {
            Stop();
            this.callback = callback;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                match = match,
                addStartNeighbours = firstNeighbour,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths,
                allowInaccessibleEnd = allowInaccessibleEnd,
                callback = ProcessRequestResult
            };
            if (CurrentBuilding != null)
                pathRequest.startNode = CurrentBuilding.Exit;
            pathRequest.Queue();
        }

        public void LookFor(PathFinding.Match match, Vector3 towards, bool allowInaccessibleEnd, float maxCost, Callback callback)
        {
            Stop();
            this.callback = callback;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                end = towards,
                match = match,
                addStartNeighbours = firstNeighbour,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths,
                allowInaccessibleEnd = allowInaccessibleEnd,
                matchTowardsEnd = true,
                callback = ProcessRequestResult
            };
            if (CurrentBuilding != null)
                pathRequest.startNode = CurrentBuilding.Exit;
            pathRequest.Queue();
        }

        public void LookForTree(float maxCost, Callback callback)
        {
            LookFor((Node node) => Map.Instance.TryGetTree(node.Vertex, out Trees.Tree tree) && !tree.tagged, true, maxCost, callback);
        }

        public void LookForTree(Vector3 towards, float maxCost, Callback callback)
        {
            LookFor((Node node) => Map.Instance.TryGetTree(node.Vertex, out Trees.Tree tree) && !tree.tagged, towards, true, maxCost, callback);
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
                    pathIterator = new PathIterator(path);
                    firstNeighbour = null;
                    CurrentBuilding = null;
                    if (pathIterator.Last is BuildingNode)
                        DestinationBuilding = (pathIterator.Last as BuildingNode).building;
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
                transform.position = pathIterator.CurrentPosition;
                if (pathIterator.AtEnd)
                {
                    if (pathIterator.Last is BuildingNode)
                        CurrentBuilding = (pathIterator.Last as BuildingNode).building;
                    firstNeighbour = null;
                    pathIterator = null;
                    path = null;
                    callback?.Invoke(Status.AtDestination);
                    callback = null;
                }
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


            Gizmos.color = Color.yellow;
            if (PathFinding.PathFind(transform.position, default, null, null, null, (Node node) => { return Map.Instance.TryGetTree(node.Vertex, out _); }, false, true, 500, 1, out List<PathFinding.Point> p) == PathFinding.Result.Success)
                PathFinding.DrawPath(p);

            Gizmos.color = Color.red;
            foreach (Node node in NavigationGraph.NearestSquareNodes(transform.position, false))
                Gizmos.DrawLine(transform.position, node.Position);

            Gizmos.color = Color.blue;
            foreach (Node node in NavigationGraph.NearestSquareNodes(transform.position, true))
                Gizmos.DrawLine(transform.position, node.Position);
        }
    }
}
