using UnityEngine;

namespace HexClicker.Behaviour
{
    public delegate float Evaluate(Agent target);

    public enum Evaluation
    {
        Add,
        Multiply,
        Max,
        Min
    }

    
    
    [System.Serializable]
    public class Connection : ScriptableObject
    {
        public Graph graph;
        public Rect button;
        public Node from;
        public Node to;
        public Condition condition;
        public int index;

        public Connection(Node from, Node to, Graph graph, int index)
        {
            this.from = from;
            this.to = to;
            this.graph = graph;
            this.index = index;
        }

        public float Evaluate(Agent target)
        {
            if (condition == null)
                return 1;

            return condition.Evaluate(target);
        }
    }
}
