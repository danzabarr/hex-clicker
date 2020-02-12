using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class HexMap : MonoBehaviour, IEnumerable<HexTile>
{
    public static HexMap Instance { get; private set; }

    private new Camera camera;
    private HexTile[] tiles;
    private HexTile select;
    private PathFinding.Path<HexTile> testPath;
    private Dictionary<int, HexRegion> regions;

    [Header("Map Settings")]
    [SerializeField]
    private HexTile tilePrefab;
    [SerializeField]
    private int seed;
    [SerializeField]
    private int width;
    [SerializeField]
    private int height;
    [SerializeField]
    private int resolution;
    public float SeedOffsetX { get; private set; }
    public float SeedOffsetY { get; private set; }
    public int Width => width;
    public int Height => height;
    public int Resolution => resolution;
    public int TileCount => width * height;

    [Header("Tile Elevation")]
    [SerializeField]
    private float tileHeightFrequency;
    [SerializeField]
    private AnimationCurve tileHeightCurve;
    [SerializeField]
    private AnimationCurve tileHeightFalloffCurve;

    [Header("Terrain Noise")]
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
    [Range(-5, 5)]
    private float treesMinimumTemperature;
    [SerializeField]
    [Range(-5, 5)]
    private float treesMaximumTemperature;
    [SerializeField]
    private float treesMinimumAltitude;
    [SerializeField]
    private float treesMaximumAltitude;
    [SerializeField]
    private Gradient treesColor;
    [SerializeField]
    private float treeMinimumHeight, treeMaximumHeight;
    [SerializeField]
    private float treeMinimumScale, treeMaximumScale;

    private InstancedRenderer treesRenderer;

    [Header("Grass")]
    [SerializeField]
    private Material grassMaterial;
    [SerializeField]
    private bool grassShadowCasting;

    [Header("Temperature")]
    [SerializeField]
    [Range(-10, 10)]
    private float temperature;
    [SerializeField]
    private float latitudeScale;
    [SerializeField]
    private float altitudeTemperature;

    [Header("Water")]
    [SerializeField]
    private Transform water;
    [SerializeField]
    private float waterLevel = 0f;
    [SerializeField]
    [Range(0, 1)]
    private float waterBlending = .1f;
    [SerializeField]
    private Color waterColor = Color.blue;

    [Header("Snow")]
    [SerializeField]
    [Range(0, 1)]
    private float snowBlending = 0.1f;
    [SerializeField]
    [Range(0, 1)]
    private float snowSlopeMax = .5f;
    [SerializeField]
    private float snowShininess = 1;
    [SerializeField]
    private float snowSpecular = 1;
    [SerializeField]
    private Color snowColor = Color.white;
    [SerializeField]
    private float snowIntensity = 1;

    [Header("Regions")]
    [SerializeField]
    private Material[] regionMaterials;
    [SerializeField]
    private int regionPlacing = 1;

    /// <summary>
    /// Returns the tile from the array at the supplied coordinates, or null if out of range.
    /// </summary>
    public HexTile this[int x, int y]
    {
        get
        {
            int storeX = x + width / 2;
            int storeY = y + height / 2 + storeX / 2 - width / 4;
            return storeX < 0 || storeY < 0 || storeX >= width || storeY >= height ? null : tiles[storeX + storeY * width];
        }
    }

    /// <summary>
    /// Iterator for the tile array.
    /// </summary>
    public IEnumerator<HexTile> GetEnumerator() => ((IEnumerable<HexTile>)tiles).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public HexTile this[int index] => tiles[index];
    public int Length => tiles.Length;

    /// <summary>
    /// Returns an array containing the the six neighbouring tiles (or null if the neighbouring tile is out of range) around the supplied coordinates, starting with the tile in the positive X direction, and rotating clockwise.
    /// </summary>
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

    /// <summary>
    /// The position on a plane with normal 0,1,0 at position 0,0,0 which when projected through the main camera is directly beneath the cursor, in world coordinates.
    /// </summary>
    public Vector3 MousePointOnXZ0Plane { get; private set; }

    /// <summary>
    /// The position on a plane with normal 0,1,0 at position 0,0,0 which when projected through the main camera is directly beneath the cursor, in hex coordinates.
    /// </summary>
    public Vector2 MouseHexagonOnXZ0Plane { get; private set; }

    /// <summary>
    /// The position on a plane with normal 0,1,0 at position 0,0,0 which when projected through the main camera is directly beneath the cursor, in hex coordinates rounded to the nearest tile.
    /// </summary>
    public Vector2Int MouseHexagonTileOnXZ0Plane { get; private set; }

    /// <summary>
    /// Returns the height of the terrain at the supplied x and z coordinates. Does not necessarily return the height of the mesh according to resolution.
    /// Terrain height is influenced by the elevation of the tile, and the six surrounding tiles, and some additional contributions from noise.
    /// </summary>
    public float SampleHeight(float x, float z, int tileX, int tileY)
    {
        return SampleNoise(x, z);
        //Tile elevation for tileX, tileY
        float tileHeight = SampleElevation(tileX, tileY);

        //XZ world position of the sample
        Vector2 cartesian = new Vector2(x, z);

        //XZ world position of the center of the tile
        Vector2 centerCartesian = HexUtils.HexToCartesian(tileX, tileY);

        //Distance from the sample to the tile
        float centerDistance = Vector2.Distance(cartesian, centerCartesian);

        //Calculate the influence of the main tile
        float maxDistance = HexUtils.SQRT_3 * 1.1f;
        float heightSum = tileHeight * tileHeightFalloffCurve.Evaluate((1f - Mathf.Clamp(centerDistance / maxDistance, 0, 1)));


        //Loop through and add the relative influence of the six neighbouring tiles
        for (int i = 0; i < 6; i++)
        {
            int nX = tileX + HexUtils.neighbourX[i];
            int nY = tileY + HexUtils.neighbourY[i];

            Vector2 neighbourCartesian = HexUtils.HexToCartesian(nX, nY);
            float distance = Vector2.Distance(cartesian, neighbourCartesian);
            float neighbourTileHeight = SampleElevation(nX, nY);

            heightSum += neighbourTileHeight * tileHeightFalloffCurve.Evaluate((1f - Mathf.Clamp(distance / maxDistance, 0, 1)));
        }

        //Sample noise
        float noise = SampleNoise(x, z);

        //Combine factors.
        return heightSum + noisePerTileHeightCurve.Evaluate(heightSum) * (noise - .5f);
    }

    /// <summary>
    /// Returns the height of the terrain at the supplied x and z coordinates. Does not necessarily return the height of the mesh according to resolution. Convenience method for if the tileX and tileY is not available within the scope.
    /// </summary>
    public float SampleHeight(float x, float z)
    {
        Vector2Int tile = HexUtils.HexRound(HexUtils.CartesianToHex(x, z));
        return SampleHeight(x, z, tile.x, tile.y);
    }

    public bool SampleTile(float x, float z, out HexTile tile)
    {
        Vector2Int hex = HexUtils.HexRound(HexUtils.CartesianToHex(x, z));
        tile = this[hex.x, hex.y];
        return tile != null;
    }

    /// <summary>
    /// Returns the world position which is at the height of the terrain, for the x and z coordinates of the supplied position.
    /// </summary>
    public Vector3 OnTerrain(float x, float z) => new Vector3(x, SampleHeight(x, z), z);
    public Vector3 OnTerrain(Vector3 p) => OnTerrain(p.x, p.z);
    
    /// <summary>
    /// Returns the elevation value for the tile at the hex coordinates supplied. 
    /// </summary>
    public float SampleElevation(int x, int y)
    {
        Vector2 cartesian = HexUtils.HexToCartesian(x, y);

        float noise = Mathf.PerlinNoise((cartesian.x + SeedOffsetX) * tileHeightFrequency, (cartesian.y + SeedOffsetY) * tileHeightFrequency);

        float elevation = tileHeightCurve.Evaluate(noise);

        float middleValleyBias = Mathf.Clamp(Mathf.Abs(cartesian.x / 50), 0, 0.4f);

        float southFlatnessBias =  Mathf.Clamp(cartesian.x / 50 + 1, .3f, 1f);

        return elevation * southFlatnessBias + middleValleyBias;

    }
    
    /// <summary>
    /// Samples some perlin noise at the supplied world XZ coordinates
    /// </summary>
    public float SampleNoise(float x, float z) => noiseCurve.Evaluate(Noise.Perlin(x + SeedOffsetX, z + SeedOffsetY, noiseSettings));

    /// <summary>
    /// Samples a value used for determining whether a tree should grow at the supplied world XZ coordinates
    /// </summary>
    public float SampleTree(float x, float z) => (Mathf.PerlinNoise((x + SeedOffsetX + 10000) * treesFrequency, (z + SeedOffsetY + 10000) * treesFrequency));

    /// <summary>
    /// Returns the 'temperature' value for a given world position.
    /// </summary>
    public float SampleTemperature(float x, float y, float z)
    {
        float temperature = this.temperature + 0.5f;
        temperature += x / latitudeScale;
        temperature -= Mathf.Max(0, y * altitudeTemperature) - altitudeTemperature / 4;
        return temperature;
    }

    /// <summary>
    /// Returns the material used for rendering the region borders of a certain region ID
    /// </summary>
    public Material RegionMaterial(int regionID)
    {
        if (regionID == 0)
            return null;

        regionID--;

        if (regionID < 0 || regionID >= regionMaterials.Length)
            return null;
        return regionMaterials[regionID];
    }

    public void Awake()
    {
        Instance = this;
        camera = Camera.main;
        Generate();
    }

    public void OnValidate()
    {
        //Globals used by multiple shaders
        Shader.SetGlobalFloat("_LatitudeScale", latitudeScale);
        Shader.SetGlobalFloat("_Temperature", temperature);
        Shader.SetGlobalFloat("_AltitudeTemperature", altitudeTemperature);

        Shader.SetGlobalColor("_WaterColor", waterColor);
        Shader.SetGlobalFloat("_WaterLevel", waterLevel);
        Shader.SetGlobalFloat("_WaterBlending", waterBlending);

        Shader.SetGlobalVector("_SnowColor", snowColor);
        Shader.SetGlobalFloat("_SnowIntensity", snowIntensity);
        Shader.SetGlobalFloat("_SnowBlending", snowBlending);
        Shader.SetGlobalFloat("_SnowShininess", snowShininess);
        Shader.SetGlobalFloat("_SnowSpecular", snowSpecular);
        Shader.SetGlobalFloat("_SnowSlopeMax", snowSlopeMax);

        water.transform.position = new Vector3(0, waterLevel, 0);
    }

    public void Update()
    {
        RenderRegionBorders();

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
                select = null;
            }
            if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")))
            {
                HexTile mouse = hitInfo.collider.GetComponent<HexTile>();
                if (mouse)
                {
                    select = mouse;
                    Debug.Log(select);

                    //foreach (HexTile tile in tiles)
                    //    if (tile != null)
                    //        tile.showTileBorder = false;
                    //
                    //foreach (HexTile tile in HexUtils.BreadthFirstFloodFill(select, HexUtils.SameType))
                    //    tile.showTileBorder = true;

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
        if (treesRenderer != null)
            treesRenderer.Draw(treeMesh, treeMaterial, LayerMask.NameToLayer("Trees"));

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")))
            {
                HexTile mouse = hitInfo.collider.GetComponent<HexTile>();
                if (mouse)
                {
                    SetTileRegion(regionPlacing, mouse.Position.x, mouse.Position.y);
                }
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")))
            {
                HexTile mouse = hitInfo.collider.GetComponent<HexTile>();
                if (mouse)
                {
                    SetTileRegion(0, mouse.Position.x, mouse.Position.y);
                }
            }
        }

        foreach (HexTile tile in tiles)
            Graphics.DrawMesh(tile.Mesh, tile.transform.position, Quaternion.identity, grassMaterial, LayerMask.NameToLayer("Grass"), null, 0, null, grassShadowCasting);
    }

    private Dictionary<Vector2Int, NavigationMesh.Node> navNodes;
    private List<NavigationMesh.Edge> navEdges;

    [ContextMenu("Generate Navigation Mesh")]
    public void GenerateNavigationMesh()
    {
        NavigationMesh.GenerateMesh(this, out navNodes, out navEdges);
    }

    public void OnDrawGizmos()
    {
        //if (navNodes != null)
        //{
        //    foreach (NavigationMesh.Node node in navNodes.Values)
        //    {
        //        //Handles.Label(node.Position, "" + node.Hex);
        //        Gizmos.DrawSphere(node.Position, 0.01f);
        //        //foreach (NavigationMesh.Neighbour neighbour in node.Neighbours)
        //        //    Gizmos.DrawLine(node.Position, neighbour.Node.Position);
        //    }
        //}
        if (navEdges != null)
        {
            Gizmos.color = Color.red;
            foreach (NavigationMesh.Edge edge in navEdges)
            {
                Gizmos.DrawLine(edge.Node1.Position, edge.Node2.Position);
            }
        }
       
        //DrawHexagon(MouseHexagonTileOnXZ0Plane.x, MouseHexagonTileOnXZ0Plane.y);

        if (testPath != null && testPath.FoundPath)
        {
            for (int i = 0; i < testPath.Nodes.Count - 1; i++)
            {
                HexTile i0 = testPath[i];
                HexTile i1 = testPath[i + 1];
                Gizmos.DrawLine(i0.transform.position, i1.transform.position);
            }
        }

        if (regions != null)
        {
            foreach (HexRegion region in regions.Values)
                region.OnDrawGizmos();
        }
        int j = 0;
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
            {
                int i = x + y * Width;
               // Handles.Label(tiles[i].transform.position, j + "");
                j++;
            }
    }

    /// <summary>
    /// Clears the map in all respects. Nulls various fields so beware.
    /// </summary>
    [ContextMenu("Clear")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);
        tiles = null;
        SeedOffsetX = 0;
        SeedOffsetY = 0;
        if (treesRenderer != null)
            treesRenderer.Clear();
        treesRenderer = null;
        regions = null;
        navEdges = null;
        navNodes = null;
    }
    
    /// <summary>
    /// Generates the map in all respects. Resets all regions and regenerates trees.
    /// </summary>
    [ContextMenu("Generate")]
    public void Generate()
    {
        Clear();

        tiles = new HexTile[width * height];
        regions = new Dictionary<int, HexRegion>();

        Random.InitState(seed);
        SeedOffsetX = Random.value * 10000;
        SeedOffsetY = Random.value * 10000;

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                tiles[x + z * width] = Instantiate(tilePrefab, transform);
                int hexX = x - width / 2;
                int hexY = z - height / 2 - x / 2 + width / 4;
                tiles[x + z * width].GenerateMesh(this, hexX, hexY, true);
            }

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                int hexX = x - width / 2;
                int hexY = z - height / 2 - x / 2 + width / 4;
                tiles[x + z * width].Neighbours = GetNeighbours(hexX, hexY);
            }

        SetupTrees();
    }

    /// <summary>
    /// Renders all the region borders.
    /// </summary>
    private void RenderRegionBorders()
    {
        if (regions == null)
            return;
        int layer = LayerMask.NameToLayer("Regions");
        foreach (HexRegion region in regions.Values)
            Graphics.DrawMesh(region.Mesh, new Vector3(0,0.05f,0), Quaternion.identity, RegionMaterial(region.RegionID), layer, null, 0, null, false, false);
    }

    /// <summary>
    /// Sets a tile to a certain region ID. 0 is no region. Existing regions affected by the change are updated accordingly, emptied regions are deleted. New regions are created if necessary.
    /// Returns true if the change was made successfully.
    /// </summary>
    public bool SetTileRegion(int regionID, int x, int y)
    {
        HexTile tile = this[x, y];
        if (tile == null)
            return false;

        if (tile.RegionID == regionID)
            return false;

        if (tile.ContigRegionID != 0)
        {
            HexRegion existing = regions[tile.ContigRegionID];
            if (existing != null && existing.RemoveMember(tile, out List<HexRegion> newRegions))
            {
                if (existing.Size <= 0)
                    regions.Remove(tile.ContigRegionID);

                foreach (HexRegion newRegion in newRegions)
                {
                    regions[newRegion.ContigRegionID] = newRegion;
                }
            }
            else
                return false;
        }

        if (regionID != 0)
        {
            bool added = false;

            HexRegion region = null;

            foreach (HexTile neighbour in tile.Neighbours)
            {
                if (neighbour == null)
                    continue;
                if (neighbour.RegionID == regionID)
                {
                    if (added)
                    {
                        if (neighbour.ContigRegionID != region.ContigRegionID)
                        {
                            int neighbourID = neighbour.ContigRegionID;
                            HexRegion toJoin = regions[neighbourID];
                            if (region.JoinRegion(toJoin))
                                regions.Remove(neighbourID);
                        }
                    }
                    else
                    {
                        HexRegion neighbourRegion = regions[neighbour.ContigRegionID];
                        if (neighbourRegion != null && neighbourRegion.AddMember(tile))
                        {
                            region = neighbourRegion;
                            added = true;
                        }
                    }
                }
            }

            if (!added)
            {
                region = new HexRegion(regionID, HexUtils.NewContigRegionID);
                region.AddMember(tile);
                regions.Add(region.ContigRegionID, region);
            }

        }
        return true;
    }

    private void SetupTrees()
    {
        treesRenderer = new InstancedRenderer();
        Random.InitState(seed);
        foreach (HexTile tile in tiles)
            SetupTrees(tile);
    }

    private void SetupTrees(HexTile tile)
    {
        // Vector4[] colors = new Vector4[population];

        Vector3[] vertices = tile.Mesh.vertices;

        for (int i = 0; i < vertices.Length - resolution * 3; i++)
        {
            Vector3 position = OnTerrain(tile.transform.position + vertices[i] + new Vector3((Random.value - 0.5f), 0, (Random.value - 0.5f)) * 1f / resolution);

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

            Quaternion rotation = Quaternion.Euler(0, 90, 0);// Quaternion.Euler(0, Random.Range(-180, 180), 0);
            Vector3 scale = new Vector3(1, Random.Range(treeMinimumHeight, treeMaximumHeight), 1) * Random.Range(treeMinimumScale, treeMaximumScale) * treeSample;
            Color color = treesColor.Evaluate(Random.value);
            tile.TreesCount++;

            treesRenderer.Add(Matrix4x4.TRS(position, rotation, scale), color);

            // colors[i] = Color.Lerp(Color.red, Color.blue, Random.value);
        }

        //block.SetVectorArray("_Colors", colors);
    }
}
