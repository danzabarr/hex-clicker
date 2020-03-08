using System;
using TMPro;
using UnityEngine;

namespace HexClicker.UI.Options
{
    public class OptionIntInput : MonoBehaviour
    {
        [SerializeField] private new string name;
        [SerializeField] private int value;
        [SerializeField] private bool onlyPositive;
        [SerializeField] private TMP_InputField inputField;

        private void Awake()
        {
            value = PlayerPrefs.GetInt(name, value);
            if (onlyPositive)
            {
                value = Mathf.Abs(value);
            }
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.text = value.ToString();
            inputField.textComponent.alignment = TextAlignmentOptions.MidlineRight;
        }

        public void UpdateValue()
        {
            value = Convert.ToInt32(inputField.text);
            if (onlyPositive)
            {
                value = Mathf.Abs(value);
            }
            PlayerPrefs.SetInt(name, value);
        }
    }
}
