using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexClicker.Behaviour
{
    /// <summary>
    /// This describes what to do when a state ends and there is no valid subsequent state available.
    /// </summary>
    public enum StateMode
    {
        Single,     //The agent waits and reevaluates its connections each update until a new state is found.
        Loop,       //The agent repeats the current state.
        Restart,    //The agent restarts the behaviour from the entry node.
    }

    public abstract class Node : ScriptableObject
    {
        public Graph graph;
        public Rect rect;
        public List<Connection> connections = new List<Connection>();
        public StateMode mode;

        public Rect Button
        {
            get
            {
                float buttonWidth = rect.width / 2;
                float buttonHeight = 25;
                return new Rect(rect.x + rect.width * .5f - buttonWidth * .5f, rect.yMax - 10, buttonWidth, buttonHeight);
            }
        }

        public Vector2 InPoint => new Vector2(rect.x + rect.width * .5f, rect.yMin + 10);

        public virtual void OnBegin(Agent target) { }
        public virtual void OnEnd(Agent target) { }
        public virtual void OnPause(Agent target) { }
        public virtual void OnResume(Agent target) { }

        public Connection NextConnection(Agent target)
        {
            if (target == null)
                return null;
            Connection next = null;
            float best = 0;

            if (this != graph.any)
            {
                foreach(Connection c in graph.any.connections)
                {
                    float value = c.Evaluate(target);
                    if (value > best)
                    {
                        next = c;
                        best = value;
                    }
                }
            }

            foreach(Connection c in connections)
            {
                float value = c.Evaluate(target);
                if (value > best)
                {
                    next = c;
                    best = value;
                }
            }

            return next;
        }

        public Node NextState(Agent target)
        {
            return NextConnection(target)?.to;
        }

        public bool Connect(Node node, out Connection connection)
        {
            connection = null;
            if (node == null)
                return false;

            if (node.graph != graph)
                return false;

            if (node is EntryNode)
                return false;

            if (node is AnyNode)
                return false;

            if (HasConnection(node))
                return false;

            connections.Add(connection = new Connection(this, node, graph, connections.Count));
            return true;
        }

        public bool HasConnection(Node node)
        {
            if (node == null)
                return false;
            return connections.Any(c => c.to == node);
        }

        public bool Disconnect(Node node)
        {
            if (node == null)
                return false;

            return connections.RemoveAll(c => c.to == node) > 0;
        }
    }
}
