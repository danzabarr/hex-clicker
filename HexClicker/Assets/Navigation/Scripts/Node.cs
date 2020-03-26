using HexClicker.Buildings;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    [System.Serializable]
    public class Node
    {
        public static readonly float MinDesirePathCost = .5f;
        public static readonly float MaxDesirePathCost = 2;

        public readonly Vector2Int Vertex;
        public readonly Vector3 Position;
        public readonly ConcurrentDictionary<Node, float> Neighbours = new ConcurrentDictionary<Node, float>();

        public readonly bool ZeroDistance;
        public readonly bool OffGrid;

        private float desirePathCost = MaxDesirePathCost;
        public float DesirePathCost
        {
            get => desirePathCost;
            set => desirePathCost = Mathf.Clamp(value, MinDesirePathCost, MaxDesirePathCost);
        }
        public float MovementCost => DesirePathCost;// + roads + other stuff;
        public int Obstructions { get; set; }
        public bool Accessible => Obstructions <= 0;
        //public float NeighbourCost(int i, float takePaths) => Neighbours[i].Distance * Mathf.Lerp(1, (MovementCost + Neighbours[i].Node.MovementCost) / 2, takePaths);
        //public bool NeighbourAccessible(int i) => true;

        public Node(Vector2Int vertex, Vector3 position)
        {
            Vertex = vertex;
            Position = position;
        }

        public Node(Vector3 position)
        {
            Position = position;
            OffGrid = true;
        }

        public Node(Vector3 position, bool zeroDistance)
        {
            Position = position;
            ZeroDistance = zeroDistance;
            OffGrid = true;
        }

        public void Disconnect()
        {
            foreach (Node node in Neighbours.Keys)
                node.Neighbours.TryRemove(this, out _);
//                node.Neighbours.Remove(this);

            Neighbours.Clear();
        }

        public static bool Connect(Node n1, Node n2, bool check = true)
        {
            if (n1 == null || n2 == null)
                return false;

            if (check && Connected(n1, n2))
                return false;

            float distance = Distance(n1, n2);
            n1.Neighbours.TryAdd(n2, distance);
            n2.Neighbours.TryAdd(n1, distance);
            return true;
        }

        public static bool Connected(Node n1, Node n2)
        {
            return n1.Neighbours.ContainsKey(n2);
        }

        public static float Distance(Node n1, Node n2) => (n1.ZeroDistance || n2.ZeroDistance) ? 0 : Vector3.Distance(n1.Position, n2.Position);
    }
}
