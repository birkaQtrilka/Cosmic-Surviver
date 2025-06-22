using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu()]
public class ColorSettings : ScriptableObject
{
    public Material planetMat;
    public Material oceanMat;
    public BiomeColorSettings biomeColorSettings;
    public Gradient oceanColor;

    [System.Serializable]
    public class BiomeColorSettings
    {
        public Biome[] biomes;
        public NoiseSettings noise;
        public float noiseOffset;
        public float noiseStrength;
        [Range(0,1)]
        public float blendAmount;
        [System.Serializable]
        public class Biome
        {
            public string name;
            public Gradient gradient;
            public Color tint;
            [Range(0,1)]
            public float startHeight;
            [Range(0,1)]
            public float tintPercent;
        }
    }
    public void MaximizeAllTints(bool flag)
    {
        foreach (var biome in biomeColorSettings.biomes)
        {
            biome.tintPercent = flag ? 1 : 0;//later store the actual tint value
        }
    }

}
