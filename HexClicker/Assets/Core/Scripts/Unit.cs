using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour
{
    private Navigation.PathFindThreaded pathFindThread;
    public PathFinding.Path<Navigation.Node> Path { get; private set; }
    public void SetPath(PathFinding.Path<Navigation.Node> path)
    {
        if (pathFindThread != null && !pathFindThread.Completed)
            pathFindThread.Abort();
        pathFindThread = null;
        Path = path;
    }

    public void SetDestination(Vector3 destination)
    {
        if (pathFindThread != null && !pathFindThread.Completed)
            pathFindThread.Abort();
        pathFindThread = new Navigation.PathFindThreaded(transform.position, destination, 5000, 20000, PathFinding.StandardCostFunction, HexMap.TileSize / HexMap.NavigationResolution * .5f);
    }

    public void Update()
    {
        if (pathFindThread != null && pathFindThread.Completed)
        {
            if (pathFindThread.Result == PathFinding.Result.Success)
                Path = pathFindThread.Path;
            else
                Path = null;

            pathFindThread = null;
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Navigation.DrawPath(Path, false);
    }
}