using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HexClicker.UI.Options
{
    public abstract class OptionDropdown<Enum> : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private Dropdown dropdown;
        public Enum Value => (Enum)(System.Enum.GetValues(typeof(Enum)).GetValue(dropdown.value));

        private void Awake()
        {
            List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();
            foreach (Enum e in System.Enum.GetValues(typeof(Enum)))
                options.Add(new Dropdown.OptionData(e.ToString().Replace("_", " ")));
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
