using UnityEngine;

[System.Serializable]
public struct BiomeTintSnapshot
{
    public float[] tintPercent;
}


[CreateAssetMenu()]
public class ColorSettings : ScriptableObject
{
    public Material planetMat;
    public Material oceanMat;
    public BiomeColorSettings biomeColorSettings;
    public Gradient oceanColor;

    public BiomeTintSnapshot cachedTints;
    [HideInInspector] 
    public bool cleared;
    [System.Serializable]
    public class BiomeColorSettings
    {
        public Biome[] biomes;
        public NoiseSettings noise;
        public float noiseOffset;
        public float noiseStrength;
        [Range(0, 1)]
        public float blendAmount;
        [System.Serializable]
        public class Biome
        {
            public string name;
            public Gradient gradient;
            public Color tint;
            [Range(0, 1)]
            public float startHeight;
            [Range(0, 1)]
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

    public void CacheTintState()
    {
        cachedTints.tintPercent = new float[biomeColorSettings.biomes.Length];
        for (int i = 0; i < biomeColorSettings.biomes.Length; i++)
        {
            cachedTints.tintPercent[i] = biomeColorSettings.biomes[i].tintPercent;
        }
    }

    public void RestoreTintState()
    {
        if (cachedTints.tintPercent == null || biomeColorSettings == null)
        {
            Debug.LogWarning("No cached tints to restore.");
            return;
        }
        if(cachedTints.tintPercent.Length != biomeColorSettings.biomes.Length)
        {
            cachedTints.tintPercent = new float[biomeColorSettings.biomes.Length];
        }
        for (int i = 0; i < biomeColorSettings.biomes.Length; i++)
        {
            biomeColorSettings.biomes[i].tintPercent = cachedTints.tintPercent[i];
        }
    }
}
