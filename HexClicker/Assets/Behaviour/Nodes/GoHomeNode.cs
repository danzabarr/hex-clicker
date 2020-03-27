using HexClicker.Buildings;
using HexClicker.Units;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Behaviour
{
    public class GoHomeNode : Node
    {
        public bool findNew;
        public float maxCost;
        public float maxCostNew;
        [Range(0, 1)] public float takeExistingPaths;

        public override void OnBegin(Agent target)
        {
            if (target is Unit)
            {
                Unit unit = target as Unit;
                Building building = unit.Home;

                if (building != null)
                {
                    unit.NavAgent.SetDestination(building, maxCost, takeExistingPaths, 0,
                        (Navigation.Agent.Status status) =>
                        {
                            switch (status)
                            {
                                case Navigation.Agent.Status.Failed:
                                    unit.End(this, StateResult.Failed);
                                    break;

                                case Navigation.Agent.Status.Started:
                                    break;

                                case Navigation.Agent.Status.Stopped:
                                    break;

                                case Navigation.Agent.Status.Obstructed:
                                    OnBegin(target);
                                    break;

                                case Navigation.Agent.Status.InvalidTarget:
                                    unit.End(this, StateResult.Failed);
                                    break;

                                case Navigation.Agent.Status.AtDestination:
                                    unit.End(this, StateResult.Succeeded);
                                    break;
                            }
                        }
                    );
                }
                else
                {
                    if (findNew)
                    {
                        unit.NavAgent.LookFor((Navigation.Node node) =>
                        {
                            if (!(node is Navigation.BuildingNode))
                                return false;

                            Building b = (node as Navigation.BuildingNode).Building;

                            if (!(b is Home))
                                return false;

                            Home home = b as Home;

                            return home.EmptySpaces > 0;
                        },

                        false, maxCostNew, takeExistingPaths, 0,

                        (Navigation.Agent.Status status) =>
                        {
                            switch (status)
                            {
                                case Navigation.Agent.Status.Failed:
                                    unit.End(this, StateResult.Failed);
                                    break;

                                case Navigation.Agent.Status.Started:

                                    if (unit.NavAgent.DestinationBuilding == null)
                                    {
                                        unit.End(this, StateResult.Failed);
                                    }
                                    else
                                    {
                                        Home home = unit.NavAgent.DestinationBuilding as Home;
                                        home.AddResident(unit);
                                    }

                                    break;

                                case Navigation.Agent.Status.Stopped:
                                    break;

                                case Navigation.Agent.Status.Obstructed:
                                    OnBegin(target);
                                    break;

                                case Navigation.Agent.Status.InvalidTarget:
                                    unit.End(this, StateResult.Failed);
                                    break;

                                case Navigation.Agent.Status.AtDestination:
                                    unit.End(this, StateResult.Succeeded);
                                    break;
                            }
                        }
                        );
                    }
                }
            }
        }

        public override void OnResume(Agent target)
        {
            OnBegin(target);
        }
    }
}
