using System;
using UnityEngine;
using UnityEngine.UI;

namespace HexClicker.UI.Options
{
    public class OptionCheckBox : MonoBehaviour
    {
        [SerializeField] private new string name;
        [SerializeField] private Toggle toggle;

        private void Awake()
        {
            toggle.isOn = Convert.ToBoolean(PlayerPrefs.GetInt(name, toggle.isOn ? 1 : 0));
        }
        
        public void UpdateValue()
        { 
            PlayerPrefs.SetInt(name, toggle.isOn ? 1 : 0);
        }
    }
}
