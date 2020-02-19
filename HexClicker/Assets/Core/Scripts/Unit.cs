using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    public float speed;

    private Navigation.PathFindThreaded pathFindThread;
    public PathFinding.Path<Navigation.Node> Path { get; private set; }

    private Navigation.PathIterator pathIterator;
    public void SetPath(PathFinding.Path<Navigation.Node> path)
    {
        if (pathFindThread != null && !pathFindThread.Completed)
            pathFindThread.Abort();
        pathFindThread = null;
        Path = path;
        if (path == null)
            pathIterator = null;
        else
            pathIterator = new Navigation.PathIterator(path);
    }

    public void SetDestination(Vector3 destination)
    {
        if (pathFindThread != null && !pathFindThread.Completed)
            pathFindThread.Abort();
        pathFindThread = new Navigation.PathFindThreaded(transform.position, destination, 5000, 20000, PathFinding.StandardCostFunction, 1);
    }

    public void Update()
    {
        if (pathFindThread != null && pathFindThread.Completed)
        {
            if (pathFindThread.Result == PathFinding.Result.Success)
            {
                Path = pathFindThread.Path;
                pathIterator = new Navigation.PathIterator(Path);
            }
            else
            {
                Path = null;
                pathIterator = null;
            }

            pathFindThread = null;
        }

        if (pathIterator != null)
        {
            pathIterator.AdvanceDistance(speed * Time.deltaTime);
            transform.position = pathIterator.CurrentPosition;
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Navigation.DrawPath(Path, true, false, true);
    }
}