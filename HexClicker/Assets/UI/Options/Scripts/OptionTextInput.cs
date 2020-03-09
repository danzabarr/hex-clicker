using TMPro;
using UnityEngine;

public class OptionTextInput : MonoBehaviour
{
    [SerializeField] private string key;
    [SerializeField] private TMP_InputField inputField;
    
    private void Awake()
    {
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.text = PlayerPrefs.GetString(key, inputField.text);
        inputField.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
    }

    public void UpdateValue()
    {
        PlayerPrefs.SetString(key, inputField.text);
    }
}
