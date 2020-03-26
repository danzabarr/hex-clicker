using HexClicker.Buildings;
using HexClicker.Units;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class WorkNode : Node
    {
        public readonly WorkPoint point;
        public readonly float rotation;
        public Building Building => point.Building;
        public Unit Worker => point.Worker;
        public WorkNode(WorkPoint point, Vector3 position, float rotation) : base(position)
        {
            this.point = point;
            this.rotation = rotation;
        }
    }
}
