using UnityEngine;

namespace HexClicker.Behaviour
{
    [CreateAssetMenu(fileName = "Success Condition", menuName = "Behaviour/Conditions/Success")]
    public class SuccessCondition : Condition
    {
        public float successValue;

        public override float Evaluate(Agent agent)
        {
            return agent.Result == StateResult.Failed ? 0 : successValue;
        }
    }
}
