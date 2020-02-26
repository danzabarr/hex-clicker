using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker
{
    public class OnTerrain : MonoBehaviour
    {
        void Update()
        {
            transform.position = World.Map.Instance.OnTerrain(transform.position);
        }

        //private void OnMouseOver()
        //{
        //    Debug.Log("Eek");
        //}
    }
}
