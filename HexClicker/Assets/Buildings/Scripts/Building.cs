using HexClicker.Navigation;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Buildings
{
    public class Building : MonoBehaviour
    {
        private static Dictionary<System.Type, List<Building>> buildings = new Dictionary<System.Type, List<Building>>();

        private BuildingPart[] parts;
        private Area[] areas;
        public BuildingNode Enter { get; private set; }
        public BuildingNode Exit { get; private set; }

        private AccessPoint[] accessPoints;

        private WorkPoint[] constructionPoints;

        public static T[] GetAll<T>() where T : Building
        {
            if (buildings.TryGetValue(typeof(T), out List<Building> list))
                return Array.ConvertAll(list.ToArray(), item => (T)item);
            return new T[0];
        }

        public void OnPlace()
        {
            Enter = new BuildingNode(this);
            Exit = new BuildingNode(this);
            ExtractParts();
            foreach (AccessPoint access in accessPoints)
                access.ConnectToGraph();
            foreach (WorkPoint cp in constructionPoints)
                cp.ConnectToGraph();

            if (buildings.TryGetValue(GetType(), out List<Building> list))
                list.Add(this);
            else
                buildings.Add(GetType(), new List<Building>() { this });
        }

        public void OnRemove()
        {
            foreach (AccessPoint access in accessPoints)
                access.DisconnectFromGraph();
            foreach (WorkPoint cp in constructionPoints)
                cp.DisconnectFromGraph();

            if (buildings.TryGetValue(GetType(), out List<Building> list))
                list.Remove(this);
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
                return;

            if (ScreenCast.MouseScene.Cast(out RaycastHit hit))
            {
                PathFinding.Result result;
                List<PathFinding.Point> path;

                BuildingPart bp = hit.collider.GetComponent<BuildingPart>();

                if (bp != null)
                    result = PathFinding.PathFind(default, default, Exit, bp.Parent.Enter, null, null, null, false, false, 5000, 1, out path);
                else
                    result = PathFinding.PathFind(default, hit.point, Exit, null, null, null, null, false, false, 5000, 1, out path);
                
                if (result == PathFinding.Result.Success)
                {
                    Gizmos.color = Color.green;
                    PathFinding.DrawPath(path, true);
                }
            }
        }

        public void ExtractParts()
        {
            parts = GetComponentsInChildren<BuildingPart>();

            foreach (BuildingPart part in parts)
                part.SetupPlacingObjects();

            areas = GetComponentsInChildren<Area>();
            accessPoints = GetComponentsInChildren<AccessPoint>();
            constructionPoints = GetComponentsInChildren<WorkPoint>();
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
