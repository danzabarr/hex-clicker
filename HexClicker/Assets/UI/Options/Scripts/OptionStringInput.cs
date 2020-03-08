using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OptionStringInput : MonoBehaviour
{
    [SerializeField]private new string name;
    [SerializeField] private string savedString;
    [SerializeField] private TMP_InputField  inputField;
    
    private void Awake()
    {
        
        savedString = PlayerPrefs.GetString(name, savedString);
       
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.text = savedString;
    }

    public void UpdateValue()
    {
        savedString = inputField.text;
        PlayerPrefs.SetString(name, savedString);
    }
    
    private void OnEnable()
    {
        Debug.Log(PlayerPrefs.HasKey(name));
        Debug.Log(PlayerPrefs.GetString(name));
    }
}
