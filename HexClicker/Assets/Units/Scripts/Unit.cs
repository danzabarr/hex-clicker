using HexClicker.Behaviour.Jobs;
using HexClicker.Buildings;
using HexClicker.World;
using System.Collections;
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

        public float ConstructionRate => 1;

        private void Awake()
        {
            NavAgent = GetComponent<Navigation.Agent>();
        }
    }
}
