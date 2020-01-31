using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonTile : MonoBehaviour
{
    [SerializeField]
    private MeshFilter meshFilter;
    [SerializeField]
    private MeshRenderer meshRenderer;
    [SerializeField]
    private MeshCollider meshCollider;
    [SerializeField]
    private Material border;

    private static Material[] materials, borderShown;
    public Mesh Mesh => meshFilter.mesh;
    
    public int X { get; private set; }
    public int Y { get; private set; }
    public float TileHeight { get; private set; }
    public int treesCount;
    public float Temperature { get; private set; }
    public HexagonMap.TileHeight TileHeightType;

    public void Awake()
    {
        if (materials == null)
        {
            materials = meshRenderer.sharedMaterials;
            borderShown = new Material[]
            {
                materials[0],
                materials[1],
                border
            };
        }
    }

    public override string ToString() => "HexagonTile [" + X + "," + Y + "]\n Type: " + TileHeightType + "\n Altitude: " + string.Format("{0:0.00}", TileHeight) + "\n Temperature: " + string.Format("{0:0.00}", Temperature) + "\n Trees: " + treesCount; 

    public void ShowBorder(bool show) => meshRenderer.sharedMaterials = show ? borderShown : materials;

    public void Generate(HexagonMap map, int x, int y, bool fixNormalsAtSeams)
    {
        X = x;
        Y = y;

        Vector2 cartesianPosition = HexagonMap.HexToCartesian(x, y);
        transform.position = new Vector3(cartesianPosition.x, 0, cartesianPosition.y);
        TileHeight = map.SampleTileHeight(x, y);
        TileHeightType = HexagonMap.TileHeightType(TileHeight);
        Temperature = map.SampleTemperature(x, TileHeight, y);

        int res = map.resolution;

        Vector3 CalculateVectorNormal(float vX, float vY, float vZ, float radius)
        {
            Vector3 centerPoint = new Vector3(vX, vY, vZ);
            Vector3[] radialPoints = new Vector3[6];

            for (int p = 0; p < 6; p++)
            {
                float angle = Mathf.PI / 2f + Mathf.PI * 2f / 6f * p;
                float pX = vX + Mathf.Sin(angle) * radius;
                float pZ = vZ + Mathf.Cos(angle) * radius;
                Vector2 pHex = HexagonMap.CartesianToHex(pX, pZ);
                radialPoints[p] = new Vector3(pX, map.SampleHeight(pX, pZ, pHex.x, pHex.y, x, y), pZ);
            }

            Vector3 surfaceNormalSum = Vector3.zero;

            for (int p = 0; p < 6; p++)
                surfaceNormalSum += CalculateSurfaceNormal(centerPoint, radialPoints[p], radialPoints[(p + 1) % 6]);

            //return surfaceNormalSum.normalized;
            surfaceNormalSum /= 6;

            return surfaceNormalSum;
        }

        Vector2 centerCartesian = HexagonMap.HexToCartesian(x, y);

        int verticesCount = 3 * (res + 1) * res + 1;
        int trianglesCount = 6 * res * res * 3;

        Vector3[] vertices = new Vector3[verticesCount];
        //Vector3[] normals = new Vector3[verticesCount];
        Vector2[] uv = new Vector2[verticesCount];
        Vector2[] uv2 = new Vector2[verticesCount];

        int[] triangles = new int[trianglesCount];

        Mesh mesh = new Mesh();

        float centerHeight = map.SampleHeight(centerCartesian.x, centerCartesian.y, x, y, x, y);
        vertices[0] = new Vector3(0, centerHeight, 0);
        //if (fixNormalsAtSeams)
        //    normals[0] = CalculateVectorNormal(globalCartesian.x, centerHeight, globalCartesian.y, 1f / rings);
        uv[0] = centerCartesian;

        int CenteredHexagonalNum(int n) => 3 * n * (n - 1) + 1;

        int i = 1;
        int j = 0;

        for (int r = 0; r < res; r++)
        {
            float radius = (1f + r) / res;
            int trianglesPerEdge = (r * 2) + 1;
            int k = 1;

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
                    Vector2 globalCartesian = centerCartesian + localCartesian;
                    Vector2 globalHex = HexagonMap.CartesianToHex(globalCartesian.x, globalCartesian.y);

                    float height = map.SampleHeight(globalCartesian.x, globalCartesian.y, globalHex.x, globalHex.y, x, y);
                    float angle = Mathf.Atan2(localCartesian.x, localCartesian.y);
                    float radialX = (angle + Mathf.PI) / (Mathf.PI * 2);
                    float radialY = radius;

                    vertices[i] = new Vector3(localCartesian.x, height, localCartesian.y);
                    uv[i] = centerCartesian + localCartesian;
                    uv2[i] = new Vector2(radialX, radialY);
                    i++;
                }

                for (int t = 0; t < trianglesPerEdge; t++)
                {
                    int i0 = CenteredHexagonalNum(r + 1) + e * (trianglesPerEdge / 2 + 1) + (t + 1) / 2;
                    int i1;
                    int i2 = 0;
                    
                    if (r > 0)
                    {
                        i2 = CenteredHexagonalNum(r) + (e * (trianglesPerEdge / 2) + (t) / 2) % (CenteredHexagonalNum(r + 1) - CenteredHexagonalNum(r));
                    }
                    if (r == 0)
                    {
                        i1 = (e + 1) % 6 + 1;
                    }
                    else if (t % 2 == 0)
                    {
                        i1 = CenteredHexagonalNum(r + 1) + (e * (trianglesPerEdge / 2 + 1) + (t + 1) / 2 + 1) % (CenteredHexagonalNum(r + 2) - CenteredHexagonalNum(r + 1));
                    }
                    else
                    {
                        i1 = CenteredHexagonalNum(r) + k % (CenteredHexagonalNum(r + 1) - CenteredHexagonalNum(r));
                        k++;
                    }

                    triangles[j * 3 + 0] = i0;
                    triangles[j * 3 + 1] = i1;
                    triangles[j * 3 + 2] = i2;
                    j++;
                }
            }
        }

        mesh.vertices = vertices;
        //mesh.normals = normals;
        mesh.uv = uv;
        mesh.uv2 = uv2;
        mesh.triangles = triangles;

        //if (!fixNormalsAtSeams)
        mesh.RecalculateNormals();

        if (fixNormalsAtSeams)
        {
            Vector3[] normals = mesh.normals;

            i = CenteredHexagonalNum(res);
            for (int e = 0; e < 6; e++)
            {
                float angle0 = Mathf.PI / 2f + Mathf.PI * 2f / 6f * e;
                float angle1 = Mathf.PI / 2f + Mathf.PI * 2f / 6f * (e + 1);

                float sinAngle0 = Mathf.Sin(angle0);
                float cosAngle0 = Mathf.Cos(angle0);
                float sinAngle1 = Mathf.Sin(angle1);
                float cosAngle1 = Mathf.Cos(angle1);

                Vector2 e0 = new Vector3(sinAngle0, cosAngle0);
                Vector2 e1 = new Vector3(sinAngle1, cosAngle1);

                for (int v = 0; v < res; v++)
                {
                    Vector2 localCartesian = Vector2.Lerp(e0, e1, (float)v / (res));
                    Vector2 globalCartesian = centerCartesian + localCartesian;
                    Vector2 globalHex = HexagonMap.CartesianToHex(globalCartesian.x, globalCartesian.y);
                    float height = map.SampleHeight(globalCartesian.x, globalCartesian.y, globalHex.x, globalHex.y, x, y);

                    normals[i] = CalculateVectorNormal(globalCartesian.x, height, globalCartesian.y, 1f / res);
                    i++;
                }
            }

            mesh.normals = normals;
        }

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;

    }

    

    static Vector3 CalculateSurfaceNormal(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 v1 = Vector3.zero;             // Vector 1 (x,y,z) & Vector 2 (x,y,z)
        Vector3 v2 = Vector3.zero;
        Vector3 normal = Vector3.zero;

        // Finds The Vector Between 2 Points By Subtracting
        // The x,y,z Coordinates From One Point To Another.

        // Calculate The Vector From Point 2 To Point 1
        v1.x = p1.x - p2.x;
        v1.y = p1.y - p2.y;
        v1.z = p1.z - p2.z;
        // Calculate The vector From Point 3 To Point 2
        v2.x = p2.x - p3.x;
        v2.y = p2.y - p3.y;
        v2.z = p2.z - p3.z;

        // Compute The Cross Product To Give Us A Surface Normal
        normal.x = v1.y * v2.z - v1.z * v2.y;   // Cross Product For Y - Z
        normal.y = v1.z * v2.x - v1.x * v2.z;   // Cross Product For X - Z
        normal.z = v1.x * v2.y - v1.y * v2.x;   // Cross Product For X - Y

        normal.Normalize();

        return normal;
    }
}
