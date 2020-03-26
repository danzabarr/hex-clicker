using System.Collections;
using System.Collections.Generic;
using HexClicker.Items;
using HexClicker.Navigation;
using UnityEngine;

namespace HexClicker.Buildings
{
    public class Storage : MonoBehaviour
    {
        [SerializeField] private AccessPoint[] accessPoints;
        [SerializeField] private WorkPoint[] workPoints;
        [SerializeField] private bool connectToGraph;
        [SerializeField] private Item[] items;
        public StorageNode Node { get; private set; }
        public Building Building { get; private set; }
        private List<Node> links;

        public void Connect()
        {
            Building = GetComponentInParent<Building>();
            Node = new StorageNode(this, transform.position);

            if (connectToGraph)
            {
                links = NavigationGraph.NearestSquareNodes(transform.position, true);
                foreach (Node link in links)
                {
                    float distance = Navigation.Node.Distance(Node, link);
                    link.Neighbours.TryAdd(Node, distance);
                    Node.Neighbours.TryAdd(link, distance);
                }
            }

            foreach (AccessPoint ap in accessPoints)
            {
                float distance = Navigation.Node.Distance(Node, ap.InsideNode);
                ap.InsideNode.Neighbours.TryAdd(Node, distance);
                Node.Neighbours.TryAdd(ap.InsideNode, distance);
            }

            foreach (WorkPoint wp in workPoints)
            {
                float distance = Navigation.Node.Distance(Node, wp.Node);
                wp.Node.Neighbours.TryAdd(Node, distance);
                Node.Neighbours.TryAdd(wp.Node, distance);
            }
        }

        public void Disconnect()
        {
            if (links != null)
            {
                foreach (Node link in links)
                {
                    link.Neighbours.TryRemove(Node, out _);
                    Node.Neighbours.TryRemove(link, out _);
                }
            }
            foreach (AccessPoint ap in accessPoints)
            {
                ap.InsideNode.Neighbours.TryRemove(Node, out _);
                Node.Neighbours.TryRemove(ap.InsideNode, out _);
            }

            foreach (WorkPoint wp in workPoints)
            {
                wp.Node.Neighbours.TryRemove(Node, out _);
                Node.Neighbours.TryRemove(wp.Node, out _);
            }
        }

        public virtual bool AcceptsItem(string name) => true;

        /// <summary>
        /// Returns the quantity of items that would be added
        /// </summary>
        public int SpaceFor(string name, int quantity)
        {
            if (!AcceptsItem(name))
                return 0;

            if (!ItemDB.TryGet(name, out Item prefab))
                return 0;

            if (prefab == null)
                return 0;

            int added = 0;
            if (prefab.Splittable)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] == null)
                        continue;
                    if (items[i].name == name)
                    {
                        int space = items[i].MaxStorageStack - items[i].Quantity;
                        if (space <= 0)
                            continue;

                        int toAdd = Mathf.Min(space, quantity);

                        quantity -= toAdd;
                        added += toAdd;
                        if (quantity <= 0)
                            return added;
                    }
                }

                if (quantity <= 0)
                    return added;
            }


            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                    continue;

                if (prefab.Splittable)
                {
                    int toAdd = Mathf.Min(quantity, prefab.MaxStorageStack);
                    quantity -= toAdd;
                    added += toAdd;

                    if (quantity <= 0)
                        return added;
                }
                else
                {
                    added = quantity;
                    return added;
                }
            }

            return added;
        }

        /// <summary>
        /// Returns the quantity of items added.
        /// </summary>
        public int AddItem(string name, int quantity)
        {
            if (!AcceptsItem(name))
                return 0;

            if (!ItemDB.TryGet(name, out Item prefab))
                return 0;

            if (prefab == null)
                return 0;

            int added = 0;
            if (prefab.Splittable)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] == null)
                        continue;
                    if (items[i].name == name)
                    {
                        int space = items[i].MaxStorageStack - items[i].Quantity;
                        if (space <= 0)
                            continue;

                        int toAdd = Mathf.Min(space, quantity);

                        items[i].Quantity += toAdd;
                        quantity -= toAdd;
                        added += toAdd;
                        if (quantity <= 0)
                            return added;
                    }
                }

                if (quantity <= 0)
                    return added;
            }
            

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                    continue;

                if (prefab.Splittable)
                {
                    int toAdd = Mathf.Min(quantity, prefab.MaxStorageStack);

                    items[i] = Instantiate(prefab);
                    items[i].Quantity = toAdd;
                    quantity -= toAdd;
                    added += toAdd;

                    if (quantity <= 0)
                        return added;
                }
                else
                {
                    items[i] = Instantiate(prefab);
                    items[i].Quantity = quantity;
                    added = quantity;
                    return added;
                }
            }

            return added;
        }

        public int QuantityStored(string name)
        {
            int quantity = 0;
            foreach (Item item in items)
                if (item != null && item.name == name)
                    quantity += item.Quantity;
            return quantity;
        }

        public int StacksStored(string name)
        {
            int stacks = 0;
            foreach (Item item in items)
                if (item != null && item.name == name)
                    stacks++;
            return stacks;
        }
    }
}
