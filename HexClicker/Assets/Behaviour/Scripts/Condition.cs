using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Behaviour
{
    public abstract class Condition : ScriptableObject
    {
        public abstract float Evaluate(Agent agent);
    }
}
