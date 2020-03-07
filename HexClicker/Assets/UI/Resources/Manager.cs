using System;
using HexClicker.UI.Notifications;
using UnityEngine;

namespace HexClicker.UI.Resources
{
    public class Manager : MonoBehaviour
    {
        public static Manager Instance { get; set; }
        public DisplayItem[] DisplayItems;
        

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != null)
            {
                Destroy(gameObject);
            }
        }

        public void ButtoTest()
        {
            UpdateDisplayItem("checkbox", 1000000);
        }

        public void UpdateDisplayItem(string itemName, int newValue)
        {
            foreach (DisplayItem i in DisplayItems)
            {
                if (i.GetName() == itemName)
                {
                    i.UpdateDisplay(newValue);
                    return;
                }
                else
                {
                    NotificationSystem.Instance.Post("Resource Manager Error",
                        "The " + itemName +
                        " doesn't exist or can't be updated. Please contact Dan cause he fixes shit");
                    
                    Debug.LogWarning("There is no resource with the passed on name! The passed on name was: " + name);
                }
            }
        }

        public void UpdateDisplayItem(int id, int newValue)
        {
            foreach (DisplayItem i in DisplayItems)
            {
                if (i.GetID() == id)
                {
                    i.UpdateDisplay(newValue);
                    return;
                }
                else
                {
                    NotificationSystem.Instance.Post("Resource Manager Error",
                        "The it with ID number: " + id +
                        " doesn't exist or can't be updated. Please contact Dan cause he fixes shit");
                    Debug.LogWarning("There is no resource with the passed on ID! The passed on ID was: " + id);
                }
            }
        }

        public void SetDisplayItem(string itemName, bool active)
        {
            foreach (DisplayItem i in DisplayItems)
            {
                if (i.GetName() == itemName)
                {
                    i.gameObject.SetActive(active);
                }
                else
                {
                    NotificationSystem.Instance.Post("Resource Manager Error",
                        "The " + itemName +
                        " doesn't exist or can't be updated. Please contact Dan cause he fixes shit");
                    
                    Debug.LogWarning("There is no resource with the passed on name! The passed on name was: " + name);
                }
            }
        }
        
        public void SetDisplayItem(int id, bool active)
        {
            foreach (DisplayItem i in DisplayItems)
            {
                if (i.GetID() == id)
                {
                    i.gameObject.SetActive(active);
                    return;
                }
                else
                {
                    NotificationSystem.Instance.Post("Resource Manager Error",
                        "The it with ID number: " + id +
                        " doesn't exist or can't be updated. Please contact Dan cause he fixes shit");
                    Debug.LogWarning("There is no resource with the passed on ID! The passed on ID was: " + id);
                }
            }
        }
    }
}
