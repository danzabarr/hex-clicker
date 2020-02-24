using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    [System.Serializable]
    public struct NoiseSettings
    {
        [SerializeField]
        internal float frequency;
        [SerializeField]
        [Range(1, 8)]
        internal int octaves;
        [SerializeField]
        [Range(1f, 4f)]
        internal float lacunarity;
        [SerializeField]
        [Range(0f, 1f)]
        internal float persistence;
        public NoiseSettings(float frequency, int octaves, float lacunarity, float persistence)
        {
            this.frequency = frequency;
            this.octaves = octaves;
            this.lacunarity = lacunarity;
            this.persistence = persistence;
        }
    }

    private static readonly int NOISE_OFFSET = 0;//54614;

    public static float[] GenerateHeightMapPerlin(int seed, int chunkX, int chunkZ, float offsetX, float offsetY, int resolution, NoiseSettings settings, AnimationCurve curve = null)
    {
        //SimplexNoise noise = new SimplexNoise(seed);

        Random.InitState(seed);

        offsetX += Random.value;
        offsetY += Random.value;

        float[] heightMap = new float[(resolution + 1) * (resolution + 1)];
        float stepSize = 1f / resolution;
        for (int y = 0; y < resolution + 1; y++)
        {
            for (int x = 0; x < resolution + 1; x++)
            {

                Vector2Int chunk = Mesh2Chunk(chunkX * resolution + x, chunkZ * resolution + y, resolution);

                int cX = chunk.x - NOISE_OFFSET;
                int cZ = chunk.y - NOISE_OFFSET;

                Vector2 point00 = new Vector3(cX + 0, cZ + 0);
                Vector2 point10 = new Vector3(cX + 1, cZ + 0);
                Vector2 point01 = new Vector3(cX + 0, cZ + 1);
                Vector2 point11 = new Vector3(cX + 1, cZ + 1);

                Vector2 point0 = Vector3.Lerp(point00, point01, (y % resolution) * stepSize);
                Vector2 point1 = Vector3.Lerp(point10, point11, (y % resolution) * stepSize);

                Vector2 point = Vector3.Lerp(point0, point1, (x % resolution) * stepSize);

                //float sample = Simplex(noise, point.x, point.y, 0, settings);
                float sample = Perlin(point.x + offsetX, point.y + offsetY, settings);

                if (curve != null) sample = curve.Evaluate(sample);

                heightMap[x + y * (resolution + 1)] = sample;
            }
        }
        return heightMap;
    }

    public static float[] GenerateHeightMapSimplex(int seed, int chunkX, int chunkZ, float offsetX, float offsetY, int resolution, NoiseSettings settings, AnimationCurve curve = null)
    {
        SimplexNoise noise = new SimplexNoise(seed + "");

        float[] heightMap = new float[(resolution + 1) * (resolution + 1)];
        float stepSize = 1f / resolution;
        for (int y = 0; y < resolution + 1; y++)
        {
            for (int x = 0; x < resolution + 1; x++)
            {

                Vector2Int chunk = Mesh2Chunk(chunkX * resolution + x, chunkZ * resolution + y, resolution);

                int cX = chunk.x - NOISE_OFFSET;
                int cZ = chunk.y - NOISE_OFFSET;

                Vector2 point00 = new Vector3(cX + 0, cZ + 0);
                Vector2 point10 = new Vector3(cX + 1, cZ + 0);
                Vector2 point01 = new Vector3(cX + 0, cZ + 1);
                Vector2 point11 = new Vector3(cX + 1, cZ + 1);

                Vector2 point0 = Vector3.Lerp(point00, point01, (y % resolution) * stepSize);
                Vector2 point1 = Vector3.Lerp(point10, point11, (y % resolution) * stepSize);

                Vector2 point = Vector3.Lerp(point0, point1, (x % resolution) * stepSize);

                float sample = Simplex(noise, point.x, point.y, 0, settings);
                //float sample = Perlin(point.x + offsetX, point.y + offsetY, settings);

                if (curve != null) sample = curve.Evaluate(sample);

                heightMap[x + y * (resolution + 1)] = sample;
            }
        }
        return heightMap;
    }

    //Converts mesh x/z to chunk coordinates (static, variable resolution version)
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
    public static float Simplex(SimplexNoise noise, float x, float y, float z, NoiseSettings settings)
    {
        return Simplex(noise, x, y, z, settings.frequency, settings.octaves, settings.lacunarity, settings.persistence);
    }
    public static float Simplex(SimplexNoise noise, float x, float y, float z, float frequency, int octaves, float lacunarity, float persistence)
    {
        float sum = noise.noise(x * frequency, y * frequency, z * frequency);
        float amplitude = 1f;
        float range = 1f;
        for (int o = 1; o < octaves; o++)
        {
            frequency *= lacunarity;
            amplitude *= persistence;
            range += amplitude;
            sum += noise.noise(x * frequency, y * frequency, z * frequency) * amplitude;
        }
        return sum / range;
    }

    public static float Perlin(float x, float y, NoiseSettings settings)
    {
        return Perlin(x, y, settings.frequency, settings.octaves, settings.lacunarity, settings.persistence);
    }

    public static float Perlin(float x, float y, float frequency, int octaves, float lacunarity, float persistence)
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
