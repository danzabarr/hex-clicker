using HexClicker.Buildings;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Behaviour.Jobs
{
    public enum Workplace
    {
        Bakery
    }

    public enum Tool
    {

    }

    public class Job 
    {
        public static Dictionary<Workplace, Type> WorkplaceTypes = new Dictionary<Workplace, Type>()
        {
            { Workplace.Bakery, typeof(Bakery) }
        };

        public static Dictionary<Tool, Type> ToolTypes = new Dictionary<Tool, Type>()
        {

        };
    }
}
