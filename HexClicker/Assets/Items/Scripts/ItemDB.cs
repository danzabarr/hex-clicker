using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Items
{
    public static class ItemDB
    {
        private static Dictionary<string, ItemData> database;

        public static bool TryGet(string key, out ItemData value)
        {
            if (database == null)
            {
                database = new Dictionary<string, ItemData>();
                foreach (ItemData item in Resources.LoadAll<ItemData>(""))
                    database.Add(item.ID, item);
            }

            return database.TryGetValue(key, out value);
        }
    }
}
