using System;
using UnityEngine;
using UnityEngine.UI;

namespace HexClicker.UI.Options
{
    public class OptionCheckBox : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private Toggle toggle;

        private void Awake()
        {
            toggle.isOn = Convert.ToBoolean(PlayerPrefs.GetInt(key, toggle.isOn ? 1 : 0));
        }
        
        public void UpdateValue()
        { 
            PlayerPrefs.SetInt(key, toggle.isOn ? 1 : 0);
        }
    }
}
