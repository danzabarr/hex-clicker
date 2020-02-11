using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HexTile : MonoBehaviour, PathFinding.INode
{
    public enum ElevationType
    {
        Water,
        Plain,
        Hill,
        Mountain
    }

    //Static convert a (float) height to one of the ElevationType types
    public static ElevationType GetType(float height)
    {
        if (height < 0) return ElevationType.Water;
        if (height < 1) return ElevationType.Plain;
       // if (height < .8f) return ElevationType.Hill;
        return ElevationType.Mountain;
    }

    //Static returns the pathing movement cost of an ElevationType
    public static int GetCost(ElevationType type)
    {
        switch (type)
        {
            case ElevationType.Water:
                return 0;
            case ElevationType.Plain:
                return 0;
            case ElevationType.Hill:
                return 1;
            case ElevationType.Mountain:
                return 100;
            default:
                return 0;
        }
    }

    [SerializeField]
    private MeshFilter meshFilter;
    [SerializeField]
    private MeshRenderer meshRenderer;
    [SerializeField]
    private MeshCollider meshCollider;
    [SerializeField]
    private Material border;
    public Mesh Mesh => meshFilter.sharedMesh;
    public Vector2Int Position { get; private set; }
    public float Elevation { get; private set; }
    public ElevationType Type { get; set; }
    public int TreesCount { get; set; }
    public float Temperature { get; private set; }
    public int RegionID { get; set; }
    public int ContigRegionID { get; set; }
    public bool showTileBorder;

    #region Helper Fields
    /*
        ----------!!-ATTENTION-!!----------
        These global helper field values are assumed to be zero/false within local scope!
        They are used for tagging tiles as 'visited' or 'contained' in a list and in various other functions instead of a list.Contains(tile) call.
        If you change them, you must set them to zero/false after use!
    */

    [HideInInspector]
    public bool[] edgesVisited = new bool[6];
    [HideInInspector]
    public bool inFloodFillSet;
    [HideInInspector]
    public int state;
    #endregion

    public void Update()
    {
        if (showTileBorder)
            Graphics.DrawMesh(Mesh, transform.position, Quaternion.identity, border, LayerMask.NameToLayer("Grid"), null, 0, null, false, false);
    }

    public override string ToString() => "HexTile " + Position + "\n Type: " + Type + "\n Altitude: " + string.Format("{0:0.00}", Elevation) + "\n Temperature: " + string.Format("{0:0.00}", Temperature) + "\n Trees: " + TreesCount; 

    //Generate the mesh for this tile.
    public void GenerateMesh(HexMap map, int x, int y, bool fixNormalsAtSeams)
    {
        //Set position
        Position = new Vector2Int(x, y);
        Vector2 centerCartesian = HexUtils.HexToCartesian(x, y);
        transform.position = new Vector3(centerCartesian.x, 0, centerCartesian.y);

        //Sample the elevation for the tile
        Elevation = map.SampleElevation(x, y);
        
        //Store the ElevationType for the elevation
        Type = GetType(Elevation);

        //Store the temperature for the tile
        Temperature = map.SampleTemperature(x, Elevation, y);

        int res = map.Resolution;

        //Setting up the mesh arrays
        //uv is just global XZ coordinates
        //uv2 are uvs which can be used for texturing the tiles in a radial pattern, i.e. around and from the center of the tile to the edges. Custom shader required to make use of these uvs
        int verticesCount = 3 * (res + 1) * res + 1;
        int trianglesCount = 6 * res * res * 3;
        Vector3[] vertices = new Vector3[verticesCount];
        Vector2[] uv = new Vector2[verticesCount];
        Vector2[] uv2 = new Vector2[verticesCount];
        int[] triangles = new int[trianglesCount];

        //Calculate the center point in the mesh
        float centerHeight = map.SampleHeight(centerCartesian.x, centerCartesian.y, x, y);
        vertices[0] = new Vector3(0, centerHeight, 0);
        uv[0] = centerCartesian;
        uv2[0] = Vector2.zero;

        //See https://en.wikipedia.org/wiki/Centered_hexagonal_number
        int CenteredHexagonalNum(int n) => 3 * n * (n - 1) + 1;

        //i is the index of the vertex array
        int i = 1;
        //j is a number used for generating the triangle indices
        int j = 0;

        //For each hexagon ring, starting from the inside working out
        for (int r = 0; r < res; r++)
        {
            float radius = (1f + r) / res;
            int trianglesPerEdge = (r * 2) + 1;
            int k = 1;

            //For each of the six edges
            for (int e = 0; e < 6; e++)
            {
                //The XZ start and end point of each edge
                Vector2 e0 = new Vector2(HexUtils.sinAngles[e], HexUtils.cosAngles[e]) * radius;
                Vector2 e1 = new Vector2(HexUtils.sinAngles[(e + 1) % 6], HexUtils.cosAngles[(e + 1) % 6]) * radius;

                //For each subdivision of the ring edge
                for (int v = 0; v < r + 1; v++)
                {
                    //Interpolate between the start and the end of the edge to find the XZ point for this subdivision
                    Vector2 localCartesian = Vector2.Lerp(e0, e1, (float)v / (r + 1));
                    //Convert to global by adding the center position
                    Vector2 globalCartesian = centerCartesian + localCartesian;
                    //Sample the height for this vertex
                    float height = map.SampleHeight(globalCartesian.x, globalCartesian.y, x, y);
                    //Get the angle and construct the radial coordinates used for the uv2 of this vertex
                    float angle = Mathf.Atan2(localCartesian.x, localCartesian.y);
                    float radialX = (angle + Mathf.PI) / (Mathf.PI * 2);
                    float radialY = radius;

                    //Fill the arrays for this vertex and increment i
                    vertices[i] = new Vector3(localCartesian.x, height, localCartesian.y);
                    uv[i] = centerCartesian + localCartesian;
                    uv2[i] = new Vector2(radialX, radialY);
                    i++;
                }


                //For each triangle of the ring edge
                for (int t = 0; t < trianglesPerEdge; t++)
                {
                    //The inside of this loop is pure number fuckery to get the correct indices for the vertices that form the triangle for this ring edge.
                    //I just wrote down the pattern for a few rings and figured out some equations by trial and error that would procedurally generate the same pattern.
                    //i0, i1, i2 are the three indices. It is a clockwise winding order.
                    int i0 = CenteredHexagonalNum(r + 1) + e * (trianglesPerEdge / 2 + 1) + (t + 1) / 2;
                    int i1;
                    int i2 = 0;
                    
                    if (r > 0)
                        i2 = CenteredHexagonalNum(r) + (e * (trianglesPerEdge / 2) + (t) / 2) % (CenteredHexagonalNum(r + 1) - CenteredHexagonalNum(r));
                    if (r == 0)
                        i1 = (e + 1) % 6 + 1;
                    else if (t % 2 == 0)
                        i1 = CenteredHexagonalNum(r + 1) + (e * (trianglesPerEdge / 2 + 1) + (t + 1) / 2 + 1) % (CenteredHexagonalNum(r + 2) - CenteredHexagonalNum(r + 1));
                    else
                    {
                        i1 = CenteredHexagonalNum(r) + k % (CenteredHexagonalNum(r + 1) - CenteredHexagonalNum(r));
                        k++;
                    }

                    //Fill the triangles array with the correct indices and increment j
                    triangles[j * 3 + 0] = i0;
                    triangles[j * 3 + 1] = i1;
                    triangles[j * 3 + 2] = i2;
                    j++;
                }
            }
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices,
            uv = uv,
            uv2 = uv2,
            triangles = triangles
        };

        mesh.RecalculateNormals();

        if (fixNormalsAtSeams)
        {
            Vector3 CalculateVectorNormal(Vector3 centerPoint, float radius)
            {
                /*
                * Calculates a vector normal for a vertex at a given position, used for 'smooth shading'.
                * Samples six points equally spaced around the vertex at a given radius and calculates the surface normal of the six equilateral triangles that are formed.
                * Takes the average of the six surface normals to return as the vector normal.
                * This function is used to correct the normals at the edges of the tile mesh, which when automatically calculated using mesh.RecalculateNormals(), does not take into account the geometry of adjacent meshes, and produces 'creases' at the tile edges.
                * Toggle fixNormalsAtSeams to see this in effect.
                */
                Vector3[] radialPoints = new Vector3[6];

                for (int p = 0; p < 6; p++)
                {
                    float pX = centerPoint.x + HexUtils.sinAngles[p] * radius;
                    float pZ = centerPoint.z + HexUtils.cosAngles[p] * radius;
                    radialPoints[p] = new Vector3(pX, map.SampleHeight(pX, pZ, x, y), pZ);
                }

                Vector3 surfaceNormalSum = Vector3.zero;

                for (int p = 0; p < 6; p++)
                    surfaceNormalSum += CalculateSurfaceNormal(centerPoint, radialPoints[p], radialPoints[(p + 1) % 6]);

                //Can avoid a sqrt here as CalculateSurfaceNormal returns a unit vector and dividing by six works fine
                //return surfaceNormalSum.normalized;
                return surfaceNormalSum / 6;
            }

            Vector3[] normals = mesh.normals;

            //Loops through all the normals in the array that need correcting.
            //Starting with the index which is the centered hexagonal number one smaller than the tile (which is the resolution)
            //And looping through all the points on all six edges 
            i = CenteredHexagonalNum(res);
            for (int e = 0; e < res * 6; e++)
            {
                //Conveniently use the local vertex position stored in the array, and add the transform.position for global position
                //The radius to sample at is 1 / resolution, which replicates the positions of the six surrounding mesh vertices, whether they exist in this mesh or not
                normals[i] = CalculateVectorNormal(transform.position + vertices[i], 1f / res);
                i++;
            }

            mesh.normals = normals;
        }

        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        meshFilter.sharedMesh = mesh;
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


    #region PathFinding
    public float Cost => GetCost(Type);
    public PathFinding.INode PathParent { get; set; }
    public float PathDistance { get; set; }
    public float PathCrowFliesDistance { get; set; }
    public float PathCost { get; set; }
    public int PathSteps { get; set; }
    public int PathTurns { get; set; }
    public int PathEndDirection { get; set; }
    public bool Accessible => Type != ElevationType.Water;
    public int NeighboursCount => 6;
    public HexTile[] Neighbours { get; set; }
    public PathFinding.INode Neighbour(int neighbourIndex) => Neighbours[neighbourIndex];
    public float NeighbourDistance(int neighbourIndex) => 1;
    public float NeighbourCost(int neighbourIndex) => Neighbours[neighbourIndex].Cost;
    public bool NeighbourAccessible(int neighbourIndex) => Neighbours[neighbourIndex].Accessible;
    public float Distance(PathFinding.INode node) => Vector2.Distance(Position, (node as HexTile).Position);

    #endregion
}
