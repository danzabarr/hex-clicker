using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HexUtils
{
    public static readonly float SQRT_3 = Mathf.Sqrt(3);

    public static readonly int[] neighbourX = { 1, 0, -1, -1, 0, 1, };
    public static readonly int[] neighbourY = { 0, 1, 1, 0, -1, -1, };

    public static readonly float[] angles =
    {
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 0,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 1,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 2,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 3,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 4,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 5,
    };

    public static readonly float[] sinAngles =
    {
        Mathf.Sin(angles[0]),
        Mathf.Sin(angles[1]),
        Mathf.Sin(angles[2]),
        Mathf.Sin(angles[3]),
        Mathf.Sin(angles[4]),
        Mathf.Sin(angles[5]),
    };

    public static readonly float[] cosAngles =
    {
        Mathf.Cos(angles[0]),
        Mathf.Cos(angles[1]),
        Mathf.Cos(angles[2]),
        Mathf.Cos(angles[3]),
        Mathf.Cos(angles[4]),
        Mathf.Cos(angles[5]),
    };

    public static Vector2 HexToCartesian(float x, float y) => new Vector2(3f / 2f * x, SQRT_3 / 2f * x + SQRT_3 * y);
    public static Vector2 HexToCartesian(Vector2 hex) => HexToCartesian(hex.x, hex.y);
    public static Vector2 CartesianToHex(float x, float z) => new Vector2(2f / 3f * x, -1f / 3f * x + SQRT_3 / 3f * z);
    public static Vector2 CubeToHex(Vector3 cube) => new Vector2(cube.x, cube.y);
    public static Vector2Int CubeToHex(Vector3Int cube) => new Vector2Int(cube.x, cube.y);
    public static Vector3 HexToCube(Vector2 hex) => new Vector3(hex.x, hex.y, -hex.x - hex.y);
    public static Vector3Int HexToCube(Vector2Int hex) => new Vector3Int(hex.x, hex.y, -hex.x - hex.y);
    public static Vector3Int CubeRound(Vector3 cube)
    {
        int rx = Mathf.RoundToInt(cube.x);
        int ry = Mathf.RoundToInt(cube.y);
        int rz = Mathf.RoundToInt(cube.z);

        float x_diff = Mathf.Abs(rx - cube.x);
        float y_diff = Mathf.Abs(ry - cube.y);
        float z_diff = Mathf.Abs(rz - cube.z);

        if (x_diff > y_diff && x_diff > z_diff)
            rx = -ry - rz;
        else if (y_diff > z_diff)
            ry = -rx - rz;
        else
            rz = -rx - ry;

        return new Vector3Int(rx, ry, rz);
    }

    public static Vector2Int HexRound(Vector2 hex)
    {
        Vector3Int cube = CubeRound(HexToCube(hex));
        return new Vector2Int(cube.x, cube.y);
    }

    public static void DrawHexagon(float x, float y)
    {
        Vector2 cartesian = HexUtils.HexToCartesian(x, y);

        for (int i = 0; i < 6; i++)
        {
            float angle0 = Mathf.PI / 2f + Mathf.PI * 2f / 6f * i;
            float angle1 = Mathf.PI / 2f + Mathf.PI * 2f / 6f * (i + 1);

            float sinAngle0 = Mathf.Sin(angle0);
            float cosAngle0 = Mathf.Cos(angle0);
            float sinAngle1 = Mathf.Sin(angle1);
            float cosAngle1 = Mathf.Cos(angle1);

            Vector3 i0 = new Vector3(cartesian.x + sinAngle0, 0, cartesian.y + cosAngle0);
            Vector3 i1 = new Vector3(cartesian.x + sinAngle1, 0, cartesian.y + cosAngle1);

            Gizmos.DrawLine(i0, i1);
        }
    }

    public static Mesh Mesh { get; } = CreateHexagonMesh();

    private static int IDCounter = 0;
    public static int NewContigRegionID
    {
        get
        {
            IDCounter++;
            return IDCounter;
        }
    }

    public static Mesh CreateHexagonMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[7];
        Vector2[] uv = new Vector2[7];
        int[] triangles = new int[18];

        vertices[0] = Vector3.zero;
        uv[0] = Vector3.zero;

        for (int i = 0; i < 6; i++)
        {
            vertices[1 + i] = new Vector3(sinAngles[i], 0, cosAngles[i]);
            uv[1 + i] = new Vector2((angles[i] + Mathf.PI) / (Mathf.PI * 2), 1);
            triangles[i * 3 + 0] = 0;
            triangles[i * 3 + 1] = 1 + i;
            triangles[i * 3 + 2] = 1 + (i + 1) % 6;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        return mesh;
    }

    public delegate bool Match(HexTile original, HexTile tile);
    public static bool SameType(HexTile original, HexTile tile) => original.Type == tile.Type;

    public static List<HexTile> RecursiveDepthFirstFloodFill(HexTile start, Match match)
    {
        List<HexTile> list = new List<HexTile>();

        if (start == null)
            return list;

        void Recursive(HexTile tile)
        {
            if (match(start, tile) && !tile.inFloodFillSet)
            {
                list.Add(tile);
                tile.inFloodFillSet = true;
            }
            else
                return;

            foreach (HexTile neighbour in tile.Neighbours)
                if (neighbour != null && match(start, neighbour))
                    Recursive(neighbour);
        }

        Recursive(start);


        foreach (HexTile tile in list)
            tile.inFloodFillSet = false;

        return list;
    }

    public static List<HexTile> BreadthFirstFloodFill(HexTile start, Match match)
    {
        List<HexTile> list = new List<HexTile>();

        if (!match(start, start))
            return list;

        Queue<HexTile> frontier = new Queue<HexTile>();
        frontier.Enqueue(start);
        start.inFloodFillSet = true;

        while (frontier.Count > 0)
        {
            HexTile tile = frontier.Dequeue();
            list.Add(tile);
            foreach (HexTile neighbour in tile.Neighbours)
            {
                if (neighbour == null)
                    continue;

                if (!match(start, neighbour))
                    continue;

                if (neighbour.inFloodFillSet)
                    continue;

                neighbour.inFloodFillSet = true;
                frontier.Enqueue(neighbour);
            }
        }

        foreach (HexTile tile in list)
            tile.inFloodFillSet = false;

        return list;
    }

    public static bool State1(HexTile start, HexTile tile) => tile.state == 1;
    public static bool IsRegionContiguous(List<HexTile> tiles)
    {
        if (tiles.Count == 0)
            return true;

        foreach (HexTile tile in tiles)
            tile.state = 1;

        List<HexTile> floodFill = BreadthFirstFloodFill(tiles[0], State1);

        foreach (HexTile tile in tiles)
            tile.state = 0;

        return floodFill.Count == tiles.Count;
    }

    public static List<List<HexTile>> IdentifyIslands(List<HexTile> tiles)
    {
        List<List<HexTile>> islands = new List<List<HexTile>>();

        foreach (HexTile tile in tiles)
            tile.state = 1;

        foreach (HexTile tile in tiles)
        {
            if (tile.state != 1)
                continue;

            List<HexTile> island = BreadthFirstFloodFill(tile, State1);
            if (island.Count > 0)
            {
                foreach (HexTile member in island)
                    member.state = 0;
                islands.Add(island);
            }
        }

        foreach (HexTile tile in tiles)
            tile.state = 0;

        return islands;
    }
}
