using HexClicker.Navigation;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Buildings
{
    public class Building : MonoBehaviour
    {
        private BuildingPart[] parts;
        private Area[] areas;
        public BuildingNode Enter { get; private set; }
        public BuildingNode Exit { get; private set; }

        private Access[] accesses;

        public void OnPlace()
        {
            Enter = new BuildingNode(this);
            Exit = new BuildingNode(this);
            ExtractParts();
            foreach (Area area in areas)
                area.ObstructArea();
            foreach (Access access in accesses)
                access.ConnectToGraph();
        }

        private void OnDrawGizmosSelected()
        {
            if (ScreenCast.MouseScene.Cast(out RaycastHit hit))
            {
                PathFinding.Result result;
                List<PathFinding.Point> path;

                BuildingPart bp = hit.collider.GetComponent<BuildingPart>();

                if (bp != null)
                    result = PathFinding.PathFind(Exit, bp.Parent.Enter, 5000, 1, out path);
                else
                    result = PathFinding.PathFind(Exit, hit.point, 5000, 1, out path);
                
                if (result == PathFinding.Result.Success)
                {
                    Gizmos.color = Color.green;
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
