using HexClicker.Buildings;
using HexClicker.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Units
{
    public class Unit : MonoBehaviour
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
        public bool HasDestination => pathIterator != null;
        public bool AtDestination => pathIterator != null && pathIterator.T >= 1;

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
        public void SetDestination(Vector3 destination)
        {
            if (CurrentBuilding != null)
            {
                LeaveBuilding(CurrentBuilding, destination);
                return;
            }

            Stop();
            Destination = destination;
            DestinationBuilding = null;
            if (firstNeighbour != null)
                pathRequest = new PathFinding.Request(transform.position, firstNeighbour, destination, 500, 10000, takeExistingPaths, raycastModifiedPaths);
            else
                pathRequest = new PathFinding.Request(transform.position, destination, 500, 10000, takeExistingPaths, raycastModifiedPaths);
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void SetDestination(Building destination)
        {
            if (CurrentBuilding != null)
            {
                LeaveBuilding(CurrentBuilding, destination);
                return;
            }
            Stop();
            Destination = destination.Enter.Position;
            DestinationBuilding = destination;
            if (firstNeighbour != null)
                pathRequest = new PathFinding.Request(transform.position, firstNeighbour, destination.Enter, 500, 10000, takeExistingPaths, raycastModifiedPaths);
            else
                pathRequest = new PathFinding.Request(transform.position, destination.Enter, 500, 10000, takeExistingPaths, raycastModifiedPaths);
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void LeaveBuilding(Building current, Vector3 destination)
        {
            CurrentBuilding = current;
            DestinationBuilding = null;
            Destination = destination;
            pathRequest = new PathFinding.Request(current.Exit, destination, 500, 10000, takeExistingPaths, raycastModifiedPaths);
            pathRequest.Queue();
            status = Status.Waiting;
        }

        public void LeaveBuilding(Building current, Building destination)
        {
            if (destination == current)
                return;
            CurrentBuilding = current;
            Destination = destination.Enter.Position;
            DestinationBuilding = destination;
            pathRequest = new PathFinding.Request(current.Exit, destination.Enter, 500, 10000, takeExistingPaths, raycastModifiedPaths);
            pathRequest.Queue();
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
                    Debug.Log("Path was unsuccessful");
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
                    transform.position = Destination;
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

            //MoveRandomly();
            MoveMouseClick();
        }
        public void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            PathFinding.DrawPath(path, true, false, false);
            Gizmos.color = Color.red;
            if (pathIterator != null && pathIterator.NodeInfront != null)
                Gizmos.DrawSphere(pathIterator.NodeInfront.Position, 0.05f);
        }
        private void MoveRandomly()
        {
            if ((pathRequest == null && pathIterator == null) || AtDestination)
            {
                float range = Random.Range(3f, 20f);

                Vector2 randomPosition = Random.insideUnitCircle * range + transform.position.xz();
                SetDestination(World.Map.Instance.OnTerrain(randomPosition));
            }
        }
        private void MoveMouseClick()
        {
            if (Input.GetMouseButtonDown(1))
            {
                if (ScreenCast.MouseScene.Cast(out RaycastHit hitInfo))
                {

                    BuildingPart bp = hitInfo.collider.GetComponent<BuildingPart>();

                    if (bp != null)
                    {
                        SetDestination(bp.Parent);
                    }
                    else
                    {
                        SetDestination(hitInfo.point);
                    }
                }
            }
        }
    }
}
