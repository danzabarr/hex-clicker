using HexClicker.World;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowTerrain : MonoBehaviour
{
    public void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        transform.position = Map.Instance.OnTerrain(transform.position);
    }
}
