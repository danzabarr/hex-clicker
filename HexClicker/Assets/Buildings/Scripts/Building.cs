using HexClicker.Navigation;
using UnityEngine;

namespace HexClicker.Buildings
{
    public class Building : MonoBehaviour
    {
        private BuildingPart[] parts;
        private Area[] areas;
        private Access[] accesses;

        public void OnPlace()
        {
            ExtractParts();
            foreach (Area area in areas)
                area.Apply();
            foreach (Access access in accesses)
                access.ConnectToGraph();
        }

        public void ExtractParts()
        {
            parts = gameObject.GetComponentsInChildren<BuildingPart>();

            foreach (BuildingPart part in parts)
                part.SetupPlacingObjects();

            areas = gameObject.GetComponentsInChildren<Area>();

            accesses = gameObject.GetComponentsInChildren<Access>();
        }

        public void ToTerrain(Matrix4x4 parentTransform)
        {
            foreach (BuildingPart part in parts)
                part.ToTerrain(parentTransform);

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
            foreach (BuildingPart part in parts)
            {
                if (part.CheckCollisions(parentTransform, layerMask))
                    return true;
            }
            return false;
        }
    }
}
