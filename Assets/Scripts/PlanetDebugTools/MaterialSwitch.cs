using System;
using UnityEngine;
[Serializable]
public struct SwitchData
{
    public string name;
    public Material oceanMat;
    public Material planetMat;
}

[ExecuteInEditMode]
public class MaterialSwitch : MonoBehaviour
{
    public SwitchData[] switches;

    private Planet planet;
    public Planet Planet
    {
        get
        {
            if (planet == null)
            {
                planet = GetComponent<Planet>();
            }
            return planet;
        }
    }

    public void ApplySwitch(SwitchData data)
    {
        if (Planet == null) return;

        if(data.oceanMat != null)
        {
            Planet.colorSettings.planetMat = data.planetMat;
        }
        if(data.oceanMat != null)
        {
            Planet.colorSettings.oceanMat = data.oceanMat;
        }
        Planet.GeneratePlanet();
    }
}
