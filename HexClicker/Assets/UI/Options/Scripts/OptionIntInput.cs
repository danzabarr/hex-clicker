using System;
using TMPro;
using UnityEngine;

namespace HexClicker.UI.Options
{
    public class OptionIntInput : MonoBehaviour
    {
        [SerializeField]private new string name;
        [SerializeField] private int savedInt;
        [SerializeField] private bool onlyPositive;
        [SerializeField] private TMP_InputField  inputField;

        private void Awake()
        {
        
            savedInt = PlayerPrefs.GetInt(name, savedInt);
            if (onlyPositive)
            {
                savedInt = Mathf.Abs(savedInt);
            }
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.text = savedInt.ToString();
        }

        public void UpdateValue()
        {
            savedInt = Convert.ToInt32(inputField.text);
            if (onlyPositive)
            {
                savedInt = Mathf.Abs(savedInt);
            }
            PlayerPrefs.SetInt(name, savedInt);
        }
    
        private void OnEnable()
        {
            Debug.Log(PlayerPrefs.HasKey(name));
            Debug.Log(PlayerPrefs.GetInt(name));
        }
    }
}
