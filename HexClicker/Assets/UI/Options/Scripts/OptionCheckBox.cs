using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HexClicker.UI.Options
{
    public class OptionCheckBox : MonoBehaviour
    {
        [SerializeField]private new string name;
        [SerializeField] private bool state;
        [SerializeField] private Toggle  toggle;
        //[SerializeField] private UnityEvent onValueChanged;


        private void Awake()
        {
           state = Convert.ToBoolean(PlayerPrefs.GetInt(name));
           toggle.isOn = state;

           //Debug.Log(PlayerPrefs.HasKey(name));
        }
        
        public void UpdateValue()
        { 
            state = toggle.isOn;
            //Debug.Log(Convert.ToInt32(state));
            PlayerPrefs.SetInt(name, Convert.ToInt32(state));
           
        }

        private void OnEnable()
        {
            Debug.Log(PlayerPrefs.HasKey(name));
            Debug.Log(PlayerPrefs.GetInt(name));
        }
    }
}
