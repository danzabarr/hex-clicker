using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Noise
{
    public static class NoiseUtils
    {
        public static readonly int Offset = 0;

        public static Vector2Int Mesh2Chunk(int meshX, int meshZ, int resolution)
        {
            if (meshX < -1) meshX++;
            if (meshZ < -1) meshZ++;
            int chunkX = meshX / resolution;
            int chunkZ = meshZ / resolution;
            if (meshX < 0) chunkX--;
            if (meshZ < 0) chunkZ--;
            return new Vector2Int(chunkX, chunkZ);
        }
    }
}
