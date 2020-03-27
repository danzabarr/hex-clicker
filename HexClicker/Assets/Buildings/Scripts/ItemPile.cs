using HexClicker.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPile : MonoBehaviour
{
    [SerializeField, Range(0, 1)] private float fill;
    [SerializeField] private GameObject[] pile;
    public float Fill
    {
        get => fill;
        set
        {
            fill = Mathf.Clamp(value, 0, 1);
            Refresh();
        }
    }

    private void OnValidate()
    {
        Refresh();
    }

    public void Refresh()
    {
        int shownItems = Mathf.CeilToInt(pile.Length * fill);
        for (int i = 0; i < pile.Length; i++)
            pile[i].SetActive(i < shownItems);
    }
}
