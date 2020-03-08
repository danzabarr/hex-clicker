using System;
using TMPro;
using UnityEngine;

namespace HexClicker.UI.Options
{
    public class OptionCounter : MonoBehaviour
    {
        [SerializeField] private new string name;

        [SerializeField] private int value;

        [SerializeField] private int minVal;
        [SerializeField] private int maxVal = 100;

        [SerializeField] private TMP_InputField inputField;

        private void Awake()
        {
            value = PlayerPrefs.GetInt(name, value);

            inputField.text = value.ToString();
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.textComponent.alignment = TextAlignmentOptions.MidlineRight;
        }

        public void Add()
        {
            if (value + 1 > maxVal)
            {
                value = minVal;
                inputField.text = value.ToString();
            }
            else
            {
                value++;
                inputField.text = value.ToString();
            }
        }

        public void Subtract()
        {
            if (value - 1 < minVal)
            {
                value = maxVal;
                inputField.text = value.ToString();
            }
            else
            {
                value--;
                inputField.text = value.ToString();
            }
        }

        public void UpdateValue()
        {
            int i = Convert.ToInt32(inputField.text);
            
            if (i > maxVal)
            {
                i = maxVal;
                inputField.text = i.ToString();
            }

            if (i < minVal)
            {
                i = minVal;
                inputField.text = i.ToString();
            }
            
            PlayerPrefs.SetInt(name, i);
        }
    }
}
