using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColorGenerator 
{
    ColorSettings settings;
    Texture2D texture;
    const int textureResolution = 50;
    public void UpdateSettings(ColorSettings settings)
    {
        this.settings = settings;
        if(texture == null )
            texture = new(textureResolution, 1);
    }
    public void UpdateElevation(MinMax elevationMinMax)
    {
        settings.planetMat.SetVector("_elevationMinMax", new Vector4(elevationMinMax.Min, elevationMinMax.Max));
    }
    public void UpdateColors()
    {
        Color32[] colors = new Color32[textureResolution] ;
        for (int i = 0; i < textureResolution; i++)
        {
            colors[i] = settings.gradient.Evaluate(i/(textureResolution-1f));
        }
        texture.SetPixels32(colors);
        texture.Apply();
        settings.planetMat.SetTexture("_texture", texture);
    }
}
