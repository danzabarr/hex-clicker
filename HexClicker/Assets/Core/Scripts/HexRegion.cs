using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class HexRegion
{
    private List<HexTile> members;
    private List<List<HexTile>> holes;

    //These lists of points are generated but can be moved to local scope if not used for anything.
    private List<Vector3> edgeOutside, edgeInside;
    private List<List<Vector3>> holeOutsides, holeInsides;
    //

    public Mesh Mesh { get; private set; }
    public int RegionID { get; private set; }
    public int ContigRegionID { get; private set; }
    public int Size => members.Count;

    public HexRegion(int regionID, int contigRegionID)
    {
        RegionID = regionID;
        ContigRegionID = contigRegionID;
        members = new List<HexTile>();
    }

    /// <summary>
    /// Method for safely adding tiles to the region such that THIS region will remain contiguous
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    public bool AddMember(HexTile tile)
    {
        if (tile == null)
            return false;

        if (tile.ContigRegionID == ContigRegionID)
            return false;

        bool isConnected = members.Count == 0;

        if (!isConnected)
            foreach(HexTile neighbour in tile.Neighbours)
                if (neighbour != null && neighbour.ContigRegionID == ContigRegionID)
                {
                    isConnected = true;
                    break;
                }

        if (!isConnected)
            return false;

        
        members.Add(tile);
        tile.RegionID = RegionID;
        tile.ContigRegionID = ContigRegionID;

        GenerateMesh();

        return true;
    }

    /// <summary>
    /// Method for safely removing tiles to the region such that THIS region will remain contiguous
    /// </summary>
    /// <param name="tile"></param>
    /// <returns></returns>
    public bool RemoveMember(HexTile tile)
    {
        if (tile == null)
            return false;

        if (tile.ContigRegionID != ContigRegionID)
            return false;

        List<HexTile> newRegion = new List<HexTile>(members);
        newRegion.Remove(tile);

        if (newRegion.Count == members.Count)
            return false;

        if (!HexUtils.IsRegionContiguous(newRegion))
            return false;

        members = newRegion;
        tile.RegionID = 0;
        tile.ContigRegionID = 0;

        GenerateMesh();

        return true;
    }

    public bool RemoveMember(HexTile tile, out List<HexRegion> newRegions)
    {
        newRegions = default;

        if (tile == null)
            return false;

        if (tile.ContigRegionID != ContigRegionID)
            return false;

        List<HexTile> newRegion = new List<HexTile>(members);
        newRegion.Remove(tile);

        if (newRegion.Count == members.Count)
            return false;

        members = newRegion;
        tile.RegionID = 0;
        tile.ContigRegionID = 0;

        newRegions = SeparateIslands();

        GenerateMesh();
        foreach (HexRegion region in newRegions)
            region.GenerateMesh();

        return true;
    }

    public bool JoinRegion(HexRegion region)
    {
        if (RegionID != region.RegionID)
            return false;

        if (ContigRegionID == region.ContigRegionID)
            return false;

        if (region.members.Count <= 0)
            return false;

        List<HexTile> newRegion = new List<HexTile>(members);
        newRegion.AddRange(region.members);

        if (!HexUtils.IsRegionContiguous(newRegion))
            return false;

        members = newRegion;
        foreach (HexTile tile in region.members)
            tile.ContigRegionID = ContigRegionID;
        region.members = new List<HexTile>();

        GenerateMesh();
        region.GenerateMesh();

        return true;

    }

    private void GenerateMesh()
    {
        edgeOutside = new List<Vector3>();
        edgeInside = new List<Vector3>();
        
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangles = new List<int>();

        Trace(members, false, out edgeInside, out edgeOutside, vertices, uv, triangles);

        holes = IdentifyHoles(members);

        holeInsides = new List<List<Vector3>>();
        holeOutsides = new List<List<Vector3>>();

        foreach(List<HexTile> hole in holes)
        {
            Trace(hole, true, out List<Vector3> insides, out List<Vector3> outsides, vertices, uv, triangles);
            holeInsides.Add(insides);
            holeOutsides.Add(outsides);
        }

        Mesh mesh = new Mesh()
        {
            vertices = vertices.ToArray(),
            uv = uv.ToArray(),
            triangles = triangles.ToArray()
        };

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();

        Mesh = mesh;
    }

    private List<HexRegion> SeparateIslands()
    {
        List<List<HexTile>> islands = HexUtils.IdentifyIslands(members);
        List<HexRegion> newRegions = new List<HexRegion>();

        for (int i = 1; i < islands.Count; i++)
        {
            HexRegion newRegion = new HexRegion(RegionID, HexUtils.NewContigRegionID);
            newRegion.members = islands[i];
            foreach (HexTile tile in islands[i])
            {
                members.Remove(tile);
                tile.ContigRegionID = newRegion.ContigRegionID;
            }
            newRegions.Add(newRegion);
        }

        return newRegions;
    }

    //Don't make me comment this method. I won't.
    private static void Trace(List<HexTile> region, bool outwardEdge, out List<Vector3> edgeInside, out List<Vector3> edgeOutside, List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
    {
        if (region.Count <= 0)
        {
            edgeOutside = default;
            edgeInside = default;
            return;
        }

        int e = 0;
        HexTile t = null;

        foreach (HexTile tile in region)
        {
            tile.inFloodFillSet = true;
            if (t == null || tile.Position.x > t.Position.x)
                t = tile;
        }

        List<Vector3> inside = new List<Vector3>();
        List<Vector3> outside = new List<Vector3>();
        List<int> insideEdgeLengths = new List<int>();
        List<bool> turns = new List<bool>();

        bool turnedOutward = false;
        bool firstEdge = true;

        HexMap map = HexMap.Instance;

        void NextEdge()
        {
            if (t == null)
                return;

            for (int i = 0; i < 7; i++)
            {
                int startIndex = vertices.Count;
                int insideLength = map.Resolution;

                Vector3 outsidePoint = map.OnTerrain(t.transform.position + new Vector3(cosAngles[e], 0, sinAngles[e]) * map.Size);
                if (!firstEdge)
                {
                    Vector3 previousOutsidePoint = outside[outside.Count - 1];

                    for (int j = 1; j < map.Resolution; j++)
                    {
                        Vector3 intermediatePoint = map.OnTerrain(Vector3.Lerp(new Vector3(previousOutsidePoint.x, 0, previousOutsidePoint.z), new Vector3(outsidePoint.x, 0, outsidePoint.z), (float) j / map.Resolution));
                        outside.Add(intermediatePoint);
                    }
                }
                outside.Add(outsidePoint);

                Vector3 insidePoint;

                HexTile n = t.Neighbours[(e + 1) % 6] as HexTile;
                if (n != null && n.inFloodFillSet)
                {
                    if (outwardEdge)
                    {
                        insidePoint = map.OnTerrain(t.transform.position + Vector3.LerpUnclamped(new Vector3(cosAngles[e], 0, sinAngles[e]), new Vector3(cosAngles[(e + 1) % 6], 0, sinAngles[(e + 1) % 6]), -1f / map.Resolution) * map.Size);

                        if (!firstEdge)
                        {
                            Vector3 previousInsidePoint = inside[inside.Count - 1];
                            insideLength = map.Resolution;
                            if (turnedOutward)
                                insideLength--;
                            for (int j = 1; j < insideLength; j++)
                            {
                                Vector3 intermediatePoint = map.OnTerrain(Vector3.Lerp(new Vector3(previousInsidePoint.x, 0, previousInsidePoint.z), new Vector3(insidePoint.x, 0, insidePoint.z), (float)j / insideLength));
                                inside.Add(intermediatePoint);
                            }
                        }
                        inside.Add(insidePoint);
                    }
                    else
                    {
                        insidePoint = map.OnTerrain(t.transform.position + Vector3.Lerp(new Vector3(cosAngles[e], 0, sinAngles[e]), new Vector3(cosAngles[(e + 1) % 6], 0, sinAngles[(e + 1) % 6]), 1f / map.Resolution) * map.Size);

                        if (!firstEdge)
                        {
                            Vector3 previousInsidePoint = inside[inside.Count - 1];
                            insideLength = map.Resolution;
                            if (turnedOutward)
                                insideLength++;
                            for (int j = 1; j < insideLength; j++)
                            {
                                Vector3 intermediatePoint = map.OnTerrain(Vector3.Lerp(new Vector3(previousInsidePoint.x, 0, previousInsidePoint.z), new Vector3(insidePoint.x, 0, insidePoint.z), (float)j / insideLength));
                                inside.Add(intermediatePoint);
                            }
                        }
                        inside.Add(insidePoint);
                    }

                    e += 5;
                    e %= 6;
                    t = n;

                    turnedOutward = true;

                    if (!firstEdge)
                    {
                        insideEdgeLengths.Add(insideLength);
                        turns.Add(turnedOutward);
                    }

                    firstEdge = false;

                    if (t.edgesVisited[e])
                    {
                        break;
                    }

                    t.edgesVisited[e] = true;

                    NextEdge();
                    return;
                }

                if (outwardEdge)
                {
                    insidePoint = map.OnTerrain(t.transform.position + new Vector3(cosAngles[e], 0, sinAngles[e]) * map.Size * (1 + 1f / map.Resolution));

                    if (!firstEdge)
                    {
                        Vector3 previousInsidePoint = inside[inside.Count - 1];

                        insideLength = map.Resolution + 1;
                        if (turnedOutward)
                            insideLength--;
                        for (int j = 1; j < insideLength; j++)
                        {

                            Vector3 intermediatePoint = map.OnTerrain(Vector3.Lerp(new Vector3(previousInsidePoint.x, 0, previousInsidePoint.z), new Vector3(insidePoint.x, 0, insidePoint.z), (float)j / insideLength));
                            inside.Add(intermediatePoint);
                        }
                    }
                    inside.Add(insidePoint);
                }
                else
                {
                    insidePoint = map.OnTerrain(t.transform.position + new Vector3(cosAngles[e], 0, sinAngles[e]) * map.Size * (1 - 1f / map.Resolution));

                    if (!firstEdge)
                    {
                        Vector3 previousInsidePoint = inside[inside.Count - 1];

                        insideLength = map.Resolution - 1;
                        if (turnedOutward)
                            insideLength++;
                        for (int j = 1; j < insideLength; j++)
                        {
                            Vector3 intermediatePoint = map.OnTerrain(Vector3.Lerp(new Vector3(previousInsidePoint.x, 0, previousInsidePoint.z), new Vector3(insidePoint.x, 0, insidePoint.z), (float)j / insideLength));
                            inside.Add(intermediatePoint);
                        }
                    }
                    inside.Add(insidePoint);
                }

                e++;
                e %= 6;


                turnedOutward = false;
                if (!firstEdge)
                {
                    insideEdgeLengths.Add(insideLength);
                    turns.Add(turnedOutward);
                }

                if (t.edgesVisited[e])
                {
                    break;
                }

                t.edgesVisited[e] = true;

                firstEdge = false;
            }
        }

        NextEdge();

        outside.RemoveAt(outside.Count - 1);
        inside.RemoveAt(inside.Count - 1);

        int outsideIndex = vertices.Count;
        int insideIndex = vertices.Count + outside.Count;

        if (outwardEdge)
        {
            int o = 0;
            int i = 0;

            for (int turn = 0; turn < turns.Count; turn++)
            {
                bool prevTurn = turns[(turn - 1 + turns.Count) % turns.Count];

                if (prevTurn)
                {
                   triangles.Add(insideIndex + i + 0);
                   triangles.Add(outsideIndex + o + 0);
                   triangles.Add(outsideIndex + o + 1);
                }
                else
                {
                    triangles.Add(insideIndex + i + 0);
                    triangles.Add(insideIndex + (i - 1 + inside.Count) % inside.Count);
                    triangles.Add(outsideIndex + o + 0);
                }

                int len = map.Resolution * 2 - 2;

                if (!prevTurn)
                    len+=2;

                for (int j = 0; j < len; j++)
                {
                    int t0;
                    int t1;
                    int t2;

                    if (j % 2 == 0)
                    {
                        t0 = insideIndex + (i + j / 2) % inside.Count;
                        t1 = outsideIndex + (o + j / 2 + (prevTurn ? 1 : 0)) % outside.Count;
                        t2 = insideIndex + (i + j / 2 + 1) % inside.Count;
                    }
                    else
                    {
                        t0 = insideIndex + (i + j / 2 + 1) % inside.Count;
                        t1 = outsideIndex + (o + j / 2 + (prevTurn ? 1 : 0)) % outside.Count;
                        t2 = outsideIndex + (o + j / 2 + 1 + (prevTurn ? 1 : 0)) % outside.Count;
                    }

                    triangles.Add(t0);
                    triangles.Add(t1);
                    triangles.Add(t2);
                }

                o += map.Resolution;
                i += insideEdgeLengths[turn];
            }
        }
        else
        {
            int o = 0;
            int i = 0;

            for (int turn = 0; turn < turns.Count; turn++)
            {
                bool prevTurn = turns[(turn - 1 + turns.Count) % turns.Count];

                if (prevTurn)
                {
                    triangles.Add(outsideIndex + o + 0);
                    triangles.Add(insideIndex + (i - 1 + inside.Count) % inside.Count);
                    triangles.Add(insideIndex + i + 0);
                    
                    triangles.Add(outsideIndex + o + 0);
                    triangles.Add(insideIndex + i + 0);
                    triangles.Add(insideIndex + i + 1);
                }

                for (int j = 0; j < map.Resolution * 2 - 1; j++)
                {
                    int t0;
                    int t1;
                    int t2;

                    if (j % 2 == 0)
                    {
                        t0 = outsideIndex + (o + j / 2) % outside.Count;
                        t1 = insideIndex + (i + j / 2 + (prevTurn ? 1 : 0)) % inside.Count;
                        t2 = outsideIndex + (o + j / 2 + 1) % outside.Count;
                    }
                    else
                    {
                        t0 = outsideIndex + (o + j / 2 + 1) % outside.Count;
                        t1 = insideIndex + (i + j / 2 + (prevTurn ? 1 : 0)) % inside.Count;
                        t2 = insideIndex + (i + j / 2 + 1 + (prevTurn ? 1 : 0)) % inside.Count;
                    }

                    triangles.Add(t0);
                    triangles.Add(t1);
                    triangles.Add(t2);
                }

                o += map.Resolution;
                i += insideEdgeLengths[turn];
            }
        }


        vertices.AddRange(outside);
        vertices.AddRange(inside);

        for (int i = 0; i < outside.Count; i++)
            uv.Add(new Vector2(0, 1));

        for (int i = 0; i < inside.Count; i++)
            uv.Add(new Vector2(0, 0));

        foreach (HexTile tile in region)
        {
            tile.edgesVisited = new bool[6];
            tile.inFloodFillSet = false;
        }

        edgeInside = inside;
        edgeOutside = outside;

    }

    public static List<List<HexTile>> IdentifyHoles(List<HexTile> region)
    {
        List<List<HexTile>> holes = new List<List<HexTile>>();

        HexMap map = HexMap.Instance;

        if (region.Count >= map.TileCount)
            return holes;

        const int isUnknown = 0;
        const int isEdge = 1;
        const int isOutsideRegion = 2;
        const int isInsideRegion = 3;
        const int isHole = 4;

        foreach (HexTile tile in map)
            tile.state = isUnknown;

        foreach (HexTile tile in region)
            tile.state = isInsideRegion;

        List<HexTile> outsideEdge = new List<HexTile>();

        foreach (HexTile tile in region)
        {
            foreach (HexTile neighbour in tile.Neighbours)
            {
                if (neighbour == null)
                    continue;
                if (neighbour.state == isUnknown)
                {
                    outsideEdge.Add(neighbour);
                    neighbour.state = isEdge;
                }
            }
        }

        //Returns whether the list is a hole
        bool BreadthFirstFloodFill(HexTile start, out List<HexTile> list)
        {
            list = null;

            if (start.state != isEdge)
                return false;

            list = new List<HexTile>();

            bool hole = true;

            Queue<HexTile> frontier = new Queue<HexTile>();
            start.inFloodFillSet = true;
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                HexTile tile = frontier.Dequeue();
                list.Add(tile);
                foreach (HexTile neighbour in tile.Neighbours)
                {
                    if (neighbour == null)
                    {
                        hole = false;
                        continue;
                    }

                    if (neighbour.state == isOutsideRegion)
                        hole = false;

                    if (neighbour.state == isInsideRegion)
                        continue;

                    if (neighbour.inFloodFillSet)
                        continue;

                    neighbour.inFloodFillSet = true;
                    frontier.Enqueue(neighbour);
                }
            }

            foreach (HexTile tile in list)
            {
                tile.inFloodFillSet = false;
                if (hole)
                    tile.state = isHole;
                else
                    tile.state = isOutsideRegion;
            }

            return hole;
        }

        foreach (HexTile tile in outsideEdge)
            if (BreadthFirstFloodFill(tile, out List<HexTile> hole))
                holes.Add(hole);

        return holes;
    }

    public void OnDrawGizmos()
    {
        /*
        if (members != null)
        {
            Gizmos.color = new Color(1, 1, 1, .5f);
            foreach (HexTile tile in members)
                if (tile != null)
                    Gizmos.DrawMesh(HexUtils.Mesh, tile.transform.position);
        }

        void DrawOutline(List<Vector3> points)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p0 = points[i];
                Vector3 p1 = points[(i + 1) % points.Count];
                Gizmos.color = Color.white;
                Gizmos.DrawLine(p0, p1);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(p0, .01f );
            }
        }
        
        if (edgeOutside != null)
        {
            DrawOutline(edgeOutside);
        }

        if (edgeInside != null)
        {
            DrawOutline(edgeInside);
        }

        if (holes != null)
        {
            for (int i = 0; i < holes.Count; i++)
            {
                DrawOutline(holeInsides[i]);
                DrawOutline(holeOutsides[i]);
            }
        }
        */

        if (Mesh != null)
        {
            Gizmos.color = new Color(1, 1, 1, .5f);
            Gizmos.DrawMesh(Mesh);
            Gizmos.color = Color.red;
            Gizmos.DrawWireMesh(Mesh);
            int index = 0;
            foreach(Vector3 vertex in Mesh.vertices)
            {
                Handles.Label(vertex, index + "");
                index++;
            }
        }

    }

    public static readonly float[] angles =
   {
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * -0.5f,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 0.5f,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 1.5f,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 2.5f,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 3.5f,
        Mathf.PI / 2f + Mathf.PI * 2f / 6f * 4.5f,
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
}
