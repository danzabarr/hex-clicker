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
    public class Access : MonoBehaviour
    {
        [System.Serializable]
        public struct Path
        {
            public bool isEntrance;
            public bool isExit;
            public Vector3 inside;
            public Vector3 outside;
        }

        [SerializeField] private Building parent;
        public bool showHandles;
        [SerializeField] private Path[] paths;

        private List<Node> links;
        private List<Node> insideNodes;
        private List<Node> outsideNodes;

        public void ConnectToGraph()
        {
            Map map = Map.Instance;

            insideNodes = new List<Node>();
            outsideNodes = new List<Node>();

            foreach(Path path in paths)
            {
                if (!path.isEntrance && !path.isExit)
                    continue;

                Node inside = new Node(map.OnTerrain(transform.TransformPoint(path.inside)), false, false, true);
                Node outside = new Node(map.OnTerrain(transform.TransformPoint(path.outside)), false, false, true);
                float distance = Node.Distance(inside, outside);

                insideNodes.Add(inside);
                outsideNodes.Add(outside);

                links = NavigationGraph.NearestSquareNodes(outside.Position);

                if (path.isEntrance)
                {
                    inside.Neighbours.Add(new Node.Neighbour(parent.Enter, 0));
                    outside.Neighbours.Add(new Node.Neighbour(inside, distance));
                    foreach (Node link in links)
                        link.Neighbours.Add(new Node.Neighbour(outside, Node.Distance(link, outside)));
                }

                if (path.isExit)
                {
                    parent.Exit.Neighbours.Add(new Node.Neighbour(inside, 0));
                    inside.Neighbours.Add(new Node.Neighbour(outside, distance));
                    foreach (Node link in links)
                        outside.Neighbours.Add(new Node.Neighbour(link, Node.Distance(outside, link)));
                }
            }
        }

        public void DisconnectFromGraph()
        {
            foreach (Node o in outsideNodes)
                foreach (Node l in links)
                    o.RemoveNeighbour(l);
            

            foreach (Node l in links)
                foreach(Node o in outsideNodes)
                    l.RemoveNeighbour(o);

            foreach (Node i in insideNodes)
                parent.Exit.RemoveNeighbour(i);


            outsideNodes = null;
            links = null;
        }

        private void OnDrawGizmos()
        {
            foreach(Path path in paths)
            {
                Vector3 insideWorld = transform.TransformPoint(path.inside);
                Vector3 outsideWorld = transform.TransformPoint(path.outside);

                Gizmos.DrawLine(insideWorld, outsideWorld);
                if (path.isExit)
                    DrawArrowHead(insideWorld, outsideWorld, .2f, 20);
                if (path.isEntrance)
                    DrawArrowHead(outsideWorld, insideWorld, .2f, 20);
            }
        }

        private void OnDrawGizmosSelected()
        {
            foreach(Node node in outsideNodes)
            {
                Gizmos.DrawCube(node.Position, Vector3.one * 0.05f);
                foreach(Node.Neighbour neighbour in node.Neighbours)
                    Gizmos.DrawLine(node.Position, neighbour.Node.Position);
            }
        }

        public static void DrawArrowHead(Vector3 start, Vector3 end, float headLength, float headAngle)
        {
            Gizmos.DrawLine(end, end + Quaternion.LookRotation(end - start, Vector3.up) * Quaternion.Euler(0, -headAngle, 0) * Vector3.forward * -headLength);
            Gizmos.DrawLine(end, end + Quaternion.LookRotation(end - start, Vector3.up) * Quaternion.Euler(0, +headAngle, 0) * Vector3.forward * -headLength);
        }
    }
}
