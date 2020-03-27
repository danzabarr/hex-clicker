using HexClicker.Buildings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingButton : MonoBehaviour
{
    [SerializeField] private Building building;

    public void Toggle(bool toggle)
    {
        if (toggle)
            BuildingManager.Instance.SetPlacingObject(building);
        else
            BuildingManager.Instance.SetPlacingObject(null);
    }
}
