using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonMap : MonoBehaviour
{
    public int seed;
    public int width;
    public int height;
    public int resolution;
    public float noiseScale;
    public float noisePerTileHeight;
    public Noise.NoiseSettings noiseSettings;
    public HexagonTile tilePrefab;
    public float SeedOffsetX { get; private set; }
    public float SeedOffsetY { get; private set; }

    private HexagonTile[,] tiles;
    private new Camera camera;

    public AnimationCurve tileHeightCurve;
    public AnimationCurve tileHeightFalloffCurve;
    public AnimationCurve noiseCurve;
    public Gradient heightColorGradient;
    public HexagonTile this[int x,int y] => x < 0 || y < 0 || x >= tiles.GetLength(0) || y >= tiles.GetLength(1) ? null : tiles[x, y];

    private static int[] NeighbourX =
    {
        1,
        0,
       -1,
       -1,
        0,
        1,
    };
    private static int[] NeighbourY =
    {
         0,
         1,
         1,
         0,
        -1,
        -1,
    };


    
    public static int Closest(Vector2 v1, Vector2 v2)
    {
        if (v1.y < v2.y)
            return -1;
        if (v1.y > v2.y)
            return 1;
        return 0;
    }

    public float SampleHeight(float x, float y, float hexX, float hexY, int tileX, int tileY)
    {
        Vector3 cubeCenter = new Vector3(tileX, tileY, -tileX - tileY);
        Vector3 cubePoint = new Vector3(hexX, hexY, -hexX - hexY);

        float xDelta = cubeCenter.x - cubePoint.x;
        float yDelta = cubeCenter.y - cubePoint.y;
        float zDelta = cubeCenter.z - cubePoint.z;

        /*
        if (Mathf.Abs(xDelta - yDelta) <= .5f &&
            Mathf.Abs(xDelta - zDelta) <= .5f &&
            Mathf.Abs(yDelta - zDelta) <= .5f
            //&&
            //Mathf.Abs(cubeCenter.y - cubePoint.y) < .25f && 
            //Mathf.Abs(cubeCenter.z - cubePoint.z) < .25f
            )
            {
            return SampleTileHeight(tileX, tileY);
        }
        else
        {
        */
            float noise = SampleNoise(x, y);
            float tileHeight = SampleTileHeight(tileX, tileY);
            Vector2 cartesian = new Vector2(x, y);
            Vector2 centerCartesian = HexToCartesian(tileX, tileY);
            float maxDistance = SQRT_3;
            float centerDistance = Vector2.Distance(cartesian, centerCartesian);

            //       float heightSum = tileHeight * (1f - Mathf.Clamp(centerDistance / maxDistance, 0, 1));
            float heightSum = tileHeight * tileHeightFalloffCurve.Evaluate((1f - Mathf.Clamp(centerDistance / maxDistance, 0, 1)));

        //List<Vector2> closest = new List<Vector2>();

        // closest.Add(new Vector2(tileHeight, centerDistance));

        for (int i = 0; i < 6; i++)
            {
                int nX = tileX + NeighbourX[i];
                int nY = tileY + NeighbourY[i];

                Vector2 neighbourCartesian = HexToCartesian(nX, nY);
                float distance = Vector2.Distance(cartesian, neighbourCartesian);

                float neighbourTileHeight = SampleTileHeight(nX, nY);
            //closest.Add(new Vector3(neighbourTileHeight, distance));
                heightSum = Mathf.Max(heightSum, neighbourTileHeight * tileHeightFalloffCurve.Evaluate((1f - Mathf.Clamp(distance / maxDistance, 0, 1))));

            //
            //          if (distance > SQRT_3)
            //                continue;

            //            heightSum += neighbourTileHeight * (1f - Mathf.Clamp(distance / maxDistance, 0, 1));
        }

            //closest.Sort(Closest);

            //for (int i = 0; i < 3; i++)
            //{
            //    heightSum = Mathf.Max(heightSum, closest[i].x * (1f - Mathf.Clamp(closest[i].y / maxDistance, 0, 1)));
           //}

            return heightSum + heightSum * (noise - .5f) * noisePerTileHeight + (noise - .5f) * noiseScale;
        //}

    }

    public float SampleTileHeight(int x, int y) => tileHeightCurve.Evaluate(Mathf.PerlinNoise(x + SeedOffsetX, y + SeedOffsetY));

    public float SampleNoise(float x, float y) => noiseCurve.Evaluate(Noise.Perlin(x + SeedOffsetX, y + SeedOffsetY, noiseSettings));


    public static readonly float SQRT_3 = Mathf.Sqrt(3);
    public static Vector2 HexToCartesian(float x, float y) => new Vector2(3f / 2f * x, SQRT_3 / 2f * x + SQRT_3 * y);
    public static Vector2 CartesianToHex(float x, float z) => new Vector2(2f / 3f * x, -1f / 3f * x + SQRT_3 / 3f * z);

    public Vector3 MousePointOnXZ0Plane { get; private set; }
    public Vector2 MouseHexagonOnXZ0Plane { get; private set; }
    public Vector2Int MouseHexagonTileOnXZ0Plane { get; private set; }

    public void Awake()
    {
        camera = Camera.main;
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        camera = Camera.main;
        tiles = new HexagonTile[width, height];

        Random.InitState(seed);
        SeedOffsetX = Random.value * 10000;
        SeedOffsetY = Random.value * 10000;

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                tiles[x, z] = Instantiate(tilePrefab, transform);


        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                tiles[x, z].Generate(this, x, z, true);
    }

    public HexagonTile[] GetNeighbours(int tileX, int tileY)
    {
        return new HexagonTile[]
        {
            this[tileX + 1, tileY + 0],
            this[tileX + 0, tileY + 1],
            this[tileX - 1, tileY + 1],
            this[tileX - 1, tileY + 0],
            this[tileX + 0, tileY - 1],
            this[tileX + 1, tileY - 1],
        };
    }

    public static Vector2 CubeToAxial(Vector3 cube) => new Vector2(cube.x, cube.z);
    public static Vector3 AxialToCube(Vector2 axial) => new Vector3(axial.x, axial.y, -axial.x - axial.y);
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
        Vector3Int cube = CubeRound(AxialToCube(hex));
        return new Vector2Int(cube.x, cube.y);
    }

    public void Update()
    {
        MousePointOnXZ0Plane = XZPlane.ScreenPointXZ0PlaneIntersection(camera, Input.mousePosition);
        MouseHexagonOnXZ0Plane = CartesianToHex(MousePointOnXZ0Plane.x, MousePointOnXZ0Plane.z);
        MouseHexagonTileOnXZ0Plane = HexRound(MouseHexagonOnXZ0Plane);
    }

    public void OnDrawGizmos()
    {
        DrawHexagon(MouseHexagonTileOnXZ0Plane.x, MouseHexagonTileOnXZ0Plane.y);
    }

    /*
    public void OnDrawGizmos()
    {
        DrawHexagon(x, y);

        Vector2 globalCartesian = HexToCartesian(x, y);
        int i = 1;
        for (int r = 0; r < rings; r++)
        {
            float radius = (1f + r) / rings;
            for (int e = 0; e < 6; e++)
            {
                float angle0 = Mathf.PI / 2f + Mathf.PI * 2f / 6f * e;
                float angle1 = Mathf.PI / 2f + Mathf.PI * 2f / 6f * (e + 1);

                float sinAngle0 = Mathf.Sin(angle0);
                float cosAngle0 = Mathf.Cos(angle0);
                float sinAngle1 = Mathf.Sin(angle1);
                float cosAngle1 = Mathf.Cos(angle1);

                Vector2 e0 = new Vector3(sinAngle0, cosAngle0) * radius;
                Vector2 e1 = new Vector3(sinAngle1, cosAngle1) * radius;

                for (int v = 0; v < r + 1; v++)
                {


                    Vector2 localCartesian = Vector2.Lerp(e0, e1, (float)v / (r + 1));
                    float height = SampleHeight(globalCartesian + localCartesian);

                    Gizmos.DrawSphere(new Vector3(localCartesian.x, height, localCartesian.y), .05f);

                    i++;
                }
            }
        }
    }
    */

    public static void DrawHexagon(float x, float y)
    {
        Vector2 cartesian = HexToCartesian(x, y);

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
}
