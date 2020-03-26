using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Items
{
    public static class ItemDB
    {
        private static Dictionary<string, Item> database;

        public static bool TryGet(string key, out Item value)
        {
            if (database == null)
            {
                database = new Dictionary<string, Item>();
                foreach (Item item in Resources.LoadAll<Item>(""))
                    database.Add(item.name, item);
            }

            return database.TryGetValue(key, out value);
        }
    }
}
