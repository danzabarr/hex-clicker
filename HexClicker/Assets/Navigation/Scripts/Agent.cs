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
        [SerializeField] [Range(0, 1)] private float takeExistingPaths;

        private PathFinding.Request pathRequest;
        private List<PathFinding.Point> path;
        private PathIterator pathIterator;
        private Node[] firstNeighbour;
        public Building CurrentBuilding { get; private set; }
        public Building DestinationBuilding { get; private set; }
        public bool Stopped => pathRequest == null && pathIterator == null && path == null;
        public void SetDestination(Vector3 position)
        {
            Stop();
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                end = position,
                addStartNeighbours = firstNeighbour,
                maxCost = 1000,
                takeExistingPaths = takeExistingPaths
            };
            if (CurrentBuilding != null)
                pathRequest.startNode = CurrentBuilding.Exit;
            pathRequest.Queue();
        }

        public void SetDestination(Building building)
        {
            Stop();
            DestinationBuilding = building;
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                endNode = building.Enter,
                addStartNeighbours = firstNeighbour,
                maxCost = 1000,
                takeExistingPaths = takeExistingPaths
            };
            if (CurrentBuilding != null)
                pathRequest.startNode = CurrentBuilding.Exit;
            pathRequest.Queue();
        }

        public void LookFor(PathFinding.Match match, bool allowInaccessibleEnd)
        {
            Stop();
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                match = match,
                addStartNeighbours = firstNeighbour,
                maxCost = 1000,
                takeExistingPaths = takeExistingPaths,
                allowInaccessibleEnd = allowInaccessibleEnd
            };
            if (CurrentBuilding != null)
                pathRequest.startNode = CurrentBuilding.Exit;
            pathRequest.Queue();
        }

        public void LookFor(PathFinding.Match match, Vector3 towards, bool allowInaccessibleEnd)
        {
            Stop();
            pathRequest = new PathFinding.Request()
            {
                start = transform.position,
                end = towards,
                match = match,
                addStartNeighbours = firstNeighbour,
                maxCost = 1000,
                takeExistingPaths = takeExistingPaths,
                allowInaccessibleEnd = allowInaccessibleEnd,
                matchTowardsEnd = true
            };
            if (CurrentBuilding != null)
                pathRequest.startNode = CurrentBuilding.Exit;
            pathRequest.Queue();
        }

        public void Stop()
        {
            if (pathIterator != null)
                firstNeighbour = new Node[] { pathIterator.NodeBehind, pathIterator.NodeInfront };
            pathIterator = null;
            pathRequest = null;
            path = null;
            DestinationBuilding = null;
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
                    firstNeighbour = null;
                    CurrentBuilding = null;
                    if (pathIterator.Last is BuildingNode)
                        DestinationBuilding = (pathIterator.Last as BuildingNode).building;
                }
                else
                {
                    Debug.Log("Path was unsuccessful: " + pathRequest.Result);
                    path = null;
                    pathIterator = null;
                    DestinationBuilding = null;
                }
                pathRequest = null;
            }

            if (pathIterator != null)
            {
                pathIterator.AdvanceDistance(speed * Time.deltaTime);
                transform.position = pathIterator.CurrentPosition;
                if (pathIterator.T >= 1)
                {
                    if (pathIterator.Last is BuildingNode)
                        CurrentBuilding = (pathIterator.Last as BuildingNode).building;
                    firstNeighbour = null;
                    pathIterator = null;
                    path = null;
                }
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
