using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private bool raycastModifier;
    [SerializeField] private float pathCreation;
    [SerializeField] private MeshRenderer maskRevealer;

    private Vector2Int nearestVertex;

    private Navigation.PathFindThreaded pathFinder;
    private PathFinding.Path<Navigation.Node> path;
    private Navigation.PathIterator pathIterator;

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
    public void SetPath(PathFinding.Path<Navigation.Node> path)
    {
        Stop();
        this.path = path;
        if (path != null)
        {
            pathIterator = new Navigation.PathIterator(path);
            Destination = path.End.Position;
        }
    }

    /// <summary>
    /// Enqueues a request to pathfind to the destination.
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        Stop();
        Destination = destination;
        pathFinder = new Navigation.PathFindThreaded(transform.position, destination, 1000, 50000, PathFinding.StandardCostFunction, raycastModifier);
        Navigation.Enqueue(pathFinder);
        status = Status.Waiting;
    }

    /// <summary>
    /// Cancels movement and deletes current path if one exists.
    /// </summary>
    public void Stop()
    {
        pathIterator = null;
        if (pathFinder != null)
            pathFinder.Cancel();
        pathFinder = null;
        status = Status.Stopped;
    }

    public void Update()
    {
        //Create a new path iterator when a path has been found
        if (pathFinder != null && pathFinder.Completed)
        {
            if (pathFinder.Result == PathFinding.Result.Success)
            {
                path = pathFinder.Path;
                pathIterator = new Navigation.PathIterator(path);
                status = Status.Pathing;
            }
            else
            {
                path = null;
                pathIterator = null;
            }
            pathFinder = null;
        }

        //Move along the path
        float maskRevealerAlpha = 0;
        if (pathIterator != null)
        {
            float movementDelta = Mathf.Abs(pathIterator.AdvanceDistance(speed * Time.deltaTime));
            transform.position = pathIterator.CurrentPosition;
            maskRevealerAlpha = pathCreation * 5 * movementDelta / speed;
        }

        //Adjust revealer appropriate to current speed
        maskRevealer.material.color = new Color(1, 1, 1, maskRevealerAlpha);

        //Apply path to navigation nodes
        Vector2Int nearestVertex = Vector2Int.RoundToInt((transform.position * HexMap.NavigationResolution / HexMap.TileSize).xz());
        if (this.nearestVertex != nearestVertex)
        {
            this.nearestVertex = nearestVertex;
            if (Navigation.TryGetNode(nearestVertex, out Navigation.Node node))
            {
                node.dynamicMovementCost = Mathf.Max(node.dynamicMovementCost - pathCreation, 0);
            }
        }

        MoveRandomly();
    }

    public void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Navigation.DrawPath(path, true, false, false);

        //Gizmos.color = Color.red;
        //if (Navigation.NearestSquareNode(transform.position, out Navigation.Node node))
        //{
        //    Gizmos.DrawSphere(node.Position, .03f);
        //}
    }

    private void MoveRandomly()
    {
        if ((pathFinder == null && pathIterator == null) || AtDestination)
        {
            float range = Random.Range(3f, 6f);

            Vector2 randomPosition = Random.insideUnitCircle * range + transform.position.xz();
            SetDestination(HexMap.Instance.OnTerrain(randomPosition));
        }
    }
}