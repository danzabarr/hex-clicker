using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.PathMask
{
    public class MaskRevealer : MonoBehaviour
    {
        private Vector2 lastPosition;
        [SerializeField] private Renderer[] renderers;
        public float sensitivity;
        public Color color;

        public void Update()
        {
            //Sets the alpha value of the color of the main material of all the renderers proportionate to the movement since last update.
            Vector2 position = transform.position.xz();
            float distance = Vector2.Distance(position, lastPosition);
            lastPosition = position;
            foreach (Renderer renderer in renderers)
                renderer.material.color = new Color(color.r, color.g, color.b, Mathf.Clamp(color.a * distance * sensitivity, 0, 1));
        }
    }
}
