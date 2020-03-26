using HexClicker.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class BuildingNode : Node
    {
        public Building Building { get; private set; }
        public BuildingNode(Building building)
            : base(building.transform.position, true)
        {
            this.Building = building;
        }
    }
}