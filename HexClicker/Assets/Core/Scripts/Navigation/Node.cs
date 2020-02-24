using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class Node
    {
        public static readonly float MaxDesirePathCost = 8;
        public struct Neighbour
        {
            public Node Node { get; private set; }
            public float Distance { get; private set; }
            public Neighbour(Node node, float distance)
            {
                Node = node;
                Distance = distance;
            }
        }

        public readonly Vector2Int Index;
        public readonly Vector3 Position;
        public readonly List<Neighbour> Neighbours = new List<Neighbour>();

        private float desirePathCost = MaxDesirePathCost;
        public float DesirePathCost
        {
            get => desirePathCost;
            set => desirePathCost = Mathf.Clamp(value, 4, MaxDesirePathCost);
        }
        public float MovementCost => DesirePathCost;// + roads + other stuff;
        public bool Accessible { get; set; } = true;
        public float NeighbourCost(int i) => Neighbours[i].Distance * (MovementCost + Neighbours[i].Node.MovementCost) / 2;
        public bool NeighbourAccessible(int i) => true;

        public Node(Vector2Int hex, Vector3 position)
        {
            Index = hex;
            Position = position;
        }

        public Node(Vector3 position)
        {
            Position = position;
        }

        public void RemoveLastAddedNeighbour()
        {
            if (Neighbours.Count > 0)
                Neighbours.RemoveAt(Neighbours.Count - 1);
        }
        public static bool Connect(Node n1, Node n2, bool check = true)
        {
            if (n1 == null || n2 == null)
                return false;

            if (check && Connected(n1, n2))
                return false;

            float distance = Distance(n1, n2);
            n1.Neighbours.Add(new Node.Neighbour(n2, distance));
            n2.Neighbours.Add(new Node.Neighbour(n1, distance));
            return true;
        }
        public static bool Connected(Node n1, Node n2)
        {
            foreach (Node.Neighbour n in n1.Neighbours)
                if (n.Node == n2)
                    return true;
            return false;
        }
        public static float Distance(Node n1, Node n2) => Vector3.Distance(n1.Position, n2.Position);
    }
}