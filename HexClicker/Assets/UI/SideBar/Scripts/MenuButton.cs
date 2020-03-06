using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HexClicker.UI.SideBar
{
    public class MenuButton : MonoBehaviour
    {
        [SerializeField] private CanvasFader menu;
        private Toggle toggle;

        private void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        public void OnToggle()
        {
            if (toggle == null || menu == null)
                return;
            
            if (toggle.isOn)
                menu.StartFadeIn();
            else
                menu.StartFadeOut();
        }
    }
}
