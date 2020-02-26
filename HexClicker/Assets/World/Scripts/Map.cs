using HexClicker.Navigation;
using HexClicker.Noise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.World
{
    [System.Serializable]
    public class Map : MonoBehaviour, IEnumerable<Tile>
    {
        public static Map Instance { get; private set; }

        public static readonly int TileResolution = 32;
        public static readonly float TileSize = 12.0f;

        public static readonly float GrassRegrowthInterval = .5f;
        public static readonly float GrassRegrowthAmount = .001f;

        private float grassRegrowthCounter;

        [SerializeField] [HideInInspector] private Tile[] tiles;
        [SerializeField] [HideInInspector] private Mesh[] skirtMeshes;

        [Header("Map Settings")]
        [SerializeField] private Tile tilePrefab;
        [SerializeField] private Material skirtMaterial;

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
        [SerializeField] [Range(0, 1)] private float treesThreshold;
        [SerializeField] private float treesMinimumTemperature;
        [SerializeField] private float treesMaximumTemperature;
        [SerializeField] private float treesMinimumAltitude;
        [SerializeField] private float treesMaximumAltitude;
        [SerializeField] private float treesMinimumHeight;
        [SerializeField] private float treesMaximumHeight;
        [SerializeField] private float treesMinimumScale;
        [SerializeField] private float treesMaximumScale;
        [SerializeField] [Range(0, 1)] private float treesRandomPosition;
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

        [Header("Navigation")]
        [SerializeField] private bool navigationDrawGraph;

        [Header("Fog of War")]
        [SerializeField] [Range(1, 20)] private float fowEdgeFactor = 1;

        public float SeedOffsetX { get; private set; }
        public float SeedOffsetY { get; private set; }
        public int Width => width;
        public int Height => height;
        public int TileCount => width * height;
        /// <summary>
        /// Returns the tile from the array at the supplied coordinates, or null if out of range.
        /// </summary>
        public Tile this[int x, int y]
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
        public Tile this[int index] => tiles[index];
        /// <summary>
        /// Iterator for the tile array.
        /// </summary>
        public IEnumerator<Tile> GetEnumerator() => ((IEnumerable<Tile>)tiles).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        /// <summary>
        /// Returns an array containing the the six neighbouring tiles (or null if the neighbouring tile is out of range) around the supplied coordinates, starting with the tile in the positive X direction, and rotating clockwise.
        /// </summary>
        public Tile[] GetNeighbours(int tileX, int tileY)
        {
            return new Tile[]
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
            GenerateSkirt();
            GenerateNavigationGraph();
        }
        void Start()
        {
            SetTileVisibilityState(0, 0, true, false);
            SetTileVisibilityState(0, -1, true, false);
            SetTileVisibilityState(-1, 0, true, false);
            SetTileVisibilityState(-1, 1, true, false);
            SetTileVisibilityState(0, 1, true, false);
            SetTileVisibilityState(1, 0, true, false);
            SetTileVisibilityState(1, -1, true, false);

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

            Shader.SetGlobalFloatArray("_Tiles", tileVisibility);
            Shader.SetGlobalFloat("_EdgeFactor", fowEdgeFactor);

            water.transform.position = new Vector3(0, waterLevel, 0);
        }

        private float[] tileVisibility = new float[1024];

        private void SetTileVisibilityState(int hexX, int hexY, bool visible, bool upload = true)
        {
            hexX += 16;
            hexY += 16;
            tileVisibility[hexX + hexY * 32] = visible ? 1 : 0;
            if (upload)
                Shader.SetGlobalFloatArray("_Tiles", tileVisibility);
        }

        private void Update()
        {
            #region Render Skirts
            foreach (Mesh m in skirtMeshes)
                Graphics.DrawMesh(m, Vector3.zero, Quaternion.identity, skirtMaterial, LayerMask.NameToLayer("Map Skirts"));
            #endregion
            #region Render Trees
            if (treesRenderer != null)
            {
                treesRenderer.Draw(treesMesh, treesMaterial, LayerMask.NameToLayer("Trees"));
            }
            #endregion

            #region Path Mask
            grassRegrowthCounter += Time.deltaTime;
            bool cover = grassRegrowthCounter >= GrassRegrowthInterval;
            if (cover)
            {
                grassRegrowthCounter -= GrassRegrowthInterval;
                NavigationGraph.GrassRegrowth(GrassRegrowthAmount);
            }
            //StartCoroutine(UpdateMasks(cover));

            foreach (Tile tile in tiles)
            {
                tile.UpdateMask(cover);
            }

            #endregion;
        }
        IEnumerator UpdateMasks(bool cover)
        {
            foreach (Tile tile in tiles)
            {
                tile.UpdateMask(cover);
                yield return null;
            }
        }

        void OnDrawGizmos()
        {
            if (navigationDrawGraph)
                NavigationGraph.OnDrawGizmos();
        }
        /// <summary>
        /// Returns the height of the terrain at the supplied x and z coordinates. Does not necessarily return the height of the mesh according to resolution.
        /// </summary>
        public float SampleHeight(float x, float z) => terrainNoiseCurve.Evaluate(Perlin.Noise(x + SeedOffsetX, z + SeedOffsetY, terrainNoiseSettings));
        /// <summary>
        /// Returns the world position which is at the height of the terrain, for the x and z coordinates of the supplied position.
        /// </summary>
        public Vector3 OnTerrain(float x, float z) => new Vector3(x, SampleHeight(x, z), z);
        public Vector3 OnTerrain(Vector2 p) => OnTerrain(p.x, p.y);
        public Vector3 OnTerrain(Vector3 p) => OnTerrain(p.x, p.z);
        /// <summary>
        /// Returns true if there is a tile beneath the supplied x and z coordinates.
        /// </summary>
        public bool SampleTile(float x, float z, out Tile tile)
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
        public float SampleTree(float x, float z) => Perlin.Noise(x + SeedOffsetX + 10000, z + SeedOffsetY + 10000, treesNoiseSettings);
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
            NavigationGraph.Clear();
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

            tiles = new Tile[width * height];
            

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
            foreach (Tile tile in tiles)
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
        
        
        [ContextMenu("Generate Navigation Graph", false, 2)]
        public void GenerateNavigationGraph()
        {
            NavigationGraph.Generate(this);
        }

        

        [ContextMenu("Generate Skirt")]
        public void GenerateSkirt()
        {
            int bottom = -10;

            Mesh m0 = new Mesh();
            Mesh m1 = new Mesh();
            Mesh m2 = new Mesh();
            Mesh m3 = new Mesh();

            List<Vector3> v0 = new List<Vector3>();
            List<Vector3> v1 = new List<Vector3>();
            List<Vector3> v2 = new List<Vector3>();
            List<Vector3> v3 = new List<Vector3>();
            List<int> t0 = new List<int>();
            List<int> t1 = new List<int>();
            List<int> t2 = new List<int>();
            List<int> t3 = new List<int>();

            List<Vector2> skirtPoints = new List<Vector2>();
            int count = 0;

            for (int x = 0; x < width; x++)
            {
                int z = 0;
                int hexX = x - width / 2;
                int hexY = z - height / 2 - x / 2 + width / 4;
                Vector2 center = HexUtils.HexToCartesian(hexX, hexY, TileSize);

                float a0 = Mathf.PI * 2 / 6 * -2;
                float a1 = Mathf.PI * 2 / 6 * -1;

                Vector2 c0 = center + new Vector2(Mathf.Cos(a0) * TileSize, Mathf.Sin(a0) * TileSize);
                Vector2 c1 = center + new Vector2(Mathf.Cos(a1) * TileSize, Mathf.Sin(a1) * TileSize);

                skirtPoints.Add(c0);
                skirtPoints.Add(c1);
            }

            for (int i = 0; i < skirtPoints.Count - 1; i++)
            {
                Vector2 p0 = skirtPoints[i];
                Vector2 p1 = skirtPoints[i + 1];

                if (i == 0)
                {
                    Vector3 c0 = OnTerrain(p0);
                    Vector3 c1 = p0.xyz(bottom);
                    v0.Add(c0);
                    v0.Add(c1);
                }

                for (int j = 1; j < TileResolution + 1; j++)
                {
                    Vector3 c2 = OnTerrain(Vector2.Lerp(p0, p1, (float)j / TileResolution));
                    Vector3 c3 = Vector2.Lerp(p0, p1, (float)j / TileResolution).xyz(bottom);
                    v0.Add(c2);
                    v0.Add(c3);

                    t0.Add(count * 2 + 0);
                    t0.Add(count * 2 + 2);
                    t0.Add(count * 2 + 1);
                    t0.Add(count * 2 + 1);
                    t0.Add(count * 2 + 2);
                    t0.Add(count * 2 + 3);
                    count++;
                }
            }

            skirtPoints = new List<Vector2>();
            count = 0;

            for (int x = 0; x < width; x++)
            {
                int z = height;
                int hexX = x - width / 2;
                int hexY = z - height / 2 - x / 2 + width / 4;
                Vector2 center = HexUtils.HexToCartesian(hexX, hexY, TileSize);

                float a0 = Mathf.PI * 2 / 6 * -2;
                float a1 = Mathf.PI * 2 / 6 * -1;

                Vector2 c0 = center + new Vector2(Mathf.Cos(a0) * TileSize, Mathf.Sin(a0) * TileSize);
                Vector2 c1 = center + new Vector2(Mathf.Cos(a1) * TileSize, Mathf.Sin(a1) * TileSize);

                skirtPoints.Add(c0);
                skirtPoints.Add(c1);
            }

            for (int i = 0; i < skirtPoints.Count - 1; i++)
            {
                Vector2 p0 = skirtPoints[i];
                Vector2 p1 = skirtPoints[i + 1];

                if (i == 0)
                {
                    Vector3 c0 = OnTerrain(p0);
                    Vector3 c1 = p0.xyz(bottom);
                    v1.Add(c0);
                    v1.Add(c1);
                }

                for (int j = 1; j < TileResolution + 1; j++)
                {
                    Vector3 c2 = OnTerrain(Vector2.Lerp(p0, p1, (float)j / TileResolution));
                    Vector3 c3 = Vector2.Lerp(p0, p1, (float)j / TileResolution).xyz(bottom);
                    v1.Add(c2);
                    v1.Add(c3);

                    t1.Add(count * 2 + 0);
                    t1.Add(count * 2 + 1);
                    t1.Add(count * 2 + 2);
                    t1.Add(count * 2 + 1);
                    t1.Add(count * 2 + 3);
                    t1.Add(count * 2 + 2);
                    count++;
                }
            }

            skirtPoints = new List<Vector2>();
            count = 0;
            for (int z = 0; z < height; z++)
            {
                int x = 0;
                int hexX = x - width / 2;
                int hexY = z - height / 2 - x / 2 + width / 4;
                Vector2 center = HexUtils.HexToCartesian(hexX, hexY, TileSize);

                float a0 = Mathf.PI * 2 / 6 * 3;
                float a1 = Mathf.PI * 2 / 6 * 2;

                Vector2 c0 = center + new Vector2(Mathf.Cos(a0) * TileSize, Mathf.Sin(a0) * TileSize);
                Vector2 c1 = center + new Vector2(Mathf.Cos(a1) * TileSize, Mathf.Sin(a1) * TileSize);
                if (z == 0)
                {
                    float a2 = Mathf.PI * 2 / 6 * 4;
                    Vector2 c2 = center + new Vector2(Mathf.Cos(a2) * TileSize, Mathf.Sin(a2) * TileSize);
                    skirtPoints.Add(c2);
                }

                skirtPoints.Add(c0);
                skirtPoints.Add(c1);
            }

            for (int i = 0; i < skirtPoints.Count - 1; i++)
            {
                Vector2 p0 = skirtPoints[i];
                Vector2 p1 = skirtPoints[i + 1];

                if (i == 0)
                {
                    Vector3 c0 = OnTerrain(p0);
                    Vector3 c1 = p0.xyz(bottom);
                    v2.Add(c0);
                    v2.Add(c1);
                }

                for (int j = 1; j < TileResolution + 1; j++)
                {
                    Vector3 c2 = OnTerrain(Vector2.Lerp(p0, p1, (float)j / TileResolution));
                    Vector3 c3 = Vector2.Lerp(p0, p1, (float)j / TileResolution).xyz(bottom);
                    v2.Add(c2);
                    v2.Add(c3);

                    t2.Add(count * 2 + 0);
                    t2.Add(count * 2 + 1);
                    t2.Add(count * 2 + 2);
                    t2.Add(count * 2 + 1);
                    t2.Add(count * 2 + 3);
                    t2.Add(count * 2 + 2);
                    count++;
                }
            }

            skirtPoints = new List<Vector2>();
            count = 0;
            for (int z = 0; z < height; z++)
            {
                int x = width - 1;
                int hexX = x - width / 2;
                int hexY = z - height / 2 - x / 2 + width / 4;
                Vector2 center = HexUtils.HexToCartesian(hexX, hexY, TileSize);

                float a0 = Mathf.PI * 2 / 6 * 0;
                float a1 = Mathf.PI * 2 / 6 * 1;

                Vector2 c0 = center + new Vector2(Mathf.Cos(a0) * TileSize, Mathf.Sin(a0) * TileSize);
                Vector2 c1 = center + new Vector2(Mathf.Cos(a1) * TileSize, Mathf.Sin(a1) * TileSize);
                if (z == 0)
                {
                    float a2 = Mathf.PI * 2 / 6 * 5;
                    Vector2 c2 = center + new Vector2(Mathf.Cos(a2) * TileSize, Mathf.Sin(a2) * TileSize);
                    skirtPoints.Add(c2);
                }

                skirtPoints.Add(c0);
                skirtPoints.Add(c1);
            }

            for (int i = 0; i < skirtPoints.Count - 1; i++)
            {
                Vector2 p0 = skirtPoints[i];
                Vector2 p1 = skirtPoints[i + 1];

                if (i == 0)
                {
                    Vector3 c0 = OnTerrain(p0);
                    Vector3 c1 = p0.xyz(bottom);
                    v3.Add(c0);
                    v3.Add(c1);
                }

                for (int j = 1; j < TileResolution + 1; j++)
                {
                    Vector3 c2 = OnTerrain(Vector2.Lerp(p0, p1, (float)j / TileResolution));
                    Vector3 c3 = Vector2.Lerp(p0, p1, (float)j / TileResolution).xyz(bottom);
                    v3.Add(c2);
                    v3.Add(c3);

                    t3.Add(count * 2 + 0);
                    t3.Add(count * 2 + 2);
                    t3.Add(count * 2 + 1);
                    t3.Add(count * 2 + 1);
                    t3.Add(count * 2 + 2);
                    t3.Add(count * 2 + 3);
                    count++;
                }
            }

            if (skirtMeshes == null)
                skirtMeshes = new Mesh[4];

            m0.vertices = v0.ToArray();
            m1.vertices = v1.ToArray();
            m2.vertices = v2.ToArray();
            m3.vertices = v3.ToArray();

            m0.uv = new Vector2[m0.vertices.Length];
            m1.uv = new Vector2[m1.vertices.Length];
            m2.uv = new Vector2[m2.vertices.Length];
            m3.uv = new Vector2[m3.vertices.Length];

            m0.triangles = t0.ToArray();
            m1.triangles = t1.ToArray();
            m2.triangles = t2.ToArray();
            m3.triangles = t3.ToArray();

            m0.RecalculateBounds();
            m1.RecalculateBounds();
            m2.RecalculateBounds();
            m3.RecalculateBounds();

            m0.RecalculateNormals();
            m1.RecalculateNormals();
            m2.RecalculateNormals();
            m3.RecalculateNormals();

            skirtMeshes[0] = m0;
            skirtMeshes[1] = m1;
            skirtMeshes[2] = m2;
            skirtMeshes[3] = m3;
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
}
