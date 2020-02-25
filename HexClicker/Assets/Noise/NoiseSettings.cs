using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HexClicker.Noise
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
}
