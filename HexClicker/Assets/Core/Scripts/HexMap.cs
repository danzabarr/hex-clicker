using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

[System.Serializable]
public class HexMap : MonoBehaviour, IEnumerable<HexTile>
{
    public static HexMap Instance { get; private set; }

    public static readonly int TileResolution = 16;
    public static readonly float TileSize = 4.0f;

    public static readonly float GrassRegrowthInterval = .5f;
    public static readonly float GrassRegrowthAmount = .001f;

    private static float pathMaskCoverCounter;

    [SerializeField] [HideInInspector] private HexTile[] tiles;
    private Dictionary<int, HexRegion> regions;

    [Header("Map Settings")]
    [SerializeField]
    private HexTile tilePrefab;

    [SerializeField] private int seed;
    [SerializeField] private int width;
    [SerializeField] private int height;

    [Header("Terrain Noise")]
    [SerializeField] private Noise.NoiseSettings terrainNoiseSettings;
    [SerializeField] private AnimationCurve terrainNoiseCurve;

    [Header("Trees")]
    [SerializeField] private Mesh treesMesh;
    [SerializeField] private Material treesMaterial;
    [SerializeField] private Gradient treesColor;
    [SerializeField] private Noise.NoiseSettings treesNoiseSettings;
    [SerializeField] [Range( 0,  1)] private float treesThreshold;
    [SerializeField] private float treesMinimumTemperature;
    [SerializeField] private float treesMaximumTemperature;
    [SerializeField] private float treesMinimumAltitude;
    [SerializeField] private float treesMaximumAltitude;
    [SerializeField] private float treesMinimumHeight;
    [SerializeField] private float treesMaximumHeight;
    [SerializeField] private float treesMinimumScale;
    [SerializeField] private float treesMaximumScale;
    [SerializeField] [Range( 0,  1)] private float treesRandomPosition;
    private InstancedRenderer treesRenderer;

    [Header("Temperature")]
    [SerializeField] [Range(-10, 10)] private float temperature;
    [SerializeField] private float latitudeScale;
    [SerializeField] private float altitudeTemperature;

    [Header("Water")]
    [SerializeField] private Transform water;
    [SerializeField] private float waterLevel = 0f;
    [SerializeField] [Range(0, 1)] private float waterBlending = .1f;
    [SerializeField] private Color waterColor = Color.blue;

    [Header("Snow")]
    [SerializeField] [Range(0, 1)] private float snowBlending = 0.1f;
    [SerializeField] [Range(0, 1)] private float snowSlopeMax = .5f;
    [SerializeField] private float snowShininess = 1;
    [SerializeField] private float snowSpecular = 1;
    [SerializeField] private Color snowColor = Color.white;
    [SerializeField] private float snowIntensity = 1;

    [Header("Regions")]
    [SerializeField] private Material[] regionMaterials;
    [SerializeField] private int regionPlacing = 1;

    [Header("Navigation")]
    [SerializeField] private bool navigationDrawGraph;
    [SerializeField] private Unit[] testUnit;

    [Header("Building")]
    public Building placingObject;
    public Material cantBuildMaterial;
    public float placingRotation;

    [Header("Control Mode")]
    public ControlMode controlMode;
    public enum ControlMode
    {
        Regions,
        Build,
        Units
    }
    public float SeedOffsetX { get; private set; }
    public float SeedOffsetY { get; private set; }
    public int Width => width;
    public int Height => height;
    public int TileCount => width * height;
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
    /// Returns the tile at a given index in the array. NO CHECKS!
    /// </summary>
    public HexTile this[int index] => tiles[index];
    /// <summary>
    /// Iterator for the tile array.
    /// </summary>
    public IEnumerator<HexTile> GetEnumerator() => ((IEnumerable<HexTile>)tiles).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
    void OnEnable()
    {
        Instance = this;
    }
    void Awake()
    {
        Instance = this;
        Generate();
        GenerateNavigationGraph();
        SetPlacingObject(placingObject);
    }
    void Start()
    {
        ShaderUpload();
    }
    void OnValidate()
    {
        ShaderUpload();
    }
    private void ShaderUpload()
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
        Shader.SetGlobalFloat("_TileSize", TileSize);

        water.transform.position = new Vector3(0, waterLevel, 0);
    }
    void Update()
    {
        switch (controlMode)
        {
            case ControlMode.Regions:
                ControlModeRegions();
                break;
            case ControlMode.Build:
                ControlModeBuild();
                break;
            case ControlMode.Units:
                ControlModeUnits();
                break;
        }
        #region Render Trees
        if (treesRenderer != null)
        {
            treesRenderer.Draw(treesMesh, treesMaterial, LayerMask.NameToLayer("Trees"));
        }
        #endregion
        #region Render Region Borders
        if (regions != null)
        {
            int layer = LayerMask.NameToLayer("Regions");
            foreach (HexRegion region in regions.Values)
                Graphics.DrawMesh(region.Mesh, Vector3.zero, Quaternion.identity, RegionMaterial(region.RegionID), layer, null, 0, null, false, false);
        }
        #endregion
        #region Path Mask
        pathMaskCoverCounter += Time.deltaTime;
        bool cover = pathMaskCoverCounter >= GrassRegrowthInterval;
        if (cover)
        {
            pathMaskCoverCounter -= GrassRegrowthInterval;
            Navigation.FadeOutPaths(GrassRegrowthAmount);
        }
        StartCoroutine(UpdateMasks(cover));
        #endregion;
    }
    IEnumerator UpdateMasks(bool cover)
    {
        foreach (HexTile tile in tiles)
        {
            tile.UpdateMask(cover);
            yield return null;
        }
    }
    void OnDrawGizmos()
    {
        if (navigationDrawGraph)
            Navigation.OnDrawGizmos();
    }
    /// <summary>
    /// Returns the height of the terrain at the supplied x and z coordinates. Does not necessarily return the height of the mesh according to resolution.
    /// </summary>
    public float SampleHeight(float x, float z) => terrainNoiseCurve.Evaluate(Noise.Perlin(x + SeedOffsetX, z + SeedOffsetY, terrainNoiseSettings));
    /// <summary>
    /// Returns the world position which is at the height of the terrain, for the x and z coordinates of the supplied position.
    /// </summary>
    public Vector3 OnTerrain(float x, float z) => new Vector3(x, SampleHeight(x, z), z);
    public Vector3 OnTerrain(Vector2 p) => OnTerrain(p.x, p.y);
    public Vector3 OnTerrain(Vector3 p) => OnTerrain(p.x, p.z);
    /// <summary>
    /// Returns true if there is a tile beneath the supplied x and z coordinates.
    /// </summary>
    public bool SampleTile(float x, float z, out HexTile tile)
    {
        Vector2Int hex = HexUtils.HexRound(HexUtils.CartesianToHex(x, z, TileSize));
        tile = this[hex.x, hex.y];
        return tile != null;
    }
    public bool SampleTile(float x, float z)
    {
        Vector2Int hex = HexUtils.HexRound(HexUtils.CartesianToHex(x, z, TileSize));
        return this[hex.x, hex.y] != null;
    }
    /// <summary>
    /// Samples a value used for determining whether a tree should grow at the supplied world XZ coordinates
    /// </summary>
    public float SampleTree(float x, float z) => Noise.Perlin(x + SeedOffsetX + 10000, z + SeedOffsetY + 10000, treesNoiseSettings);
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
    /// <summary>
    /// Sets a tile to a certain region ID. 0 is no region. Existing regions affected by the change are updated accordingly, emptied regions are deleted. New regions are created if necessary.
    /// Returns true if the change was made successfully.
    /// </summary>
    public bool SetRegion(int regionID, int x, int y)
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
    /// <summary>
    /// Clears the map in all respects. Nulls various fields so beware.
    /// </summary>
    [ContextMenu("Clear", false, 0)]
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
        Navigation.Clear();
        //NavMesh.RemoveAllNavMeshData();
    }
    /// <summary>
    /// Generates the map in all respects. Resets all regions and regenerates trees.
    /// </summary>
    [ContextMenu("Generate Map", false, 1)]
    public void Generate()
    {
        #region Setup

        Clear();

        tiles = new HexTile[width * height];
        regions = new Dictionary<int, HexRegion>();

        Random.InitState(seed);
        SeedOffsetX = Random.value * 10000;
        SeedOffsetY = Random.value * 10000;
        
        #endregion
        #region Generate Tiles

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
                tiles[x + z * width] = Instantiate(tilePrefab, transform);

        for (int x = 0; x < width; x++)
            for (int z = 0; z < height; z++)
            {
                int hexX = x - width / 2;
                int hexY = z - height / 2 - x / 2 + width / 4;
                tiles[x + z * width].GenerateMesh(this, hexX, hexY, true);
            }
        #endregion
        #region Generate Trees

        treesRenderer = new InstancedRenderer();
        Random.InitState(seed);
        foreach (HexTile tile in tiles)
        {
            Vector3[] vertices = tile.Mesh.vertices;

            for (int i = 0; i < vertices.Length - TileResolution * 3; i++)
            {
                Vector3 position = OnTerrain(tile.transform.position + vertices[i] + new Vector3(Random.value - 0.5f, 0, Random.value - 0.5f) * TileSize / TileResolution * treesRandomPosition);

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
                Vector3 scale = new Vector3(1, Random.Range(treesMinimumHeight, treesMaximumHeight), 1) * Random.Range(treesMinimumScale, treesMaximumScale);
                Color color = treesColor.Evaluate(Random.value);
                tile.TreesCount++;

                treesRenderer.Add(Matrix4x4.TRS(position, rotation, scale), color);

                // colors[i] = Color.Lerp(Color.red, Color.blue, Random.value);
            }
        }
        #endregion
    }
    private void ControlModeUnits()
    {
        
       
    }
    private void ControlModeBuild()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            placingRotation--;
        if (Input.GetKey(KeyCode.RightArrow))
            placingRotation++;

        if (ScreenCast.MouseTerrain.Cast(out RaycastHit hitInfo))//Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")) && hitInfo.collider.GetComponent<HexTile>())
        {
            HexTile mouse = hitInfo.collider.GetComponent<HexTile>();
            Matrix4x4 parentTransform = Matrix4x4.TRS(hitInfo.point, Quaternion.Euler(0, placingRotation, 0), Vector3.one);

            placingObject.ToTerrain(parentTransform, this);
            if (mouse && mouse.RegionID == 1 && !placingObject.CheckCollisions(parentTransform, LayerMask.GetMask("Buildings", "Water")))
            {
                placingObject.Draw(parentTransform, LayerMask.NameToLayer("Placing"), null, true);

                if (Input.GetMouseButtonDown(0))
                {
                    Instantiate(placingObject, hitInfo.point, Quaternion.Euler(0, placingRotation, 0));
                }

                return;
            }
            placingObject.Draw(parentTransform, LayerMask.NameToLayer("Placing"), cantBuildMaterial, false);
        }
    }
    public void SetPlacingObject(Building placingObject)
    {
        this.placingObject = placingObject;
        if (placingObject)
            placingObject.ExtractParts();
    }
    private void ControlModeRegions()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (ScreenCast.MouseTerrain.Cast(out HexTile mouse))
            {
                SetRegion(regionPlacing, mouse.Position.x, mouse.Position.y);
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (ScreenCast.MouseTerrain.Cast(out HexTile mouse))
            {
                SetRegion(0, mouse.Position.x, mouse.Position.y);
            }
        }
    }
    [ContextMenu("Generate Navigation Graph", false, 2)]
    public void GenerateNavigationGraph()
    {
        Navigation.GenerateNavigationGraph(this);
    }

    #region Unused
    /*
 public void GenerateNavigationMesh()
 {

     List<NavMeshBuildSource> sources = navMeshSurface.CollectSources();

     int treeArea = NavMesh.GetAreaFromName("Tree");
     Vector3 treeSize = new Vector3(.5f, 1f, .5f);


     foreach (Matrix4x4 transform in treesRenderer)
     {
         sources.Add(new NavMeshBuildSource()
         {
             transform = transform,
             area = treeArea,
             shape = NavMeshBuildSourceShape.ModifierBox,
             size = treeSize
         });
     }
     navMeshSurface.BuildNavMesh(sources);

     */

    /*
    NavMeshBuildSettings buildSettings = new NavMeshBuildSettings
    {
        agentRadius = navMeshBuildSettings.agentRadius,
        agentHeight = navMeshBuildSettings.agentHeight,
        agentSlope = navMeshBuildSettings.agentSlope,
        agentClimb = navMeshBuildSettings.agentClimb,
        minRegionArea = navMeshBuildSettings.minRegionArea,
        overrideTileSize = true,
        tileSize = navMeshBuildSettings.tileSize,
        overrideVoxelSize = true,
        voxelSize = navMeshBuildSettings.voxelSize
    };

    List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();

    foreach (HexTile tile in tiles)
    {
        sources.Add(new NavMeshBuildSource()
        {
            transform = tile.transform.localToWorldMatrix,
            area = 0,
            component = tile,
            shape = NavMeshBuildSourceShape.Mesh,
            sourceObject = tile.Mesh
        });
    }

    NavMeshData data = NavMeshBuilder.BuildNavMeshData(buildSettings, sources, navMeshArea, Vector3.zero, Quaternion.identity);

    NavMesh.RemoveAllNavMeshData();
    NavMesh.AddNavMeshData(data);
    
}
*/
    #endregion
}
