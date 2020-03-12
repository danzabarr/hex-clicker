using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Behaviour
{
    public class AnyNode : Node
    {
        public override void OnBegin(Agent target)
        {
            target.Complete(this);
        }
    }

}
