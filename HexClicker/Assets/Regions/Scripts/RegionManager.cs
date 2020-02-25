using HexClicker.World;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Regions
{
    public class RegionManager : MonoBehaviour
    {
        [SerializeField] private Material[] regionMaterials;
        [SerializeField] private int placingRegionID;

        private static Dictionary<int, Region> regions = new Dictionary<int, Region>();
        private static int IDCounter = 0;
        public static int NewRegionID
        {
            get
            {
                IDCounter++;
                return IDCounter;
            }
        }

        private void Update()
        {
            if (regions != null)
            {
                int layer = LayerMask.NameToLayer("Regions");
                foreach (Region region in regions.Values)
                    Graphics.DrawMesh(region.Mesh, Vector3.zero, Quaternion.identity, RegionMaterial(region.RegionID), layer, null, 0, null, false, false);
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (ScreenCast.MouseTerrain.Cast(out World.Tile mouse))
                {
                    RegionManager.SetRegion(placingRegionID, mouse.Position.x, mouse.Position.y);
                }
            }
            if (Input.GetMouseButtonDown(1))
            {
                if (ScreenCast.MouseTerrain.Cast(out World.Tile mouse))
                {
                    RegionManager.SetRegion(0, mouse.Position.x, mouse.Position.y);
                }
            }

        }

        public static void Clear()
        {
            regions = new Dictionary<int, Region>();
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
        public static bool SetRegion(int regionID, int x, int y)
        {
            if (regions == null)
                return false;

            Tile tile = Map.Instance[x, y];
            if (tile == null)
                return false;

            if (tile.RegionID == regionID)
                return false;

            if (tile.ContigRegionID != 0)
            {
                Region existing = regions[tile.ContigRegionID];
                if (existing != null && existing.RemoveMember(tile, out List<Region> newRegions))
                {
                    if (existing.Size <= 0)
                        regions.Remove(tile.ContigRegionID);

                    foreach (Region newRegion in newRegions)
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

                Region region = null;

                foreach (Tile neighbour in tile.Neighbours)
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
                                Region toJoin = regions[neighbourID];
                                if (region.JoinRegion(toJoin))
                                    regions.Remove(neighbourID);
                            }
                        }
                        else
                        {
                            Region neighbourRegion = regions[neighbour.ContigRegionID];
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
                    region = new Region(regionID, NewRegionID);
                    region.AddMember(tile);
                    regions.Add(region.ContigRegionID, region);
                }

            }
            return true;
        }
    }
}
