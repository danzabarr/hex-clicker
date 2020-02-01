using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinding 
{
    public delegate float CostFunction(float distance, float cost, float crowFliesDistance, int steps, int turns);
    public static float StandardCostFunction(float distance, float cost, float crowFliesDistance, int steps, int turns) => distance + cost + crowFliesDistance + turns;
    public static float NoAdditionalsCostFunction(float distance, float cost, float crowFliesDistance, int steps, int turns) => distance + crowFliesDistance;

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
        INode PathParent { get; set; }
        float PathDistance { get; set; }   
        float PathCrowFliesDistance { get; set; }
        float PathCost  { get; set; }
        int PathSteps { get; set; }
        int PathTurns { get; set; }
        int PathEndDirection { get; set; }
        bool Accessible { get; }
        int NeighboursCount { get; }
        INode Neighbour(int neighbourIndex);
        float NeighbourDistance(int neighbourIndex);
        float NeighbourCost(int neighbourIndex);
        bool NeighbourAccessible(int neighbourIndex);
        float Distance(INode node);
    }

    [System.Serializable]
    public class Path<T> where T : INode
    {
        public bool FoundPath { get; private set; }
        public List<T> Nodes { get; private set; }
        public T this[int index] => Nodes[index];
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
            FoundPath = true;
            Nodes = nodes;
            Distance = pathDistance;
            CrowFliesDistance = pathCrowFliesDistance;
            Cost = pathCost;
            Steps = pathSteps;
            Turns = pathTurns;
        }
    }

    public static Result PathFind<T>(T start, T end, float maxDistance, int maxTries, CostFunction costFunction, out Path<T> path) where T : INode
    {
        path = null;

        if (start == null || end == null)
            return Result.FailureNoPath;

        if (!end.Accessible)
            return Result.FailureNoPath;

        if (start.Equals(end))
            return Result.AtDestination;

        float d = start.Distance(end);

        if (d > maxDistance)
            return Result.FailureTooFar;

        List<T> visited = new List<T>();
        List<T> open = new List<T>();
        List<T> closed = new List<T>();

        start.PathDistance = 0;
        start.PathCrowFliesDistance = d;

        open.Add(start);
        visited.Add(start);

        int tries = 0;
        while (true)
        {
            //Debug.Log("Try #" + tries);
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
                if (currentNode == null)
                {
                    currentNode = node;
                    currentCost = costFunction(currentNode.PathDistance, currentNode.PathCost, currentNode.PathCrowFliesDistance, currentNode.PathSteps, currentNode.PathTurns);
                }
                else
                {
                    float nodeCost = costFunction(node.PathDistance, node.PathCost, node.PathCrowFliesDistance, node.PathSteps, node.PathTurns);
                    if (nodeCost < currentCost)
                    {
                        currentCost = nodeCost;
                        currentNode = node;
                    }
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
            closed.Add(currentNode);


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
                float nextPathCrowFliesDistance = neighbour.Distance(end);
                float nextPathCost = currentNode.PathCost + currentNode.NeighbourCost(i);
                int nextPathSteps = currentNode.PathSteps + 1;
                int nextPathTurns = currentNode.PathTurns + ((currentNode.PathSteps == 0 || currentNode.PathEndDirection == i) ? 0 : 1);

                float nextTotalCost = costFunction(nextPathDistance, nextPathCost, nextPathCrowFliesDistance, nextPathSteps, nextPathTurns);

                if (nextTotalCost < costFunction(neighbour.PathDistance, neighbour.PathCost, neighbour.PathCrowFliesDistance, neighbour.PathSteps, neighbour.PathTurns))
                {
                    open.Remove(neighbour);
                    closed.Remove(neighbour);
                }

                if (!open.Contains(neighbour) && !closed.Contains(neighbour))
                {
                    neighbour.PathDistance = nextPathDistance;
                    neighbour.PathCrowFliesDistance = nextPathCrowFliesDistance;
                    neighbour.PathCost = nextPathCost;
                    neighbour.PathSteps = nextPathSteps;
                    neighbour.PathTurns = nextPathTurns;

                    neighbour.PathParent = currentNode;
                    neighbour.PathEndDirection = i;

                    open.Add(neighbour);
                    if (!visited.Contains(neighbour))
                        visited.Add(neighbour);
                }
            }
        }

        List<T> nodes = new List<T>();
        T current = end;
        while (current.PathParent != null)
        {
            nodes.Insert(0, current);
            //nodes.Add(current);
            //this is backwards.
            current = (T)current.PathParent;
        }
        nodes.Insert(0, start);
        //nodes.Add(start);
        //so is this.

        path = new Path<T>(nodes, end.PathDistance, end.PathCrowFliesDistance, end.PathCost, end.PathSteps, end.PathTurns);

        foreach (T p in visited)
            ClearPathFindingData(p);

        return Result.Success;
    }

    private static void ClearPathFindingData(INode node)
    {
        node.PathParent = null;
        node.PathDistance = 0;
        node.PathCrowFliesDistance = 0;
        node.PathCost = 0;
        node.PathSteps = 0;
        node.PathTurns = 0;
        node.PathEndDirection = 0;
    }
}
