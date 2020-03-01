using HexClicker.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Buildings
{
    public class Building : MonoBehaviour
    {
        private BuildingPart[] parts;
        private Area[] areas;
        public Node Enter { get; private set; }
        public Node Exit { get; private set; }

        private Access[] accesses;

        public void OnPlace()
        {
            Enter = new Node(transform.position, false, true, true);
            Exit = new Node(transform.position, false, true, true);
            ExtractParts();
            foreach (Area area in areas)
                area.Apply();
            foreach (Access access in accesses)
                access.ConnectToGraph();
        }

        private void OnDrawGizmosSelected()
        {
            if (ScreenCast.MouseTerrain.Cast(out RaycastHit hit))
            {
                //PathFinding.Result result = PathFinding.PathFind(Exit, hit.point, 5000, 10000, 1, true, out List<PathFinding.Point> path);
                PathFinding.Result result = PathFinding.PathFind(hit.point, Enter, 5000, 10000, 1, true, out List<PathFinding.Point> path);
                if (result == PathFinding.Result.Success)
                {
                    PathFinding.DrawPath(path, true);
                }
            }
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
