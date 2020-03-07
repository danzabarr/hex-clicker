using System;
using HexClicker.UI.Tooltip;
using TMPro;
using UnityEngine;

namespace HexClicker.UI.Resources
{
    public class DisplayItem : MonoBehaviour
    {
        [SerializeField] private int id;
        [SerializeField] private string name;
        private int displayValue;
        [SerializeField]private TextMeshProUGUI displayText;
        [SerializeField] private TooltipTarget tooltipTarget;


        public void UpdateDisplay(int newValue)
        {
            displayValue = newValue;
            string n = $"{displayValue:#,0}";
            displayText.text = "<sprite name=\"" + name + "_tick\">" + n;
        }

        public int GetID()
        {
            return id;
        }
        public string GetName()
        {
            return name;
        }
    }
}
