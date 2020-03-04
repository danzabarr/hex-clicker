using HexClicker.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class BuildingNode : Node
    {
        public readonly Building building;
        public BuildingNode(Building building)
            : base(building.transform.position, true)
        {
            this.building = building;
        }
    }
}