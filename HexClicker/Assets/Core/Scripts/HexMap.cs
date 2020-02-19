using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;

[System.Serializable]
[ExecuteAlways]
public class HexMap : MonoBehaviour, IEnumerable<HexTile>
{
    public static HexMap Instance { get; private set; }

    public static readonly int TileResolution = 16;
    public static readonly float TileSize = 4.0f;
    public static readonly int NavigationResolution = 32;
    public static readonly float NavigationMinHeight = 0.0f;
    public static readonly float NavigationMaxHeight = 1.25f;

    private new Camera camera;
    [SerializeField]
    [HideInInspector]
    private HexTile[] tiles;
    private HexTile select;
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
    public float SeedOffsetX { get; private set; }
    public float SeedOffsetY { get; private set; }
    public int Width => width;
    public int Height => height;
    public int TileCount => width * height;

    [Header("Terrain Noise")]
    [SerializeField]
    private Noise.NoiseSettings terrainNoiseSettings;
    [SerializeField]
    private AnimationCurve terrainNoiseCurve;

    [Header("Trees")]
    [SerializeField]
    private Mesh treesMesh;
    [SerializeField]
    private Material treesMaterial;
    [SerializeField]
    private Gradient treesColor;
    [SerializeField]
    private Noise.NoiseSettings treesNoiseSettings;
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
    private float treesMinimumHeight, treesMaximumHeight;
    [SerializeField]
    private float treesMinimumScale, treesMaximumScale;
    [SerializeField]
    [Range(0, 1)]
    private float treesRandomPosition;
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

    [Header("Navigation")]

    [SerializeField]
    private bool navigationDrawGraph;
    [SerializeField]
    private bool navigationRaycastModifier;
    [SerializeField]
    private Unit testUnit;
    //    public Transform start, end;

    /*
    [SerializeField]
    private Bounds navMeshArea;
    [SerializeField]
    private NavMeshBuildSettingsSerialized navMeshBuildSettings;
    [System.Serializable]
    public struct NavMeshBuildSettingsSerialized
    {
        public float agentRadius;
        public float agentHeight;
        public float agentSlope;
        public float agentClimb;
        public float minRegionArea;
        public int tileSize;
        public float voxelSize;
    }

    private NavMeshSurface navMeshSurface;
    */
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

    private List<Unit> selection = new List<Unit>();

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
    
    /// <summary>
    /// Returns the tile at a given index in the array. NO CHECKS!
    /// </summary>
    public HexTile this[int index] => tiles[index];
    
    /// <summary>
    /// Returns the number of tiles in the map.
    /// </summary>
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

    public delegate float SampleFloat(float x, float z);
    public delegate bool SampleBool(float x, float z);

    /// <summary>
    /// Returns the height of the terrain at the supplied x and z coordinates. Does not necessarily return the height of the mesh according to resolution.
    /// </summary>
    public float SampleHeight(float x, float z)
    {
        return SampleNoise(x, z);
    }
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
    /// Returns the world position which is at the height of the terrain, for the x and z coordinates of the supplied position.
    /// </summary>
    public Vector3 OnTerrain(float x, float z) => new Vector3(x, SampleHeight(x, z), z);
    public Vector3 OnTerrain(Vector2 p) => OnTerrain(p.x, p.y);
    public Vector3 OnTerrain(Vector3 p) => OnTerrain(p.x, p.z);
    
    /// <summary>
    /// Samples some perlin noise at the supplied world XZ coordinates
    /// </summary>
    public float SampleNoise(float x, float z) => terrainNoiseCurve.Evaluate(Noise.Perlin(x + SeedOffsetX, z + SeedOffsetY, terrainNoiseSettings));

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

    public void OnEnable()
    {
        Instance = this;
        camera = Camera.main;
    }

    public void Awake()
    {
        Instance = this;
        camera = Camera.main;
        Generate();
        GenerateNavigationGraph();
        SetPlacingObject(placingObject);
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


    

    public void Update()
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
        #region Render Grass
        if (tiles != null)
        {
            foreach (HexTile tile in tiles)
                Graphics.DrawMesh(tile.Mesh, tile.transform.position, Quaternion.identity, grassMaterial, LayerMask.NameToLayer("Grass"), null, 0, null, grassShadowCasting);
        }
        #endregion
        #region Render Region Borders
        if (regions != null)
        {
            int layer = LayerMask.NameToLayer("Regions");
            foreach (HexRegion region in regions.Values)
                Graphics.DrawMesh(region.Mesh, new Vector3(0, 0.05f, 0), Quaternion.identity, RegionMaterial(region.RegionID), layer, null, 0, null, false, false);
        }
        #endregion
    }

    private bool MousePickComponent<T>(int layermask, float maxDistance, out RaycastHit hitInfo, out T component) where T : Component
    {
        component = null;
        if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hitInfo, maxDistance, layermask))
            component = hitInfo.collider.GetComponent<T>();
        return component != null;
    }

    private int MousePickComponent<A, B>(int layermask, float maxDistance, out RaycastHit hitInfo, out A componentA, out B componentB)
        where A : Component
        where B : Component
    {
        componentA = null;
        componentB = null;
        if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out hitInfo, maxDistance, layermask))
        {
            componentA = hitInfo.collider.GetComponent<A>();
            componentB = hitInfo.collider.GetComponent<B>();
        }
        int result = 0;

        if (componentA != null)
            result += 1;

        if (componentB != null)
            result += 2;

        return result;
    }

    private PathFinding.Path<Navigation.Node> testPath;
    private PathFinding.Path<Navigation.Node> testPathRaycast;
    public void OnDrawGizmos()
    {
        if (navigationDrawGraph)
            Navigation.OnDrawGizmos();


        if (testPath != null)
        {
            Gizmos.color = Color.white;
            Navigation.DrawPath(testPath, true, false, true);
        }
        if (testPathRaycast != null)
        {
            Gizmos.color = Color.blue;
            Navigation.DrawPath(testPathRaycast, true, false, true);
        }
    }

    public void ControlModeUnits()
    {
        if (Input.GetMouseButtonDown(0))
        {
            switch (MousePickComponent<HexTile, Unit>(LayerMask.GetMask("Terrain", "Buildings", "Units"), 1000, out _, out _, out Unit unit))
            {
                case 1:
                    selection = new List<Unit>();
                    break;
                case 2:
                    selection = new List<Unit> { unit };
                    break;
                case 3:
                    goto case 2;
            }
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (MousePickComponent<HexTile>(LayerMask.GetMask("Terrain", "Buildings", "Units"), 1000, out RaycastHit hitInfo, out _))
            {

                //if (Navigation.PathFind(OnTerrain(testUnit.transform.position), hitInfo.point, 5000, 20000, PathFinding.StandardCostFunction, out testPath, out List<Navigation.Node> visited, false) == PathFinding.Result.Success)
                //{
                //    testPathRaycast = testPath.Duplicate();
                //
                //    Navigation.RaycastModifier(testPathRaycast, TileSize / NavigationResolution);
                //
                //    foreach(Navigation.Node node in visited)
                //        PathFinding.ClearPathFindingData(node);
                //}
                testUnit.transform.position = OnTerrain(testUnit.transform.position);
                testUnit.SetDestination(hitInfo.point);
            }
        }
    }

    public void ControlModeBuild()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
            placingRotation--;
        if (Input.GetKey(KeyCode.RightArrow))
            placingRotation++;

        if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")) && hitInfo.collider.GetComponent<HexTile>())
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
            if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out RaycastHit hitInfo, 1000, LayerMask.GetMask("Terrain")))
            {
                HexTile mouse = hitInfo.collider.GetComponent<HexTile>();
                if (mouse)
                {
                    SetRegion(regionPlacing, mouse.Position.x, mouse.Position.y);
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
                    SetRegion(0, mouse.Position.x, mouse.Position.y);
                }
            }
        }
    }


    [ContextMenu("Generate Navigation Graph", false, 2)]
    public void GenerateNavigationGraph()
    {
        Navigation.GenerateNavigationGraph();
    }
}
