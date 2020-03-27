using System.Collections;
using System.Collections.Generic;
using HexClicker.Items;
using HexClicker.Navigation;
using UnityEngine;

namespace HexClicker.Buildings
{
    public class Storage : MonoBehaviour, IEnumerable<Item>
    {
        [SerializeField] private Item[] items;
        public Item this[int index] => items[index];
        public int Length => items.Length;

        public IEnumerator<Item> GetEnumerator()
        {
            return ((IEnumerable<Item>)items).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return items.GetEnumerator();
        }

        public virtual void ValueChanged(int index) { }

        public virtual bool AcceptsItem(string name) => true;

        /// <summary>
        /// Returns the quantity of items that would be added
        /// </summary>
        public int SpaceFor(string id, int quantity)
        {
            if (!AcceptsItem(id))
                return 0;

            if (!ItemDB.TryGet(id, out ItemData prefab))
                return 0;

            if (prefab == null)
                return 0;

            int added = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    continue;
                if (items[i].id != id)
                    continue;

                int space = prefab.MaxStorageStack - items[i].quantity;
                if (space <= 0)
                    continue;

                int toAdd = Mathf.Min(space, quantity);
                quantity -= toAdd;
                added += toAdd;
                if (quantity <= 0)
                    return added;
            }

            if (quantity <= 0)
                return added;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                    continue;

                int toAdd = Mathf.Min(quantity, prefab.MaxStorageStack);
                quantity -= toAdd;
                added += toAdd;

                if (quantity <= 0)
                    return added;
            }

            return added;
        }

        /// <summary>
        /// Returns the quantity of items added.
        /// </summary>
        public int AddItem(string id, int quantity)
        {
            if (!AcceptsItem(id))
                return 0;

            if (!ItemDB.TryGet(id, out ItemData prefab))
                return 0;

            if (prefab == null)
                return 0;

            int added = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    continue;
                if (items[i].id != id)
                    continue;
                int space = prefab.MaxStorageStack - items[i].quantity;
                if (space <= 0)
                    continue;

                int toAdd = Mathf.Min(space, quantity);
                items[i].quantity += toAdd;
                ValueChanged(i);
                quantity -= toAdd;
                added += toAdd;
                if (quantity <= 0)
                    return added;
            }

            if (quantity <= 0)
                return added;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] != null)
                    continue;

                int toAdd = Mathf.Min(quantity, prefab.MaxStorageStack);

                items[i] = new Item(id, toAdd);
                ValueChanged(i);
                quantity -= toAdd;
                added += toAdd;

                if (quantity <= 0)
                    return added;
            }

            return added;
        }

        public int TakeItem(string id, int quantity)
        {
            int taken = 0;
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                    continue;

                if (items[i].id != id)
                    continue;

                int toTake = Mathf.Min(items[i].quantity, quantity);
                items[i].quantity -= toTake;
                if (items[i].quantity <= 0)
                    items[i] = null;
                ValueChanged(i);
                quantity -= toTake;
                taken += toTake;

                if (quantity <= 0)
                    return taken;
            }

            return taken;
        }

        public int QuantityStored(string id)
        {
            int quantity = 0;
            foreach (Item item in items)
                if (item != null && item.id == id)
                    quantity += item.quantity;
            return quantity;
        }

        public int StacksStored(string id)
        {
            int stacks = 0;
            foreach (Item item in items)
                if (item != null && item.id == id)
                    stacks++;
            return stacks;
        }
    }
}
