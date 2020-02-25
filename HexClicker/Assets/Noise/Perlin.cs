using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Noise
{
    public static class Perlin
    {
        public static float[] HeightMap(int seed, int chunkX, int chunkZ, float offsetX, float offsetY, int resolution, NoiseSettings settings, AnimationCurve curve = null)
        {
            Random.InitState(seed);

            offsetX += Random.value;
            offsetY += Random.value;

            float[] heightMap = new float[(resolution + 1) * (resolution + 1)];
            float stepSize = 1f / resolution;
            for (int y = 0; y < resolution + 1; y++)
            {
                for (int x = 0; x < resolution + 1; x++)
                {

                    Vector2Int chunk = NoiseUtils.Mesh2Chunk(chunkX * resolution + x, chunkZ * resolution + y, resolution);

                    int cX = chunk.x - NoiseUtils.Offset;
                    int cZ = chunk.y - NoiseUtils.Offset;

                    Vector2 point00 = new Vector3(cX + 0, cZ + 0);
                    Vector2 point10 = new Vector3(cX + 1, cZ + 0);
                    Vector2 point01 = new Vector3(cX + 0, cZ + 1);
                    Vector2 point11 = new Vector3(cX + 1, cZ + 1);

                    Vector2 point0 = Vector3.Lerp(point00, point01, (y % resolution) * stepSize);
                    Vector2 point1 = Vector3.Lerp(point10, point11, (y % resolution) * stepSize);

                    Vector2 point = Vector3.Lerp(point0, point1, (x % resolution) * stepSize);

                    //float sample = Simplex(noise, point.x, point.y, 0, settings);
                    float sample = Noise(point.x + offsetX, point.y + offsetY, settings);

                    if (curve != null) sample = curve.Evaluate(sample);

                    heightMap[x + y * (resolution + 1)] = sample;
                }
            }
            return heightMap;
        }

        public static float Noise(float x, float y, NoiseSettings settings)
        {
            return Noise(x, y, settings.frequency, settings.octaves, settings.lacunarity, settings.persistence);
        }

        public static float Noise(float x, float y, float frequency, int octaves, float lacunarity, float persistence)
        {
            float sum = Mathf.PerlinNoise(x * frequency, y * frequency);
            float amplitude = 1f;
            float range = 1f;
            for (int o = 1; o < octaves; o++)
            {
                frequency *= lacunarity;
                amplitude *= persistence;
                range += amplitude;
                sum += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            }
            return sum / range;
        }
    }
}
