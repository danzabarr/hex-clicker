using HexClicker.Buildings;
using HexClicker.World;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class AccessPoint : MonoBehaviour
    {
        public bool showHandles;

        [SerializeField] public bool isEntrance;
        [SerializeField] public bool isExit;
        [SerializeField] public Vector3 inside;
        [SerializeField] public Vector3 outside;
        public Building Building { get; private set; }
        public Node InsideNode { get; private set; }
        public Node OutsideNode { get; private set; }
        private List<Node> links;

        public void ConnectToGraph()
        {
            Map map = Map.Instance;

            Building = GetComponentInParent<Building>();

            if (!isEntrance && !isExit)
                return;

            InsideNode = new Node(map.OnTerrain(transform.TransformPoint(inside)), false);
            OutsideNode = new Node(map.OnTerrain(transform.TransformPoint(outside)), false);
            float distance = Node.Distance(InsideNode, OutsideNode);

            links = NavigationGraph.NearestSquareNodes(OutsideNode.Position, false);

            if (isEntrance)
            {
                InsideNode.Neighbours.TryAdd(Building.Enter, 0);
                OutsideNode.Neighbours.TryAdd(InsideNode, distance);
                foreach (Node link in links)
                    link.Neighbours.TryAdd(OutsideNode, Node.Distance(link, OutsideNode));
            }

            if (isExit)
            {
                Building.Exit.Neighbours.TryAdd(InsideNode, 0);
                InsideNode.Neighbours.TryAdd(OutsideNode, distance);
                foreach (Node link in links)
                    OutsideNode.Neighbours.TryAdd(link, Node.Distance(OutsideNode, link));
            }
        }

        public void DisconnectFromGraph()
        {
            foreach (Node l in links)
                OutsideNode.Neighbours.TryRemove(l, out _);

            foreach (Node l in links)
                l.Neighbours.TryRemove(OutsideNode, out _);

            Building.Exit.Neighbours.TryRemove(InsideNode, out _);

            OutsideNode = null;
            links = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Vector3 insideWorld = transform.TransformPoint(inside);
            Vector3 outsideWorld = transform.TransformPoint(outside);

            Gizmos.DrawLine(insideWorld, outsideWorld);
            if (isExit)
                ExtraGizmos.DrawArrowHead(insideWorld, outsideWorld, .1f, 20);
            if (isEntrance)
                ExtraGizmos.DrawArrowHead(outsideWorld, insideWorld, .1f, 20);
        }

        private void OnDrawGizmosSelected()
        {
            if (OutsideNode != null && OutsideNode.Neighbours != null)
            {
                Gizmos.color = Color.white;
                foreach(Node neighbour in OutsideNode.Neighbours.Keys)
                    Gizmos.DrawLine(OutsideNode.Position, neighbour.Position);
            }
        }
    }
}
