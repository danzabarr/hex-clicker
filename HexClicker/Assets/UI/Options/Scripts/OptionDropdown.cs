using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HexClicker.UI.Options
{
    [RequireComponent(typeof(Dropdown))]
    public abstract class OptionDropdown<Enum> : MonoBehaviour
    {
        [SerializeField] private string key;
        private Dropdown dropdown;
        public Enum Value => (Enum)(System.Enum.GetValues(typeof(Enum)).GetValue(dropdown.value));

        private void Awake()
        {
            dropdown = GetComponent<Dropdown>();
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (Enum e in System.Enum.GetValues(typeof(Enum)))
                options.Add(new Dropdown.OptionData(e.ToString()));
            dropdown.options = options;
            dropdown.value = PlayerPrefs.GetInt(key, dropdown.value);
        }

        private void OnValidate()
        {
            Awake();
        }

        public void UpdateValue()
        {
            PlayerPrefs.SetInt(key, dropdown.value);
        }
    }
}
