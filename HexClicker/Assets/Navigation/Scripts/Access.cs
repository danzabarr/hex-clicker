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

                Node inside = new Node(map.OnTerrain(transform.TransformPoint(path.inside)), false);
                Node outside = new Node(map.OnTerrain(transform.TransformPoint(path.outside)), false);
                float distance = Node.Distance(inside, outside);

                insideNodes.Add(inside);
                outsideNodes.Add(outside);

                links = NavigationGraph.NearestSquareNodes(outside.Position, false);

                if (path.isEntrance)
                {
                    inside.Neighbours.Add(parent.Enter, 0);
                    outside.Neighbours.Add(inside, distance);
                    foreach (Node link in links)
                        link.Neighbours.Add(outside, Node.Distance(link, outside));
                }

                if (path.isExit)
                {
                    parent.Exit.Neighbours.Add(inside, 0);
                    inside.Neighbours.Add(outside, distance);
                    foreach (Node link in links)
                        outside.Neighbours.Add(link, Node.Distance(outside, link));
                }
            }
        }

        public void DisconnectFromGraph()
        {
            foreach (Node o in outsideNodes)
                foreach (Node l in links)
                    o.Neighbours.Remove(l);


            foreach (Node l in links)
                foreach (Node o in outsideNodes)
                    l.Neighbours.Remove(o);

            foreach (Node i in insideNodes)
                parent.Exit.Neighbours.Remove(i);

            outsideNodes = null;
            links = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            foreach (Path path in paths)
            {
                Vector3 insideWorld = transform.TransformPoint(path.inside);
                Vector3 outsideWorld = transform.TransformPoint(path.outside);

                Gizmos.DrawLine(insideWorld, outsideWorld);
                if (path.isExit)
                    DrawArrowHead(insideWorld, outsideWorld, .1f, 20);
                if (path.isEntrance)
                    DrawArrowHead(outsideWorld, insideWorld, .1f, 20);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (outsideNodes != null)
            {
                Gizmos.color = Color.white;
                foreach(Node node in outsideNodes)
                {
                    foreach(Node neighbour in node.Neighbours.Keys)
                        Gizmos.DrawLine(node.Position, neighbour.Position);
                }
            }
        }

        public static void DrawArrowHead(Vector3 start, Vector3 end, float headLength, float headAngle)
        {
            Gizmos.DrawLine(end, end + Quaternion.LookRotation(end - start, Vector3.up) * Quaternion.Euler(0, -headAngle, 0) * Vector3.forward * -headLength);
            Gizmos.DrawLine(end, end + Quaternion.LookRotation(end - start, Vector3.up) * Quaternion.Euler(0, +headAngle, 0) * Vector3.forward * -headLength);
        }
    }
}
