using HexClicker.Buildings;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class StorageNode : Node
    {
        public Storage Storage { get; private set; }
        public Building Building => Storage.Building;
        public StorageNode(Storage storage, Vector3 position) : base(position)
        {
            Storage = storage;
        }
    }
}
