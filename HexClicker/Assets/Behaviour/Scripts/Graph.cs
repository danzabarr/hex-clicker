using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Behaviour
{
    [CreateAssetMenu(fileName ="BehaviourGraph", menuName ="Behaviour/Graph")]
    public class Graph : ScriptableObject, IEnumerable<Node>
    {
        public List<Node> nodes = new List<Node>();
        public EntryNode entry;
        public AnyNode any;

        public Node AddNode(Type type)
        {
            Node node = CreateInstance(type) as Node;
            node.graph = this;
            nodes.Add(node);
            return node;
        }

        public bool RemoveNode(Node node)
        {
            if (node == null)
                return false;

            if (!nodes.Remove(node))
                return false;

            if (node == entry)
                entry = null;

            if (node == any)
                any = null;

            foreach(Node n in nodes)
            {
                if (n == null)
                    continue;

                if (n.connections == null)
                    continue;

                for (int i = n.connections.Count - 1; i >= 0; i--)
                {
                    Connection c = n.connections[i];
                    if (c == null)
                        continue;

                    if (c.node == node)
                        n.connections.RemoveAt(i);
                }
            }
            return true;
        }

        public void Clear()
        {
            if (Application.isPlaying)
            {
                for (int i = 0; i < nodes.Count; i++)
                    Destroy(nodes[i]);
            }
            nodes.Clear();
        }

        public void OnDestroy()
        {
            Clear();
        }

        public IEnumerator<Node> GetEnumerator() => nodes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
