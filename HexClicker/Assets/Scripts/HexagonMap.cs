using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonMap : MonoBehaviour
{

    private HexagonTile[,] tiles;
    private HexagonTile select;
    private new Camera camera;


    public int seed;
    public int width;
    public int height;
    public int resolution;
    public HexagonTile tilePrefab;
    
    [Header("Tile Height")]
    public float tileHeightFrequency;
    public AnimationCurve tileHeightCurve;
    public AnimationCurve tileHeightFalloffCurve;

    [Header("Terrain Noise")]
    public float noiseScale;
    public float noisePerTileHeight;
    public Noise.NoiseSettings noiseSettings;
    public AnimationCurve noiseCurve;
    public AnimationCurve noisePerTileHeightCurve;

    [Header("Trees")]
    public Material treeMaterial;
    public Mesh treeMesh;
    public float treesFrequency;
    [Range(0,1)]
    public float treesThreshold;
    public AnimationCurve treesCurve;
    
    public float SeedOffsetX { get; private set; }
    public float SeedOffsetY { get; private set; }

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



    [Header("Temperature")]
    [SerializeField]
    [Range(-50, 50)]
    private float temperature;
    [SerializeField]
    private float latitudeScale;
    [SerializeField]
    private float altitudeTemperature;

    [Header("Water")]
    [SerializeField]
    private float waterLevel;
    [SerializeField]
    [Range(0, 1)]
    private float waterBlending;
    [SerializeField]
    private Color waterColor;

    [Header("Rain")]
    private float wetness;

    [Header("Snow")]
    [SerializeField]
    [Range(0, 1)]
    private float snowAmount;
    [SerializeField]
    [Range(0, 1)]
    private float snowBlending;
    [SerializeField]
    [Range(0, 1)]
    private float snowSlopeMax;
    [SerializeField]
    private float snowShininess;
    [SerializeField]
    private float snowSpecular;
    [SerializeField]
    private Color snowColor;

    public void OnValidate()
    {
        Shader.SetGlobalFloat("_LatitudeScale", latitudeScale);
        Shader.SetGlobalFloat("_Temperature", temperature);
        Shader.SetGlobalFloat("_AltitudeTemperature", altitudeTemperature);
        Shader.SetGlobalFloat("_Wetness", wetness);

        Shader.SetGlobalColor("_WaterColor", waterColor);
        Shader.SetGlobalFloat("_WaterLevel", waterLevel);
        Shader.SetGlobalFloat("_WaterBlending", waterBlending);

        Shader.SetGlobalColor("_SnowColor", snowColor);
        Shader.SetGlobalFloat("_SnowAmount", snowAmount);
        Shader.SetGlobalFloat("_SnowBlending", snowBlending);
        Shader.SetGlobalFloat("_SnowShininess", snowShininess);
        Shader.SetGlobalFloat("_SnowSpecular", snowSpecular);
        Shader.SetGlobalFloat("_SnowSlopeMax", snowSlopeMax);
    }


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

//            heightSum = Mathf.Max(heightSum, neighbourTileHeight * tileHeightFalloffCurve.Evaluate((1f - Mathf.Clamp(distance / maxDistance, 0, 1))));

            heightSum += neighbourTileHeight * tileHeightFalloffCurve.Evaluate((1f - Mathf.Clamp(distance / maxDistance, 0, 1)));

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

        return heightSum + noisePerTileHeightCurve.Evaluate(heightSum) * (noise - .25f) + noiseScale * (noise - .25f);
        //}

    }

    public float SampleTileHeight(int x, int y) => tileHeightCurve.Evaluate((Mathf.PerlinNoise((x + y + SeedOffsetX) * tileHeightFrequency, (y - x + SeedOffsetY) * tileHeightFrequency)));

    public float SampleNoise(float x, float y) => noiseCurve.Evaluate(Noise.Perlin(x + SeedOffsetX, y + SeedOffsetY, noiseSettings));

    public float SampleTree(float x, float y) => treesCurve.Evaluate((Mathf.PerlinNoise((x + y + SeedOffsetX + 10000) * treesFrequency, (y - x + SeedOffsetY + 10000) * treesFrequency)));

    public float SampleTemperature(float x, float z)
    {
        Vector2 hex = CartesianToHex(x, z);
        Vector2Int tile = HexRound(hex);
        float y = SampleHeight(x, z, hex.x, hex.y, tile.x, tile.y);
        return SampleTemperature(x, y, z);
    }

    public float SampleTemperature(float x, float y, float z)
    {
        float temperature = this.temperature / 50;
        temperature += (-z + latitudeScale / 2) / latitudeScale;
	    temperature -= y / altitudeTemperature;
        //if (input.worldPos.y < (_WaterLevel + 20)) temperature -= (1 - input.worldPos.y / (_WaterLevel + 20)) * (temperature - .75) / .75 * (1 - slopeAmount * 2);
        //temperature += diffuse * .1;
        temperature -= .125f;
        temperature = Mathf.Clamp(temperature, -1, 1);
        return temperature;
    }

    public static readonly float SQRT_3 = Mathf.Sqrt(3);
    public static Vector2 HexToCartesian(float x, float y) => new Vector2(3f / 2f * x, SQRT_3 / 2f * x + SQRT_3 * y);
    public static Vector2 CartesianToHex(float x, float z) => new Vector2(2f / 3f * x, -1f / 3f * x + SQRT_3 / 3f * z);

    public Vector3 MousePointOnXZ0Plane { get; private set; } = default;
    public Vector2 MouseHexagonOnXZ0Plane { get; private set; } = default;
    public Vector2Int MouseHexagonTileOnXZ0Plane { get; private set; } = default;

    public void Awake()
    {
        camera = Camera.main;
        Generate();
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
            {
                tiles[x, z].Generate(this, x, z, true);
                SetupTrees(tiles[x, z]);
            }
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

    public enum TileHeight
    {
        Water,
        Plain,
        Hill,
        Mountain
    }

    public static TileHeight TileHeightType(float height)
    {
        if (height < 0.0f) return TileHeight.Water;
        if (height < .25f) return TileHeight.Plain;
        if (height < .8f) return TileHeight.Hill;
        return TileHeight.Mountain;
    }

    public void Update()
    {
        if (XZPlane.ScreenPointXZ0PlaneIntersection(camera, Input.mousePosition, out Vector3 intersection))
            MousePointOnXZ0Plane = intersection;

        MouseHexagonOnXZ0Plane = CartesianToHex(MousePointOnXZ0Plane.x, MousePointOnXZ0Plane.z);
        MouseHexagonTileOnXZ0Plane = HexRound(MouseHexagonOnXZ0Plane);

        if (Input.GetMouseButtonDown(0))
        {
            if (select != null)
            {
                select.ShowBorder(false);
                select = null;
            }

            //HexagonTile mouse = this[MouseHexagonTileOnXZ0Plane.x, MouseHexagonTileOnXZ0Plane.y];
            if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")))
            {
                HexagonTile mouse = hitInfo.collider.GetComponent<HexagonTile>();
                if (mouse)
                {
                    mouse.ShowBorder(true);
                    select = mouse;
                    Debug.Log(TileHeightType(select.TileHeight));
                }
            }
        }


        //DrawInstances();
        DrawTrees();
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

    
    public class InstancedMeshBatch
    {
        public Matrix4x4[] Matrices { get; private set; } = new Matrix4x4[1023];
        public int Count { get; private set; }
        public Bounds Bounds { get; private set; }
        public bool Full => Count >= 1023;

        public void Add(Matrix4x4 matrix)
        {
            if (Count >= 1023)
                return;
            Matrices[Count] = matrix;
            Count++;
        }
        public void Draw(Mesh mesh, Material material, MaterialPropertyBlock block)
        {
            Graphics.DrawMeshInstanced(mesh, 0, material, Matrices, Count);
        }
    }

    private MaterialPropertyBlock treeMaterialPropertyBlock;
    private List<InstancedMeshBatch> treeBatches = new List<InstancedMeshBatch>();

    private void SetupTrees(HexagonTile tile)
    {

        if (treeMaterialPropertyBlock == null)
            treeMaterialPropertyBlock = new MaterialPropertyBlock();

        void Add(Matrix4x4 matrix)
        {
            if (treeBatches.Count < 1 || treeBatches[treeBatches.Count - 1].Full)
                treeBatches.Add(new InstancedMeshBatch());

            InstancedMeshBatch current = treeBatches[treeBatches.Count - 1];

            current.Add(matrix);
        }

        // Vector4[] colors = new Vector4[population];

        foreach (Vector3 vertex in tile.Mesh.vertices)
        {
            // Build matrix.
            Vector3 position = tile.transform.position + vertex;

            if (position.y < 0.05f)
                continue;

            if (position.y > .6f)
                continue;

            float treeSample = SampleTree(position.x, position.z);

            if (treeSample < treesThreshold)
                continue;

            float temperatureSample = SampleTemperature(position.x, position.y, position.z);

            if (temperatureSample > .3f)
                continue;
            if (temperatureSample < -.7f)
                continue;

            Quaternion rotation = Quaternion.Euler(0, Random.Range(-180, 180), 0);
            Vector3 scale = new Vector3(1, Random.Range(1f, 1.5f), 1) * Random.Range(.25f, .5f);

            Add(Matrix4x4.TRS(position, rotation, scale));

            // colors[i] = Color.Lerp(Color.red, Color.blue, Random.value);
        }

        // Custom shader needed to read these!!
        //block.SetVectorArray("_Colors", colors);
    }
    private void DrawTrees()
    {
        foreach (InstancedMeshBatch batch in treeBatches)
            batch.Draw(treeMesh, treeMaterial, treeMaterialPropertyBlock);
    }
    /*
    private void SetupTrees(HexagonTile tile)
    {
        foreach (Vector3 vertex in tile.Mesh.vertices)
        {
            // Build matrix.
            Vector4 position = tile.transform.position + vertex;

            float treeSample = SampleTree(position.x, position.z);

            if (treeSample < treesThreshold)
                continue;

            if (position.y < 0.05f)
                continue;

            if (position.y > .6f)
                continue;

            position.w = Random.Range(.25f, .5f);

            positions.Add(position);
        }
        instanceCount = positions.Count;
    }

    


    private int instanceCount = 0;
    public Mesh instanceMesh;
    public Material instanceMaterial;

    private int cachedInstanceCount = -1;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer argsBuffer;
    private List<Vector4> positions = new List<Vector4>(); 

    void DrawInstances()
    {

        // Update starting position buffer
        if (cachedInstanceCount != instanceCount)
            SetupInstances(positions);

        // Render
        instanceMaterial.SetBuffer("positionBuffer", positionBuffer);
        Graphics.DrawMeshInstancedIndirect(instanceMesh, 0, instanceMaterial, new Bounds(Vector3.zero, Vector3.one * 1000), argsBuffer);
    }

    void SetupInstances(List<Vector4> positions)
    {
        instanceCount = positions.Count;
        positionBuffer = new ComputeBuffer(instanceCount, 16);
        positionBuffer.SetData(positions);

        // indirect args
        uint numIndices = (instanceMesh != null) ? (uint)instanceMesh.GetIndexCount(0) : 0;
        uint[] args = new uint[5] { numIndices, (uint)instanceCount, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        cachedInstanceCount = instanceCount;
    }
    */

}
