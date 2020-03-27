using HexClicker.Items;
using HexClicker.Navigation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Buildings
{
    public class Stockpile : Building
    {
        [SerializeField] private Transform[] quadrants;

        private ItemPile[] itemPiles;
        private Item[] items;

        public StoragePoint[] StoragePoints { get; private set; }

        private void Awake()
        {
            itemPiles = new ItemPile[quadrants.Length];
            items = new Item[quadrants.Length];
            StoragePoints = GetComponentsInChildren<StoragePoint>();
        }

        public int Index(string item)
        {
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null && items[i].id == item)
                    return i;
            return -1;
        }
        public bool Contains(string item)
        {
            return Index(item) != -1;
        }

        public Item Item(int index)
        {
            return items[index];
        }

        public string ItemID(int index)
        {
            if (items[index] == null)
                return null;
            return items[index].id;
        }

        public int Quantity(int index)
        {
            if (items[index] == null)
                return 0;
            return items[index].quantity;
        }

        public int Quantity(string item)
        {
            int quantity = 0;
            foreach(Item i in items)
                if (i != null && i.id == item)
                    quantity += i.quantity;
            return quantity;
        }

        public void SetPile(int index, string item, int quantity)
        {
            if (index < 0 || index >= itemPiles.Length)
                return;

            if (item == null || quantity <= 0)
            {
                items[index] = null;
                if (itemPiles[index] != null)
                    Destroy(itemPiles[index].gameObject);
            }
            else
            {
                if (!ItemDB.TryGet(item, out ItemData data))
                    return;

                if (items[index] == null || items[index].id != item)
                {

                    items[index] = new Item(item, quantity);

                    if (itemPiles[index] != null)
                        Destroy(itemPiles[index].gameObject);

                    itemPiles[index] = Instantiate(data.Pile, quadrants[index].transform);
                }

                items[index].quantity = quantity;
                itemPiles[index].Fill = (float)items[index].quantity / data.MaxStorageStack;
            }
        }

        public int Take(int index, string item, int quantity)
        {
            if (index < 0 || index >= itemPiles.Length)
                return 0;

            if (item == null || quantity <= 0)
                return 0;

            if (items[index] == null || items[index].id != item)
                return 0;

            int toTake = Mathf.Min(quantity, items[index].quantity);

            if (toTake <= 0)
                return 0;

            if (!ItemDB.TryGet(item, out ItemData data))
                return 0;

            items[index].quantity -= toTake;

            if (items[index].quantity <= 0)
            {
                items[index] = null;
                Destroy(itemPiles[index].gameObject);
                itemPiles[index] = null;
            }
            else
            {
                itemPiles[index].Fill = (float)items[index].quantity / data.MaxStorageStack;
            }

            return toTake;
        }

        public int Add(int index, string item, int quantity)
        {
            if (index < 0 || index >= itemPiles.Length)
                return 0;

            if (item == null || quantity <= 0)
                return 0;

            if (!ItemDB.TryGet(item, out ItemData data))
                return 0;

            if (items[index] != null && items[index].id != item)
                return 0;

            int storedQuantity = items[index] == null ? 0 : items[index].quantity;

            int toAdd = Mathf.Min(quantity, data.MaxStorageStack - storedQuantity);
            if (toAdd <= 0)
                return 0;

            if (items[index] == null)
                items[index] = new Item(item, toAdd);
            else
                items[index].quantity += toAdd;

            itemPiles[index].Fill = (float)items[index].quantity / data.MaxStorageStack;

            return toAdd;
        }
    }
}
