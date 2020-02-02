using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexMap : MonoBehaviour
{
    private new Camera camera;

    private HexTile[,] tiles;
    private HexTile select;
    private PathFinding.Path<HexTile> testPath;

    [SerializeField]
    private HexTile tilePrefab;

    [Header("Map Settings")]
    [SerializeField]
    private int seed;
    public float SeedOffsetX { get; private set; }
    public float SeedOffsetY { get; private set; }
    
    [SerializeField]
    private int width;
    [SerializeField]
    private int height;
    [SerializeField]
    private int resolution;
    public int Width => width;
    public int Height => height;
    public int Resolution => resolution;

    [Header("Tile Elevation")]
    [SerializeField]
    private float tileHeightFrequency;
    [SerializeField]
    private AnimationCurve tileHeightCurve;
    [SerializeField]
    private AnimationCurve tileHeightFalloffCurve;

    [Header("Terrain Noise")]
    [SerializeField]
    private float noiseScale;
    [SerializeField]
    private float noisePerTileHeight;
    [SerializeField]
    private Noise.NoiseSettings noiseSettings;
    [SerializeField]
    private AnimationCurve noiseCurve;
    [SerializeField]
    private AnimationCurve noisePerTileHeightCurve;

    [Header("Trees")]
    [SerializeField]
    private Material treeMaterial;
    [SerializeField]
    private Mesh treeMesh;
    [SerializeField]
    private float treesFrequency;
    [SerializeField]
    [Range(0,1)]
    private float treesThreshold;
    [SerializeField]
    [Range(-1, 1)]
    private float treesMinimumTemperature;
    [SerializeField]
    [Range(-1, 1)]
    private float treesMaximumTemperature;
    [SerializeField]
    private float treesMinimumAltitude;
    [SerializeField]
    private float treesMaximumAltitude;

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
    [SerializeField]
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

    public HexTile this[int x, int y]
    {
        get
        {
            x += width / 2;
            y += height / 2;
            return x < 0 || y < 0 || x >= tiles.GetLength(0) || y >= tiles.GetLength(1) ? null : tiles[x, y];
        }
    }

    public HexTile[] GetNeighbours(int tileX, int tileY)
    {
        return new HexTile[]
        {
            this[tileX + 1, tileY + 0],
            this[tileX + 0, tileY + 1],
            this[tileX - 1, tileY + 1],
            this[tileX - 1, tileY + 0],
            this[tileX + 0, tileY - 1],
            this[tileX + 1, tileY - 1],
        };
    }
    public Vector3 MousePointOnXZ0Plane { get; private set; } = default;
    public Vector2 MouseHexagonOnXZ0Plane { get; private set; } = default;
    public Vector2Int MouseHexagonTileOnXZ0Plane { get; private set; } = default;

    public float SampleHeight(float x, float z, int tileX, int tileY)
    {
        float noise = SampleNoise(x, z);
        float tileHeight = SampleElevation(tileX, tileY);
        Vector2 cartesian = new Vector2(x, z);
        Vector2 centerCartesian = HexUtils.HexToCartesian(tileX, tileY);
        float maxDistance = HexUtils.SQRT_3;
        float centerDistance = Vector2.Distance(cartesian, centerCartesian);
        float heightSum = tileHeight * tileHeightFalloffCurve.Evaluate((1f - Mathf.Clamp(centerDistance / maxDistance, 0, 1)));

        for (int i = 0; i < 6; i++)
        {
            int nX = tileX + HexUtils.neighbourX[i];
            int nY = tileY + HexUtils.neighbourY[i];

            Vector2 neighbourCartesian = HexUtils.HexToCartesian(nX, nY);
            float distance = Vector2.Distance(cartesian, neighbourCartesian);
            float neighbourTileHeight = SampleElevation(nX, nY);

            heightSum += neighbourTileHeight * tileHeightFalloffCurve.Evaluate((1f - Mathf.Clamp(distance / maxDistance, 0, 1)));
        }

        return heightSum + noisePerTileHeightCurve.Evaluate(heightSum) * (noise - .25f) + noiseScale * (noise - .25f);
    }

    public float SampleHeight(float x, float z)
    {
        Vector2Int tile = HexUtils.HexRound(HexUtils.CartesianToHex(x, z));
        return SampleHeight(x, z, tile.x, tile.y);
    }

    public Vector3 ToTerrain(Vector3 p) => new Vector3(p.x, SampleHeight(p.x, p.z), p.z);
    public float SampleElevation(int x, int y) => tileHeightCurve.Evaluate((Mathf.PerlinNoise((x + y + SeedOffsetX) * tileHeightFrequency, (y - x + SeedOffsetY) * tileHeightFrequency))) * Mathf.Clamp(1f - Mathf.Abs(y - 20) / 60f, 0.1f, 1) * 1.25f + Mathf.Clamp(y / 50f - 0.2f, 0, .6f);
    public float SampleNoise(float x, float z) => noiseCurve.Evaluate(Noise.Perlin(x + SeedOffsetX, z + SeedOffsetY, noiseSettings));
    public float SampleTree(float x, float z) => (Mathf.PerlinNoise((x + z + SeedOffsetX + 10000) * treesFrequency, (z - x + SeedOffsetY + 10000) * treesFrequency));
    public float SampleTemperature(float x, float z) => SampleTemperature(x, SampleHeight(x, z), z);
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


    public void Awake()
    {
        camera = Camera.main;
        Generate();
    }

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

    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        tiles = null;
        SeedOffsetX = 0;
        SeedOffsetY = 0;
    }

    [ContextMenu("Generate")]
    public void Generate()
    {
        Clear();

        camera = Camera.main;
        tiles = new HexTile[width, height];

        Random.InitState(seed);
        SeedOffsetX = Random.value * 10000;
        SeedOffsetY = Random.value * 10000;

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {

                Vector2 cartesian = HexUtils.HexToCartesian(x - width / 2, z - height / 2);

                if (cartesian.x > width / 2 || cartesian.x < -width / 2)
                    continue;
                if (cartesian.y > height / 2 || cartesian.y < -height / 2)
                    continue;

                tiles[x, z] = Instantiate(tilePrefab, transform);
            }


        treesRenderer = new InstancedRenderer(new MaterialPropertyBlock());

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                if (tiles[x, z] == null)
                    continue;
                tiles[x, z].GenerateMesh(this, x - width / 2, z - height / 2, true);
                SetupTrees(tiles[x, z]);
            }

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                if (tiles[x, z] == null)
                    continue;
                tiles[x, z].Neighbours = GetNeighbours(x - width / 2, z - height / 2);
            }
    }

    public void Update()
    {
        HexTile oldStart = select;
        HexTile oldEnd = this[MouseHexagonTileOnXZ0Plane.x, MouseHexagonTileOnXZ0Plane.y];

        if (XZPlane.ScreenPointXZ0PlaneIntersection(camera, Input.mousePosition, out Vector3 intersection))
            MousePointOnXZ0Plane = intersection;

        MouseHexagonOnXZ0Plane = HexUtils.CartesianToHex(MousePointOnXZ0Plane.x, MousePointOnXZ0Plane.z);
        MouseHexagonTileOnXZ0Plane = HexUtils.HexRound(MouseHexagonOnXZ0Plane);

        if (Input.GetMouseButtonDown(0))
        {
            if (select != null)
            {
                select.ShowBorder(false);
                select = null;
            }
            if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")))
            {
                HexTile mouse = hitInfo.collider.GetComponent<HexTile>();
                if (mouse)
                {
                    mouse.ShowBorder(true);
                    select = mouse;
                    Debug.Log(select);
                }
            }
        }

        HexTile start = select;
        HexTile end = this[MouseHexagonTileOnXZ0Plane.x, MouseHexagonTileOnXZ0Plane.y];

        if (start != oldStart || end != oldEnd)
        {
            PathFinding.PathFind(start, end, 1000, 500, PathFinding.StandardCostFunction, out testPath);
        }

        //DrawInstances();
        treesRenderer.Draw(treeMesh, treeMaterial);
    }

    public void OnDrawGizmos()
    {
        DrawHexagon(MouseHexagonTileOnXZ0Plane.x, MouseHexagonTileOnXZ0Plane.y);

        if (testPath != null && testPath.FoundPath)
        {
            for (int i = 0; i < testPath.Nodes.Count - 1; i++)
            {
                HexTile i0 = testPath[i];
                HexTile i1 = testPath[i + 1];
                Gizmos.DrawLine(i0.transform.position, i1.transform.position);
            }
        }
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

    #region Trees

    private InstancedRenderer treesRenderer;

    private void SetupTrees(HexTile tile)
    {
        // Vector4[] colors = new Vector4[population];

        foreach (Vector3 vertex in tile.Mesh.vertices)
        {
            Vector3 position = tile.transform.position + vertex;

            if (position.y < treesMinimumAltitude)
                continue;

            if (position.y > treesMaximumAltitude)
                continue;

            float treeSample = SampleTree(position.x, position.z);

            if (treeSample < treesThreshold)
                continue;

            float temperatureSample = SampleTemperature(position.x, position.y, position.z);

            if (temperatureSample > treesMaximumTemperature)
                continue;

            if (temperatureSample < treesMinimumTemperature)
                continue;

            Quaternion rotation = Quaternion.Euler(0, Random.Range(-180, 180), 0);
            Vector3 scale = new Vector3(1, Random.Range(1f, 1.25f), 1) * Random.Range(.125f, .5f) * treeSample * 1.5f;

            tile.TreesCount++;

            treesRenderer.Add(Matrix4x4.TRS(position, rotation, scale));

            // colors[i] = Color.Lerp(Color.red, Color.blue, Random.value);
        }

        //block.SetVectorArray("_Colors", colors);
    }

    /* 
     * 
     * DrawMeshInstancedIndirect trees
     * 
     * 
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

    #endregion

}
