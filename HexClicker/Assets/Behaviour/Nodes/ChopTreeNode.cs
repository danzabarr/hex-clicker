using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexClicker.Units;
using HexClicker.Navigation;
using HexClicker.World;
using UnityEditor;

namespace HexClicker.Behaviour
{
    public class ChopTreeNode : Node
    {
        public float maxPathCost;

        public override void OnBegin(Agent target)
        {
            if (target is Unit)
            {
                Unit unit = target as Unit;

                unit.NavAgent.LookForTree(maxPathCost,
                    
                    (Navigation.Agent.Status status) =>
                    {
                        switch (status)
                        {
                            case Navigation.Agent.Status.Started:
                                if (Map.Instance.TryGetTree(unit.NavAgent.DestinationNode.Vertex, out Trees.Tree tree))
                                {
                                    unit.TargetTree = tree;
                                    tree.tagged = true;
                                }
                                else
                                {
                                    unit.NavAgent.Stop();
                                    target.End(this, StateResult.Failed);
                                }
                                break;

                            case Navigation.Agent.Status.Failed:
                                Debug.Log("ChopTree " + status);
                                target.End(this, StateResult.Failed);
                                break;

                            case Navigation.Agent.Status.Obstructed:
                                target.End(this, StateResult.Failed);
                                break;

                            case Navigation.Agent.Status.InvalidTarget:
                                target.End(this, StateResult.Failed);
                                break;

                            case Navigation.Agent.Status.AtDestination:

                                //Temporary
                                unit.StartCoroutine(unit.Chop(this, 3f));
                                break;
                        }
                    }
                );
            }
        }

        

        public override void OnEnd(Agent target)
        {

        }

        public override void OnPause(Agent target)
        {
            if (target is Unit)
            {
                Unit unit = target as Unit;
                unit.NavAgent.Stop();
            }
        }

        public override void OnResume(Agent target)
        {
            OnBegin(target);
        }
    }
}
