using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.UI.Menus
{
    public class RadialMenuTarget : MonoBehaviour
    {
        [SerializeField] private RadialMenu menu;
        private Outline outline;
        private bool active;

        public bool Active
        {
            get => active;
            set
            {
                active = value;
                if (outline != null)
                    outline.enabled = value;
            }
        }

        private void Awake()
        {
            outline = GetComponent<Outline>();
        }

        private void Start()
        {
            Active = false;
        }

        private void OnMouseDown()
        {
            if (menu.Open(this))
            {
                Active = true;
            }
        }
    }
}

