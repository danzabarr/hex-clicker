using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Navigation
{
    public class Area : MonoBehaviour
    {
        public bool showHandles;
        private static readonly bool useXZ = true;
        public Color outline = Color.white;
        public Color fill = new Color(1, 1, 1, .2f);

        [SerializeField] private bool inaccessible;
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
            transformedPoints = Polygon2.TransformedPoints(points, transform, useXZ);
            transformedBounds = Polygon2.CalculateBounds(transformedPoints);
            fillPoints = Polygon2.ScanLineFill(transformedPoints, transformedBounds, voxelSize, voxelOffset).ToArray();
            outlinePoints = Polygon2.VoxelTraverseOutline(transformedPoints, voxelSize, voxelOffset, true).ToArray();
        }

        private void OnValidate()
        {
            Recalculate();
        }

        private void Start()
        {
            Apply();
        }

        private void OnDrawGizmos()
        {
            Recalculate();
            /*
            Gizmos.color = Color.white;
            Polygon.DrawPolygon(transformedPoints, useXZ);
            */
            Gizmos.color = Color.white;
            Polygon2.DrawPolygon(Polygon2.TransformedPoints3(points, transform, useXZ));

            Gizmos.color = Color.green;
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 voxelSize = Vector2.one * World.Map.TileSize / NavigationGraph.Resolution;
            Vector2 voxelOffset = Vector2.one * 0;
            World.Map map = World.Map.Instance;

            void DrawCube(Vector2Int p)
            {
                Vector3 pos = ((p + voxelOffset) * voxelSize);
                Vector3 size = voxelSize;

                if (useXZ)
                {
                    pos = pos.x0y();
                    size = size.xzy();
                }
                if (map)
                    pos = map.OnTerrain(pos);

                Gizmos.DrawCube(pos, size * .95f);
            }

            Gizmos.color = fill;
            foreach (Vector2Int p in fillPoints)
                DrawCube(p);

            Gizmos.color = outline;
            foreach (Vector2Int p in outlinePoints)
                DrawCube(p);

        }

        [ContextMenu("Apply")]
        public void Apply()
        {
            Recalculate();
            foreach (Vector2Int p in fillPoints)
                if (NavigationGraph.TryGetNode(p, out Node node))
                    node.Accessible = false;

            foreach (Vector2Int p in outlinePoints)
                if (NavigationGraph.TryGetNode(p, out Node node))
                    node.Accessible = false;
        }
    }
}
