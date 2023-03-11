using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorGenerator 
{
    ColorSettings settings;
    Texture2D texture;
    const int textureResolution = 50;
    INoiseFilter biomeNoiseFilter;
    public void UpdateSettings(ColorSettings settings)
    {
        this.settings = settings;
        if(texture == null || texture.height != settings.biomeColorSettings.biomes.Length)
            texture = new(textureResolution * 2, settings.biomeColorSettings.biomes.Length, TextureFormat.RGBA32,false );
        biomeNoiseFilter = NoiseFilterFactory.CreateNoiseFilter(settings.biomeColorSettings.noise);
        
    }
    public void UpdateElevation(MinMax elevationMinMax)
    {
        settings.planetMat.SetVector("_elevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max));
    }
    public float BiomePercentFromPoint(Vector3 pointOnSphere)
    {
        float heightPersent = (pointOnSphere.y + 1) / 2f;
        heightPersent += (biomeNoiseFilter.Evaluate(pointOnSphere) - settings.biomeColorSettings.noiseOffset) * settings.biomeColorSettings.noiseStrength;
        float biomeIndex = 0;
        int numBiomes = settings.biomeColorSettings.biomes.Length;
        float blendRange = settings.biomeColorSettings.blendAmount / 2+.001f;

        for (int i = 0; i < numBiomes; i++)
        {
            float dist = heightPersent - settings.biomeColorSettings.biomes[i].startHeight;
            float weight = Mathf.InverseLerp(-blendRange, blendRange, dist);
            biomeIndex *= (1 - weight);
            biomeIndex += i * weight;

        }

        return biomeIndex/Mathf.Max(1,(numBiomes-1));
    }
    public void UpdateColors()
    {
        Color32[] colours = new Color32[texture.width * texture.height];
        int colourIndex = 0;
        foreach (var biome in settings.biomeColorSettings.biomes)
        {
            for (int i = 0; i < textureResolution * 2; i++)
            {
                Color gradientCol;
                if (i < textureResolution)
                {
                    gradientCol = settings.oceanColor.Evaluate(i / (textureResolution - 1f));
                }
                else
                {
                    gradientCol = biome.gradient.Evaluate((i - textureResolution) / (textureResolution - 1f));
                }
                Color tintCol = biome.tint;
                colours[colourIndex] = gradientCol * (1 - biome.tintPercent) + tintCol * biome.tintPercent;
                colourIndex++;
            }
        }
        texture.SetPixels32(colours);
        texture.Apply();
        settings.planetMat.SetTexture("_texture", texture);
    }
}
