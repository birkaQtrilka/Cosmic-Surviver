using System;
using UnityEngine;
[Serializable]
public struct SwitchData
{
    public string name;
    public Material oceanMat;
    public Material planetMat;
    public bool activeOceanMesh;
    public bool activePlanetMesh;
}

[ExecuteInEditMode]
public class MaterialSwitch : MonoBehaviour
{
    public SwitchData[] switches;
    [SerializeField, HideInInspector] int lastSwitchIndex = -1;

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

    public void ApplySwitch(int index)
    {
        if (index >= 0 && index < switches.Length)
        {
            ApplySwitch(switches[index]);
            lastSwitchIndex = index;
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
        Planet.SetActiveOceanMesh(data.activeOceanMesh);
        Planet.SetActivePlanetMesh(data.activePlanetMesh);
    }

    public void ApplyLastSwitch()
    {
        if (lastSwitchIndex >= 0 && lastSwitchIndex < switches.Length)
        {
            ApplySwitch(switches[lastSwitchIndex]);
        }
    }
}
