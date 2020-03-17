using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Behaviour
{
    public class EntryNode : Node
    {
        public override void OnBegin(Agent target)
        {
            target.End(this, StateResult.Succeeded);
        }
    }
}
