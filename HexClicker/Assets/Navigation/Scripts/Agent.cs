using HexClicker.Buildings;
using HexClicker.World;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class Agent : MonoBehaviour
    {
        [SerializeField] private float speed;
        [SerializeField] private bool raycastModifiedPaths;
        [SerializeField] private float pathCreation;
        [SerializeField] [Range(0, 1)] private float takeExistingPaths;

        private Vector2Int nearestNode;

        private PathFinding.Request pathRequest;
        private List<PathFinding.Point> path;
        private PathIterator pathIterator;
        private Node[] firstNeighbour;

        public enum Status
        {
            Waiting,
            Stopped,
            Pathing
        }

        public Status status;
        public Building CurrentBuilding { get; private set; }
        public Building DestinationBuilding { get; private set; }
        public Vector3 Destination { get; private set; }
        public bool HasDestination => pathRequest != null || pathIterator != null;
        public bool HasPath => pathIterator != null;
        public bool AtDestination => pathIterator != null && pathIterator.T >= 1;
        public bool Stopped => !HasDestination || AtDestination;

        /// <summary>
        /// Sets the path for this unit directly.
        /// </summary>
        public void SetPath(List<PathFinding.Point> path)
        {
            Stop();
            this.path = path;
            if (path != null)
            {
                pathIterator = new PathIterator(path);
                Destination = path[path.Count - 1].Node.Position;
            }
        }

        /// <summary>
        /// Enqueues a request to pathfind to the destination.
        /// </summary>
        public void SetDestination(Vector3 destination, float maxCost)
        {
            if (CurrentBuilding != null)
            {
                LeaveBuilding(CurrentBuilding, destination, maxCost);
                return;
            }

            Stop();
            Destination = destination;
            DestinationBuilding = null;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                addStartNeighbours = firstNeighbour,
                end = destination,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths
            };
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void SetDestination(Building destination, float maxCost)
        {
            if (CurrentBuilding != null)
            {
                LeaveBuilding(CurrentBuilding, destination, maxCost);
                return;
            }
            Stop();
            Destination = destination.Enter.Position;
            DestinationBuilding = destination;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                addStartNeighbours = firstNeighbour,
                endNode = destination.Enter,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths
            };
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void LookFor(PathFinding.Match match, bool allowInaccessibleEnd, float maxCost)
        {
            if (CurrentBuilding != null)
            {
                LeaveBuilding(CurrentBuilding, match, allowInaccessibleEnd, maxCost);
                return;
            }
            Stop();
            Destination = default;
            DestinationBuilding = null;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                addStartNeighbours = firstNeighbour,
                match = match,
                allowInaccessibleEnd = allowInaccessibleEnd,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths
            };
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void LookFor(PathFinding.Match match, Vector3 targetArea, bool allowInaccessibleEnd, float maxCost)
        {
            if (CurrentBuilding != null)
            {
                LeaveBuilding(CurrentBuilding, match, allowInaccessibleEnd, maxCost);
                return;
            }
            Stop();
            Destination = default;
            DestinationBuilding = null;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                end = targetArea,
                addStartNeighbours = firstNeighbour,
                match = match,
                matchTowardsEnd = true,
                allowInaccessibleEnd = allowInaccessibleEnd,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths
            };
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void LeaveBuilding(Building current, Vector3 destination, float maxCost)
        {
            CurrentBuilding = current;
            DestinationBuilding = null;
            Destination = destination;
            pathRequest = new PathFinding.Request()
            {
                startNode = current.Exit,
                end = destination,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths
            };
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void LeaveBuilding(Building current, Building destination, float maxCost)
        {
            if (destination == current)
                return;
            CurrentBuilding = current;
            Destination = destination.Enter.Position;
            DestinationBuilding = destination;
            pathRequest = new PathFinding.Request()
            {
                startNode = current.Exit,
                endNode = destination.Enter,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths
            };
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void LeaveBuilding(Building current, PathFinding.Match match, bool allowInaccessibleEnd, float maxCost)
        {
            CurrentBuilding = current;
            Destination = default;
            DestinationBuilding = null;
            pathRequest = new PathFinding.Request()
            {
                startNode = current.Exit,
                match = match,
                allowInaccessibleEnd = allowInaccessibleEnd,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths
            };
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void LeaveBuilding(Building current, PathFinding.Match match, Vector3 targetArea, bool allowInaccessibleEnd, float maxCost)
        {
            CurrentBuilding = current;
            Destination = default;
            DestinationBuilding = null;
            pathRequest = new PathFinding.Request()
            {
                startNode = current.Exit,
                end = targetArea,
                matchTowardsEnd = true,
                match = match,
                allowInaccessibleEnd = allowInaccessibleEnd,
                maxCost = maxCost,
                takeExistingPaths = takeExistingPaths
            };
            status = Status.Waiting;
        }

        /// <summary>
        /// Cancels movement and deletes current path if one exists.
        /// </summary>
        public void Stop()
        {
            if (pathIterator != null)
                firstNeighbour = new Node[] { pathIterator.NodeBehind, pathIterator.NodeInfront };
            pathIterator = null;
            pathRequest = null;
            status = Status.Stopped;
        }

        public void Update()
        {
            //Create a new path iterator when a path has been found
            if (pathRequest != null && pathRequest.Completed)
            {
                if (pathRequest.Result == PathFinding.Result.Success)
                {
                    
                    path = pathRequest.Path;
                    pathIterator = new PathIterator(path);
                    CurrentBuilding = null;
                    status = Status.Pathing;
                }
                else
                {
                    Debug.Log("Path was unsuccessful: " + pathRequest.Result);
                    path = null;
                    pathIterator = null;
                    firstNeighbour = null;
                }
                pathRequest = null;
            }

            if (pathIterator != null)
            {
                pathIterator.AdvanceDistance(speed * Time.deltaTime);
                transform.position = pathIterator.CurrentPosition;
                if (pathIterator.T >= 1)
                {
                    pathIterator = null;
                    path = null;
                    CurrentBuilding = DestinationBuilding;
                    firstNeighbour = null;
                    //transform.position = Destination;
                }
            }

            //Apply path to navigation nodes
            Vector2Int nearestNode = Vector2Int.RoundToInt((transform.position * NavigationGraph.Resolution / World.Map.TileSize).xz());
            if (this.nearestNode != nearestNode)
            {
                this.nearestNode = nearestNode;
                if (NavigationGraph.TryGetNode(nearestNode, out Node node))
                    node.DesirePathCost -= pathCreation;
            }
        }
        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            PathFinding.DrawPath(path, true, false, false);


            if (pathIterator != null && pathIterator.NodeInfront != null)
                Gizmos.DrawSphere(pathIterator.NodeInfront.Position, 0.05f);


            Gizmos.color = Color.yellow;
            if (PathFinding.PathFind(transform.position, default, null, null, null, (Node node) => { return Map.Instance.TryGetTree(node.Index, out _); }, false, true, 500, 1, out List<PathFinding.Point> p) == PathFinding.Result.Success)
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
