using TMPro;
using UnityEngine;

public class OptionStringInput : MonoBehaviour
{
    [SerializeField] private new string name;
    [SerializeField] private TMP_InputField inputField;
    
    private void Awake()
    {
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.text = PlayerPrefs.GetString(name, inputField.text);
        inputField.textComponent.alignment = TextAlignmentOptions.MidlineLeft;
    }

    public void UpdateValue()
    {
        PlayerPrefs.SetString(name, inputField.text);
    }
}
