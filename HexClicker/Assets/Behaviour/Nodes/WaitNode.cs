using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace HexClicker.Behaviour
{
    public sealed class WaitNode : Node
    {
        public float time;

        public override void OnBegin(Agent target)
        {
            target.StartCoroutine(Routine());

            IEnumerator Routine()
            {
                for (float t = 0; t < time; t += Time.deltaTime)
                    yield return null;
                
                target.Complete(this);
            }
        }
    }
}
