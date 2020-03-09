using System;
using TMPro;
using UnityEngine;

namespace HexClicker.UI.Options
{
    public class OptionSpinner : MonoBehaviour
    {
        [SerializeField] private string key;

        [SerializeField] private int minimum;
        [SerializeField] private int maximum = 100;

        [SerializeField] private TMP_InputField inputField;

        private void Awake()
        {
            int value = 0;
            try { value = Convert.ToInt32(inputField.text); }
            catch(Exception) { }

            value = PlayerPrefs.GetInt(key, value);
            value = Mathf.Clamp(value, minimum, maximum);

            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.text = value.ToString();
            inputField.textComponent.alignment = TextAlignmentOptions.MidlineRight;
        }

        public void Add()
        {
            int value = 0;
            try { value = Convert.ToInt32(inputField.text); }
            catch (Exception) { }

            value++;
            value = Mathf.Clamp(value, minimum, maximum);

            inputField.SetTextWithoutNotify(value.ToString());
            PlayerPrefs.SetInt(key, value);
        }

        public void Subtract()
        {
            int value = 0;
            try { value = Convert.ToInt32(inputField.text); }
            catch (Exception) { }

            value--;
            value = Mathf.Clamp(value, minimum, maximum);

            inputField.SetTextWithoutNotify(value.ToString());
            PlayerPrefs.SetInt(key, value);
        }

        public void UpdateValue()
        {
            int value = 0;
            try { value = Convert.ToInt32(inputField.text); }
            catch (Exception) { }

            value = Mathf.Clamp(value, minimum, maximum);

            inputField.SetTextWithoutNotify(value.ToString());
            PlayerPrefs.SetInt(key, value);
        }
    }
}
