using HexClicker.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Behaviour
{
    public class ConstructNode : Node
    {
        public float maxCost;
        public float workDuration;
        public float workRate;
        [Range(0, 1)] public float takeExistingPaths;

        public override void OnBegin(Agent target)
        {
            if (!(target is Unit))
                return;

            Unit unit = target as Unit;

            ValidateWorkplace(unit);

            //In the case the unit has no valid workplace, look for a new one
            if (unit.Workplace == null)
            {
                unit.NavAgent.LookFor((Navigation.Node node) =>
                {
                    //Match must be a WorkNode
                    if (!(node is Navigation.WorkNode))
                        return false;

                    Navigation.WorkNode wn = node as Navigation.WorkNode;

                    //Match must have no worker
                    if (wn.Worker != null)
                        return false;

                    //Match must be a construction point
                    if (!(wn.point is Navigation.ConstructionPoint))
                        return false;

                    //Match must be an incomplete construction point
                    if ((wn.point as Navigation.ConstructionPoint).IsComplete)
                        return false;

                    return true;
                },
                false, maxCost, takeExistingPaths, 0,

                //Callback handling various stages of pathing
                (Navigation.Agent.Status status) =>
                {
                    switch (status)
                    {
                        //On path failed, end behaviour state with status failed.
                        case Navigation.Agent.Status.Failed:
                            target.End(this, StateResult.Failed, "No path found to construction node");
                            break;

                        //On path started 
                        case Navigation.Agent.Status.Started:
                            //End behaviour state with status failed if destination is no good
                            if (unit.NavAgent.DestinationNode == null)
                                unit.End(this, StateResult.Failed, "Destination was null");

                            //Assign the unit as the worker to the WorkNode at the end of the path
                            else
                            {
                                Navigation.WorkNode workNode = unit.NavAgent.DestinationNode as Navigation.WorkNode;
                                workNode.point.AssignWorker(unit);
                            }
                            break;

                        case Navigation.Agent.Status.Stopped:
                            break;

                        //On path obstructed start over
                        case Navigation.Agent.Status.Obstructed:
                            OnBegin(target);
                            break;

                        //At destination, start construction
                        case Navigation.Agent.Status.AtDestination:
                            Construct(unit, workDuration, workRate);
                            break;
                    }
                }
                );
            }
            //In the case the unit already has a valid construction type workplace, go to that node.
            else
            {
                unit.NavAgent.SetDestination(unit.Workplace.Node, maxCost, takeExistingPaths, 0, 
                (Navigation.Agent.Status status) =>
                {
                    switch (status)
                    {
                        //On path failed, end behaviour state with status failed.
                        case Navigation.Agent.Status.Failed:
                            target.End(this, StateResult.Failed, "No path found to construction node");
                            break;

                        case Navigation.Agent.Status.Started:
                            break;

                        case Navigation.Agent.Status.Stopped:
                            break;

                        //On path obstructed start over
                        case Navigation.Agent.Status.Obstructed:
                            OnBegin(target);
                            break;

                        //At destination, start construction
                        case Navigation.Agent.Status.AtDestination:
                            Construct(unit, workDuration, workRate);
                            break;
                    }
                });
            }
        }

        private void ValidateWorkplace(Unit unit)
        {
            if (unit == null)
                return;

            if (unit.Workplace == null)
                return;

            if (unit.Workplace is Navigation.ConstructionPoint)
            {
                if ((unit.Workplace as Navigation.ConstructionPoint).IsComplete)
                {
                    unit.Workplace.UnassignWorker();
                }
            }
            else
            {
                unit.Workplace.UnassignWorker();
            }
        }

        public override void OnResume(Agent target)
        {
            OnBegin(target);
        }

        
        private void Construct(Unit unit, float duration, float rateMultiplier)
        {
            unit.Workplace.OrientWorker(unit);

            unit.StartCoroutine(ConstructRoutine(unit, duration, rateMultiplier));
        }

        private IEnumerator ConstructRoutine(Unit unit, float duration, float rateMultiplier)
        {
            Navigation.ConstructionPoint cp = unit.Workplace as Navigation.ConstructionPoint;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                cp.Progress += unit.ConstructionRate * Time.deltaTime * rateMultiplier;
                if (cp.IsComplete)
                {
                    unit.End(this, StateResult.Succeeded);

                    break;
                }
                yield return null;
            }

            if (!cp.IsComplete)
            {
                unit.End(this, StateResult.Continuing, $"Work is {cp.FractionComplete:p0} complete");
            }
        }
    }
}
