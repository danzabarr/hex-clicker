using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NavigationArea : MonoBehaviour
{
    private static readonly bool useXZ = true;
    public Color outline = Color.white;
    public Color fill = new Color(1, 1, 1, .2f);

    [SerializeField] private bool inaccessible;
    [SerializeField] private Vector2[] points = new Vector2[0];
    private Vector2Int[] fillPoints, outlinePoints;
    private Vector2[] transformedPoints;
    private Bounds2 transformedBounds;

    public Bounds2 Bounds => transformedBounds;
    public Vector2Int[] FillPoints => fillPoints;
    public Vector2Int[] OutlinePoints => outlinePoints;

    void Recalculate()
    {
        Vector2 voxelSize = Vector2.one * HexMap.TileSize / Navigation.Resolution;
        Vector2 voxelOffset = Vector2.one * 0;
        transformedPoints = Polygon.TransformedPoints(points, transform, useXZ);
        transformedBounds = Polygon.CalculateBounds(transformedPoints);
        fillPoints = Polygon.ScanLineFill(transformedPoints, transformedBounds, voxelSize, voxelOffset).ToArray();
        outlinePoints = Polygon.VoxelTraverseOutline(transformedPoints, voxelSize, voxelOffset, true).ToArray();
    }

    void Awake()
    {
        Recalculate();
        if (inaccessible)
            MakeInaccessible();
    }

    void OnValidate()
    {
        Recalculate();
    }

    void OnDrawGizmos()
    {
        /*
        Gizmos.color = Color.white;
        Polygon.DrawPolygon(transformedPoints, useXZ);
        */
        Gizmos.color = Color.white;
        Polygon.DrawPolygon(Polygon.TransformedPoints3(points, transform, useXZ));
    }

    void OnDrawGizmosSelected()
    {
        Vector2 voxelSize = Vector2.one * HexMap.TileSize / Navigation.Resolution;
        Vector2 voxelOffset = Vector2.one * 0;
        HexMap map = HexMap.Instance;

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
        foreach(Vector2Int p in fillPoints)
            DrawCube(p);

        Gizmos.color = outline;
        foreach(Vector2Int p in outlinePoints)
            DrawCube(p);


    }

    [ContextMenu("Make Inaccessible")]
    public void MakeInaccessible()
    {
        foreach(Vector2Int p in fillPoints)
            if (Navigation.TryGetNode(p, out Navigation.Node node))
                node.Accessible = false;

        foreach (Vector2Int p in outlinePoints)
            if (Navigation.TryGetNode(p, out Navigation.Node node))
                node.Accessible = false;
    }
}
