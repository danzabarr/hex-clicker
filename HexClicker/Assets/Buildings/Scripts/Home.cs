using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexClicker.Units;

namespace HexClicker.Buildings
{
    public class Home : Building
    {
        [SerializeField] private List<Unit> residents;

        public int Capacity
        {
            get => residents.Count;
            set
            {
                value = Mathf.Max(0, value);

                Unit[] newResidents = new Unit[value];

                int j = 0;

                for (int i = 0; i < value; i++)
                {
                    for (; j < residents.Count; j++)
                    {
                        if (residents[j] == null)
                            continue;
                        newResidents[i] = residents[j];
                        j++;
                        break;
                    }
                }
                for (; j < residents.Count; j++)
                {
                    if (residents[j] == null)
                        continue;
                    residents[j].Home = null;
                }
                residents = new List<Unit>(newResidents);
            }
        }

        public bool AddResident(Unit unit)
        {
            if (unit == null)
                return false;

            int index = -1;
            for (int i = 0; i < residents.Count; i++)
                if (residents[i] == unit)
                    return false;
                else if (residents[i] == null)
                    index = i;
            if (index < 0)
                return false;
            residents[index] = unit;

            if (unit.Home != null)
                unit.Home.RemoveResident(unit);
            unit.Home = this;

            return false;
        }

        public bool RemoveResident(Unit unit)
        {
            if (unit == null)
                return false;

            for (int i = 0; i < residents.Count; i++)
                if (residents[i] == unit)
                {
                    residents[i] = null;
                    unit.Home = null;
                    return true;
                }
            return false;
        }

        public int EmptySpaces
        {
            get
            {
                int spaces = 0;
                foreach (Unit unit in residents)
                    if (unit == null)
                        spaces++;
                return spaces;
            }
        }

        public int FilledSpaces 
        {
            get
            {
                int spaces = 0;
                foreach (Unit unit in residents)
                    if (unit != null)
                        spaces++;
                return spaces;
            }
        }
    }
}
