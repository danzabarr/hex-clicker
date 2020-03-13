using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Behaviour
{
    [CreateAssetMenu(fileName ="Random Condition", menuName ="Behaviour/Conditions/Random")]
    public class RandomCondition : Condition
    {
        public float min = 0, max = 1;

        public override float Evaluate(Agent agent)
        {
            return Random.Range(min, max);
        }
    }
}
