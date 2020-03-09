using System;
using TMPro;
using UnityEngine;

namespace HexClicker.UI.Options
{
    public class OptionIntegerInput : MonoBehaviour
    {
        [SerializeField] private string key;
        [SerializeField] private bool onlyPositive;
        [SerializeField] private TMP_InputField inputField;

        private void Awake()
        {
            int value = 0;
            try { value = Convert.ToInt32(inputField.text); }
            catch (Exception) { }

            value = PlayerPrefs.GetInt(key, value);

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
            int value = 0;
            try { value = Convert.ToInt32(inputField.text); }
            catch (Exception) { }
            if (onlyPositive)
            {
                value = Mathf.Abs(value);
            }
            inputField.SetTextWithoutNotify(value.ToString());
            PlayerPrefs.SetInt(key, value);
        }
    }
}
