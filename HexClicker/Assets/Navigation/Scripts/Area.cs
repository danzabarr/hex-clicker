using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class Area : MonoBehaviour
    {
        public bool showHandles;
        public static readonly Color outlineColor = new Color(1, 0, 0, .2f);
        public static readonly Color fillColor = new Color(1, 1, 1, .2f);

        private bool hasStarted;
        private bool hasObstructed;

        [SerializeField] private bool calculateOutline;
        [SerializeField] private bool calculateFill = true;
        [SerializeField] private Vector2[] points;

        private Vector2Int[] fillPoints, outlinePoints;
        private Vector2[] transformedPoints;
        private Bounds2 transformedBounds;

        public Bounds2 Bounds => transformedBounds;
        public Vector2Int[] FillPoints => fillPoints;
        public Vector2Int[] OutlinePoints => outlinePoints;

        private void Recalculate()
        {
            Vector2 voxelSize = Vector2.one * World.Map.TileSize / NavigationGraph.Resolution;
            Vector2 voxelOffset = Vector2.one * 0;
            transformedPoints = Polygon2.TransformedPoints(points, transform, true);
            transformedBounds = Polygon2.CalculateBounds(transformedPoints);
            if (calculateFill) fillPoints = Polygon2.ScanLineFill(transformedPoints, transformedBounds, voxelSize, voxelOffset).ToArray();
            if (calculateOutline) outlinePoints = Polygon2.VoxelTraverseOutline(transformedPoints, voxelSize, voxelOffset, true).ToArray();
        }

        public bool InsidePolygon(Vector3 point)
        {
            return Polygon2.PolygonContainsPoint(points, point.xz());
        }

        public bool InsideArea(Vector2Int point)
        {
            if (calculateFill)
                foreach (Vector2Int p in FillPoints)
                    if (point == p)
                        return true;
            if (calculateOutline)
                foreach (Vector2Int p in OutlinePoints)
                    if (point == p)
                        return true;
            return false;
        }

        private void OnValidate()
        {
            Recalculate();
        }

        private void Start()
        {
            if (!hasObstructed)
            {
                ObstructArea();
                hasObstructed = true;
            }
            hasStarted = true;
        }

        private void OnEnable()
        {
            if (hasStarted && !hasObstructed)
            {
                ObstructArea();
                hasObstructed = true;
            }
        }

        private void OnDisable()
        {
            if (hasObstructed)
            {
                RevertArea();
                hasObstructed = false;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Polygon2.DrawPolygon(Polygon2.TransformedPoints3(points, transform, true));
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 voxelSize = Vector2.one * World.Map.TileSize / NavigationGraph.Resolution;
            Vector2 voxelOffset = Vector2.one * 0;
            World.Map map = World.Map.Instance;

            void DrawCube(Vector2Int p)
            {
                Vector3 pos = ((p + voxelOffset) * voxelSize).x0y();
                Vector3 size = voxelSize.x0y();

                if (map)
                    pos = map.OnTerrain(pos);

                Gizmos.DrawCube(pos, size * .95f);
            }

            if (calculateFill && fillPoints != null)
            {
                Gizmos.color = fillColor;
                foreach (Vector2Int p in fillPoints)
                    DrawCube(p);
            }

            if (calculateOutline && outlinePoints != null)
            {
                Gizmos.color = outlineColor;
                foreach (Vector2Int p in outlinePoints)
                    DrawCube(p);
            }
        }

        public void ObstructArea()
        {
            Debug.Log(this + " " + transform.position);
            Recalculate();
            if (calculateFill)
            {
                foreach (Vector2Int p in fillPoints)
                    if (NavigationGraph.TryGetNode(p, out Node node))
                        node.Obstructions++;
            }
            if (calculateOutline)
            {
                foreach (Vector2Int p in outlinePoints)
                    if (NavigationGraph.TryGetNode(p, out Node node))
                        node.Obstructions++;
            }
        }

        public void RevertArea()
        {
            if (calculateFill)
            {
                if (fillPoints != null)
                foreach (Vector2Int p in fillPoints)
                    if (NavigationGraph.TryGetNode(p, out Node node))
                        node.Obstructions--;
            }
            if (calculateOutline)
            {
                if (outlinePoints != null)
                foreach (Vector2Int p in outlinePoints)
                    if (NavigationGraph.TryGetNode(p, out Node node))
                        node.Obstructions--;
            }
        }
    }
}
