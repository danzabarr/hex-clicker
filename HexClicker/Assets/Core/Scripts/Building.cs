using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    private BuildingPart[] parts;

    public void ExtractParts()
    {
        parts = gameObject.GetComponentsInChildren<BuildingPart>();

        foreach (BuildingPart part in parts)
            part.SetupPlacingObjects();
    }

    public void ToTerrain(Matrix4x4 parentTransform, HexMap map)
    {
        foreach (BuildingPart part in parts)
            part.ToTerrain(parentTransform, map);

        foreach (BuildingPart part in parts)
            part.RecalculatePlacingTransform();
    }

    public void Draw(Matrix4x4 parentTransform, int layer, Material material = null, bool shadows = true)
    {
        foreach (BuildingPart part in parts)
            part.Draw(parentTransform, layer, material, shadows);
    }

    public bool CheckCollisions(Matrix4x4 parentTransform, int layerMask)
    {
        foreach(BuildingPart part in parts)
        {
            if (part.CheckCollisions(parentTransform, layerMask))
                return true;
        }
        return false;
    }
}
