using HexClicker.Buildings;
using HexClicker.Units;
using HexClicker.World;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class WorkPoint : MonoBehaviour
    {
        [SerializeField] private bool showHandles;
        [SerializeField] private Vector3 position;
        [SerializeField] private float rotation;

        public Unit Worker { get; private set; }

        private List<Node> links;
        public WorkNode Node { get; private set; }
        public Building Building { get; private set; }
        public Quaternion Orientation => transform.rotation * Quaternion.AngleAxis(rotation, Vector3.up);

        public void OrientWorker(Unit unit)
        {
            StartCoroutine(OrientWorker(unit, 30f));
        }
        
        private IEnumerator OrientWorker(Unit unit, float degreesPerSecond)
        {
            Quaternion start = unit.transform.rotation;
            Quaternion target = Orientation;
            float delta = Quaternion.Angle(start, target);

            for (float d = 0; d < delta; d += Time.deltaTime * degreesPerSecond)
            {
                unit.transform.rotation = Quaternion.Lerp(start, target, d / delta);
                yield return null;
            }
            unit.transform.rotation = target;
        }


        public bool AssignWorker(Unit unit)
        {
            if (Worker != null)
                return false;
            Worker = unit;
            unit.Workplace = this;
            return true;
        }

        public bool UnassignWorker()
        {
            if (Worker == null)
                return false;
            Worker.Workplace = null;
            Worker = null;
            return true;
        }

        public void ConnectToGraph()
        {
            Map map = Map.Instance;
            Building = GetComponentInParent<Building>();
            Node = new WorkNode(this, map.OnTerrain(transform.TransformPoint(position)), rotation);
            links = NavigationGraph.NearestSquareNodes(Node.Position, false);

            foreach(Node link in links)
            {
                float distance = Navigation.Node.Distance(link, Node);
                Node.Neighbours.TryAdd(link, distance);
                link.Neighbours.TryAdd(Node, distance);
            }
        }

        public void DisconnectFromGraph()
        {
            Node.Neighbours.Clear();
            foreach (Node link in links)
                link.Neighbours.TryRemove(Node, out _);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Vector3 world = transform.TransformPoint(position);

            float length = .125f;

            Vector3 vector = Orientation * Vector3.forward;
            Gizmos.DrawSphere(world, .05f);
            Gizmos.DrawLine(world, world + vector * length);
            ExtraGizmos.DrawArrowHead(world, world + vector * length, .05f, 20);
        }

        private void OnDrawGizmosSelected()
        {
            if (Node != null && Node.Neighbours != null)
            {
                Gizmos.color = Color.yellow;
                foreach (Node neighbour in Node.Neighbours.Keys)
                    Gizmos.DrawLine(Node.Position, neighbour.Position);
            }
        }
    }
}
