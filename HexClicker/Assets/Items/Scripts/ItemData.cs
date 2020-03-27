using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Items
{
    [CreateAssetMenu(fileName = "Item", menuName = "Items/Basic")]
    public class ItemData : ScriptableObject
    {
        [SerializeField] private string identifier;
        [SerializeField] private string descriptiveName;
        [SerializeField] private string emoticon;
        [SerializeField] private Texture2D icon;
        [SerializeField] private GameObject prefab;
        [SerializeField] private ItemPile prefabPile;
        [SerializeField] private int maxStorageStack = 64;
        public string Name => descriptiveName;
        public string ID => identifier;
        public string Emoticon => emoticon;
        public Texture2D Icon => icon;
        public GameObject Prefab => prefab;
        public ItemPile Pile => prefabPile;
        public int MaxStorageStack => maxStorageStack;

        [ReadOnly] public int Quantity;
    }

    [System.Serializable]
    public class Item
    {
        public string id;
        public int quantity;
        public ItemData Data => ItemDB.TryGet(id, out ItemData data) ? data : null;

        public Item(string id, int quantity)
        {
            this.id = id;
            this.quantity = quantity;
        }
    }
}
