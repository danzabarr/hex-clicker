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

    public class Condition : ScriptableObject
    {
        public virtual float Evaluate(Agent agent) => 1;
    }
    

    [System.Serializable]
    public class Connection : ScriptableObject
    {
        public Node node;
        public Evaluation evaluation;
        public Condition[] conditions;

        public Connection(Node node)
        {
            this.node = node;
        }

        public Connection(Node node, Evaluation evaluation, params Condition[] conditions)
        {
            this.node = node;
            this.evaluation = evaluation;
            this.conditions = conditions;
        }

        public float Evaluate(Agent target)
        {
            if (conditions.Length == 0)
                return 1;

            switch (evaluation)
            {
                case Evaluation.Add:
                    float sum = 0;
                    foreach (Condition c in conditions)
                        sum += c.Evaluate(target);
                    return sum;

                case Evaluation.Multiply:
                    float product = 1;
                    foreach (Condition c in conditions)
                        product *= c.Evaluate(target);
                    return product;

                case Evaluation.Max:
                    float max = float.MinValue;
                    foreach (Condition c in conditions)
                        max = Mathf.Max(max, c.Evaluate(target));
                    return max;

                case Evaluation.Min:
                    float min = float.MaxValue;
                    foreach (Condition c in conditions)
                        min = Mathf.Min(min, c.Evaluate(target));
                    return min;

                default:
                    return 1;
            }
        }
    }
}
