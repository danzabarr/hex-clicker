using HexClicker.Buildings;
using HexClicker.Items;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class StoragePoint : WorkPoint
    {
        [SerializeField] private int index;
        public int Index => index;
        public Item Item => (Building as Stockpile)?.Item(Index);
    }
}
