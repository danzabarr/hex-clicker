using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinding 
{
    public delegate float CostFunction(float distance, float cost, float crowFliesDistance, int steps, int turns);
    public static float StandardCostFunction(float distance, float cost, float crowFliesDistance, int steps, int turns) => distance + cost + crowFliesDistance;
    public static float NoAdditionalCosts(float distance, float cost, float crowFliesDistance, int steps, int turns) => distance + crowFliesDistance;
    public static float StraightPaths(float distance, float cost, float crowFliesDistance, int steps, int turns) => distance + cost + crowFliesDistance + turns;
    public enum Result
    {
        Success,
        AtDestination,
        FailureNoPath,
        FailureTooManyTries,
        FailureTooFar,
    }
    public interface INode
    {
        int PathParent { get; set; }
        int PathIndex { get; set; }
        float PathDistance { get; set; }   
        float PathCrowFliesDistance { get; set; }
        float PathCost  { get; set; }
        int PathSteps { get; set; }
        int PathTurns { get; set; }
        int PathEndDirection { get; set; }
        bool Accessible { get; }
        int NeighboursCount { get; }
        INode Neighbour(int i);
        float NeighbourDistance(int i);
        float NeighbourCost(int i);
        bool NeighbourAccessible(int i);
        float EuclideanDistance(INode node);
        float XZEuclideanDistance(INode node);
        bool Open { get; set; }
        bool Closed { get; set; }
    }
    [System.Serializable]
    public class Path<T> : IEnumerable<T> where T : INode
    {
        public List<T> Nodes { get; private set; }
        public int Count => Nodes.Count;
        public T this[int index] => Nodes[index];
        public IEnumerator<T> GetEnumerator() => Nodes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public float Distance { get; private set; }
        public float Cost { get; private set; }
        public float CrowFliesDistance { get; private set; }
        public int Steps { get; private set; }
        public int Turns { get; private set; }
        public T Start => Nodes != null && Nodes.Count > 0 ? Nodes[0] : default;
        public T End => Nodes != null && Nodes.Count > 0 ? Nodes[Nodes.Count - 1] : default;
        public float TotalCost(CostFunction function) => function(Distance, Cost, CrowFliesDistance, Steps, Turns);
        public Path(List<T> nodes, float pathDistance, float pathCrowFliesDistance, float pathCost, int pathSteps, int pathTurns)
        {
            Nodes = nodes;
            Distance = pathDistance;
            CrowFliesDistance = pathCrowFliesDistance;
            Cost = pathCost;
            Steps = pathSteps;
            Turns = pathTurns;
        }

        public Path<T> Duplicate() => new Path<T>(new List<T>(Nodes), Distance, CrowFliesDistance, Cost, Steps, Turns);
    }
    public static Result PathFind<T>(T start, T end, float maxDistance, int maxTries, CostFunction costFunction, out Path<T> path, out List<T> visited, bool cleanUpOnSuccess = true) where T : INode
    {
        path = null;
        visited = new List<T>();

        if (start == null || end == null)
            return Result.FailureNoPath;

        if (!end.Accessible)
            return Result.FailureNoPath;

        if (start.Equals(end))
            return Result.AtDestination;

        float d = start.EuclideanDistance(end);

        if (d > maxDistance)
            return Result.FailureTooFar;

        List<T> open = new List<T>();
        //List<T> closed = new List<T>();

        start.PathDistance = 0;
        start.PathCrowFliesDistance = d;

        open.Add(start);
        start.Open = true;

        visited.Add(start);
        start.PathIndex = 0;

        int tries = 0;
        while (true)
        {
            tries++;
            if (tries > maxTries)
            {
                foreach (INode p in visited)
                    ClearPathFindingData(p);

                return Result.FailureTooManyTries;
            }

            T currentNode = default;

            if (open.Count == 0)
            {
                foreach (INode p in visited)
                    ClearPathFindingData(p);

                return Result.FailureNoPath;
            }

            float currentCost = 0;

            foreach (T node in open)
            {
                float cost = costFunction(node.PathDistance, node.PathCost, node.PathCrowFliesDistance, node.PathSteps, node.PathTurns);
                if (currentNode == null || cost < currentCost)
                {
                    currentNode = node;
                    currentCost = cost;
                }
            }

            if (currentNode.Equals(end))
            {
                break;
            }

            if (currentNode.PathDistance > maxDistance)
            {
                foreach (T p in visited)
                    ClearPathFindingData(p);

                return Result.FailureTooFar;
            }

            open.Remove(currentNode);
            currentNode.Open = false;

            currentNode.Closed = true;

            for (int i = 0; i < currentNode.NeighboursCount; i++)
            {
                T neighbour = (T) currentNode.Neighbour(i);

                if (neighbour == null)
                    continue;

                if (!neighbour.Accessible)
                    continue;

                if (!currentNode.NeighbourAccessible(i))
                    continue;

                float distance = currentNode.NeighbourDistance(i);
                float nextPathDistance = currentNode.PathDistance + distance;
                float nextPathCrowFliesDistance = neighbour.EuclideanDistance(end);
                float nextPathCost = currentNode.PathCost + currentNode.NeighbourCost(i);
                int nextPathSteps = currentNode.PathSteps + 1;
                int nextPathTurns = currentNode.PathTurns + ((currentNode.PathSteps == 0 || currentNode.PathEndDirection == i) ? 0 : 1);

                float nextTotalCost = costFunction(nextPathDistance, nextPathCost, nextPathCrowFliesDistance, nextPathSteps, nextPathTurns);

                if (nextTotalCost < costFunction(neighbour.PathDistance, neighbour.PathCost, neighbour.PathCrowFliesDistance, neighbour.PathSteps, neighbour.PathTurns))
                {
                    open.Remove(neighbour);
                    neighbour.Open = false;

                    neighbour.Closed = false;
                }

                if (!neighbour.Open && !neighbour.Closed)
                {
                    neighbour.PathDistance = nextPathDistance;
                    neighbour.PathCrowFliesDistance = nextPathCrowFliesDistance;
                    neighbour.PathCost = nextPathCost;
                    neighbour.PathSteps = nextPathSteps;
                    neighbour.PathTurns = nextPathTurns;

                    neighbour.PathParent = currentNode.PathIndex;
                    neighbour.PathEndDirection = i;

                    open.Add(neighbour);
                    neighbour.Open = true;

                    if (neighbour.PathIndex == -1)
                    {
                        neighbour.PathIndex = visited.Count;
                        visited.Add(neighbour);
                    }
                }
            }
        }

        List<T> nodes = new List<T>();
        T current = end;
        while (current.PathParent != -1)
        {
            nodes.Insert(0, current);
            //nodes.Add(current);
            //this is backwards.
            current = visited[current.PathParent];
        }
        nodes.Insert(0, start);
        //nodes.Add(start);
        //so is this.

        path = new Path<T>(nodes, end.PathDistance, end.PathCrowFliesDistance, end.PathCost, end.PathSteps, end.PathTurns);

        if (cleanUpOnSuccess)
        {
            foreach (T p in visited)
                ClearPathFindingData(p);
        }

        return Result.Success;
    }
    public static void ClearPathFindingData(INode node)
    {
        node.PathParent = -1;
        node.PathIndex = -1;
        node.PathDistance = 0;
        node.PathCrowFliesDistance = 0;
        node.PathCost = 0;
        node.PathSteps = 0;
        node.PathTurns = 0;
        node.PathEndDirection = 0;
        node.Open = false;
        node.Closed = false;
    }
}
