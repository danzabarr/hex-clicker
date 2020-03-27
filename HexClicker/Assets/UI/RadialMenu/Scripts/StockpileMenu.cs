using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HexClicker.Buildings;

namespace HexClicker.UI.Menus
{
    public class StockpileMenu : RadialMenu
    {
        public void SetIndex0To100()
        {
            Stockpile sp = Target.GetComponent<Stockpile>();
            sp.SetPile(0, "wood", 1);
        }
        public void SetIndex1To100()
        {
            Stockpile sp = Target.GetComponent<Stockpile>();
            sp.SetPile(1, "wood", 4);
        }

        public void SetIndex2To100()
        {
            Stockpile sp = Target.GetComponent<Stockpile>();
            sp.SetPile(2, "wood", 8);
        }

        public void SetIndex3To100()
        {
            Stockpile sp = Target.GetComponent<Stockpile>();
            sp.SetPile(3, "wood", 16);
        }

        public void ClearAll()
        {
            Stockpile sp = Target.GetComponent<Stockpile>();
            sp.SetPile(0, "", 0);
            sp.SetPile(1, "", 0);
            sp.SetPile(2, "", 0);
            sp.SetPile(3, "", 0);
        }
    }
}
