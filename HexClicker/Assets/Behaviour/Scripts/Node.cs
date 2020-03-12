using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HexClicker.Behaviour
{
    public enum StateMode
    {
        Single,
        Loop,
        Restart,
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
                float buttonWidth = 50;
                float buttonHeight = 15;
                return new Rect(rect.x + rect.width * .5f - buttonWidth * .5f, rect.yMax - 10, buttonWidth, buttonHeight);
            }
        }

        public Vector2 InPoint => new Vector2(rect.x + rect.width * .5f, rect.yMin + 10);

        public virtual void OnBegin(Agent target) { }
        public virtual void OnEnd(Agent target) { }
        public virtual void OnPause(Agent target) { }
        public virtual void OnResume(Agent target) { }

        public Node Evaluate(Agent target)
        {
            int index = -1;
            float highest = 0;
            for (int i = 0; i < connections.Count; i++)
            {
                float value = connections[i].Evaluate(target);
                if (value > highest)
                {
                    index = i;
                    highest = value;
                }
            }

            if (index < 0)
                return null;

            return connections[index].node;
        }

        public bool Connect(Node node, out Connection connection)
        {
            connection = null;
            if (node == null)
                return false;
            if (HasConnection(node))
                return false;
            connections.Add(connection = new Connection(node));
            return true;
        }

        public bool Connect(Node node, out Connection connection, Evaluation type, Condition conditions)
        {
            connection = null;
            if (node == null)
                return false;
            if (HasConnection(node))
                return false;
            connections.Add(connection = new Connection(node, type, conditions));
            return true;
        }

        public bool HasConnection(Node node)
        {
            if (node == null)
                return false;
            return connections.Any(c => c.node == node);
        }

        public bool Disconnect(Node node)
        {
            if (node == null)
                return false;
            if (!HasConnection(node))
                return false;

            return connections.RemoveAll(c => c.node == node) > 0;
        }
    }
}
