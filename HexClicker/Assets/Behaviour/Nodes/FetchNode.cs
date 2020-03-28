using HexClicker.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexClicker.Navigation;
using HexClicker.Buildings;
using HexClicker.Items;

namespace HexClicker.Behaviour
{
    public class FetchNode : Node
    {
        public float maxCost;
        [Range(0, 1)] public float takeExistingPaths;
        public string item;
        public int quantity;

        public override void OnBegin(Agent target)
        {
            if (target == null)
                return;

            if (!(target is Unit))
                return;

            Unit unit = target as Unit;

            Stockpile[] stockpiles = Building.GetAll<Stockpile>();
            List<WorkNode> storagePoints = new List<WorkNode>();

            foreach (Stockpile sp in stockpiles)
                foreach(StoragePoint p in sp.StoragePoints)
                {
                    if (p.Item == null)
                        continue;
                    if (p.Item.id != item)
                        continue;
                    if (p.Item.quantity <= 0)
                        continue;
                    storagePoints.Add(p.Node);
                }

            if (storagePoints.Count <= 0)
            {
                target.End(this, StateResult.Failed, "No storage points available containing the required item.");
                return;
            }

            unit.NavAgent.GoToNearest(storagePoints.ToArray(), maxCost, takeExistingPaths, 0,
            (Navigation.Agent.Status status) =>
            {
                switch (status)
                {
                    case Navigation.Agent.Status.Failed:
                        target.End(this, StateResult.Failed, "No path found to a valid storage point.");
                        break;

                    case Navigation.Agent.Status.Started:

                        break;

                    case Navigation.Agent.Status.Stopped:
                        break;

                    case Navigation.Agent.Status.Obstructed:
                        OnBegin(target);
                        break;

                    case Navigation.Agent.Status.AtDestination:

                        Navigation.Node destination = unit.NavAgent.DestinationNode;
                        if (destination == null || !(destination is WorkNode))
                        {
                            target.End(this, StateResult.Failed, "Destination was not a valid storage point.");
                            break;
                        }

                        WorkNode wn = destination as WorkNode;
                        if (wn.point == null || !(wn.point is StoragePoint))
                        {
                            target.End(this, StateResult.Failed, "Destination was not a valid storage point.");
                            break;
                        }

                        StoragePoint sp = wn.point as StoragePoint;

                        if (sp.Item == null || sp.Item.id != item)
                        {
                            target.End(this, StateResult.Failed, "Destination was not valid storage point.");
                            break;
                        }
                        Stockpile stockpile = sp.Building as Stockpile;
                        int index = sp.Index;
                        int taken = stockpile.Take(index, item, quantity);

                        unit.Carry(item, quantity);
                        if (taken < quantity)
                            target.End(this, StateResult.Continuing, $"Picked up {taken}/{quantity}");
                        else
                            target.End(this, StateResult.Succeeded);

                        break;
                }
            });

            /*

            unit.NavAgent.LookFor((Navigation.Node node) =>
            {
                if (node == null)
                    return false;

                if (!(node is WorkNode))
                    return false;

                WorkNode wn = node as WorkNode;
                if (!(wn.point is StoragePoint))
                    return false;

                StoragePoint sp = wn.point as StoragePoint;

                Item storedItem = sp.Item;
                if (storedItem == null)
                    return false;

                if (storedItem.id != item)
                    return false;

                return true;
            }, 
            false, maxCost, takeExistingPaths, 0,
            (Navigation.Agent.Status status) =>
            {
                switch (status)
                {
                    case Navigation.Agent.Status.Failed:
                        target.End(this, StateResult.Failed);
                        break;

                    case Navigation.Agent.Status.Started:
                        
                        break;

                    case Navigation.Agent.Status.Stopped:
                        break;

                    case Navigation.Agent.Status.Obstructed:
                        OnBegin(target);
                        break;

                    case Navigation.Agent.Status.InvalidTarget:
                        target.End(this, StateResult.Failed);
                        break;

                    case Navigation.Agent.Status.AtDestination:

                        Navigation.Node destination = unit.NavAgent.DestinationNode;
                        if (destination == null || !(destination is WorkNode))
                        {
                            target.End(this, StateResult.Failed);
                            break;
                        }

                        WorkNode wn = destination as WorkNode;
                        if (wn.point == null || !(wn.point is StoragePoint))
                        {
                            target.End(this, StateResult.Failed);
                            break;
                        }

                        StoragePoint sp = wn.point as StoragePoint;

                        if (sp.Item == null || sp.Item.id != item)
                        {
                            target.End(this, StateResult.Failed);
                            break;
                        }

                        Stockpile stockpile = sp.Building as Stockpile;
                        int index = sp.Index;
                        int taken = stockpile.Take(index, item, quantity);

                        unit.Carry(item, quantity);
                        if (taken < quantity)
                            target.End(this, StateResult.Continuing);
                        else
                            target.End(this, StateResult.Succeeded);

                        break;
                }
            });

            */

        }
    }
}
