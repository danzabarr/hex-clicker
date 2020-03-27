using HexClicker.Behaviour.Jobs;
using HexClicker.Buildings;
using HexClicker.Items;
using HexClicker.World;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Units
{
    [RequireComponent(typeof(Navigation.Agent))]
    public class Unit : Behaviour.Agent
    {
        public Navigation.Agent NavAgent { get; private set; }
        public Trees.Tree TargetTree { get; set; }
        public Home Home { get; set; }
        public Navigation.WorkPoint Workplace { get; set; }

        private List<Item> heldItems = new List<Item>();
        public bool Carry(string item, int quantity)
        {
            if (item == null || quantity <= 0)
                return false;

            foreach (Item i in heldItems)
            {
                if (i != null && i.id == item)
                {
                    i.quantity += quantity;
                    return true;
                }
            }

            if (!ItemDB.TryGet(item, out ItemData _))
                return false;

            heldItems.Add(new Item(item, quantity));
            return true;
        }

        public int Drop(string item, int quantity)
        {
            if (item == null || quantity <= 0)
                return 0;

            for (int i = 0; i < heldItems.Count; i++)
            {
                if (heldItems[i] != null && heldItems[i].id == item)
                {
                    int toDrop = heldItems[i].quantity;
                    heldItems.RemoveAt(i);
                    return toDrop;
                }
            }

            return 0;
        }

        public float ConstructionRate => 1;

        private void Awake()
        {
            NavAgent = GetComponent<Navigation.Agent>();
        }
    }
}
