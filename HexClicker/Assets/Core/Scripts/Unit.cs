using HexClicker.Navigation;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

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

    public enum Status
    {
        Waiting,
        Stopped,
        Pathing
    }

    public Status status;

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
        Stop();
        Destination = destination;
        pathRequest = new PathFinding.Request(transform.position, destination, 500, 10000, takeExistingPaths, raycastModifiedPaths);
        pathRequest.Queue();
        status = Status.Waiting;
    }

    /// <summary>
    /// Cancels movement and deletes current path if one exists.
    /// </summary>
    public void Stop()
    {
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
                status = Status.Pathing;
            }
            else
            {
                path = null;
                pathIterator = null;
            }
            pathRequest = null;
        }

        if (pathIterator != null)
        {
            pathIterator.AdvanceDistance(speed * Time.deltaTime);
            transform.position = pathIterator.CurrentPosition;
        }

        //Apply path to navigation nodes
        Vector2Int nearestNode = Vector2Int.RoundToInt((transform.position * NavigationGraph.Resolution / HexMap.TileSize).xz());
        if (this.nearestNode != nearestNode)
        {
            this.nearestNode = nearestNode;
            if (NavigationGraph.TryGetNode(nearestNode, out Node node))
                node.DesirePathCost -= pathCreation;
        }

        MoveRandomly();
        //MoveMouseClick();
    }
    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        PathFinding.DrawPath(path, true, false, false);
    }
    private void MoveRandomly()
    {
        if ((pathRequest == null && pathIterator == null) || AtDestination)
        {
            float range = Random.Range(3f, 6f);

            Vector2 randomPosition = Random.insideUnitCircle * range + transform.position.xz();
            SetDestination(HexMap.Instance.OnTerrain(randomPosition));
        }
    }
    private void MoveMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (ScreenCast.MouseTerrain.Cast(out RaycastHit hitInfo))
            {
                SetDestination(hitInfo.point);
            }
        }
    }
}