using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteAlways]
public class PlanetAtmosphere : MonoBehaviour
{
    public static List<PlanetAtmosphere> ActiveAtmospheres = new();
    
    public ShapeSettings shapeSettings;
    public AtmosphereSettings atmosphereSettings;
    public bool isLightSource;

    [SerializeField] bool _toggleAtmosphere;
    [SerializeField, ReadOnly] bool _atmosphereActive = true;
    [HideInInspector] public bool atmosphereSettingsFoldout;


    private void OnValidate()
    {
        if (_toggleAtmosphere)
        {
            _toggleAtmosphere = false;
            _atmosphereActive = !_atmosphereActive;
            if (_atmosphereActive && !ActiveAtmospheres.Contains(this)) ActiveAtmospheres.Add(this);
            else if (!_atmosphereActive) ActiveAtmospheres.Remove(this);


        }
    }

    private void OnEnable()
    {
        if (!ActiveAtmospheres.Contains(this))
        {
            ActiveAtmospheres.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveAtmospheres.Remove(this);
        UnityEditor.EditorUtility.SetDirty(this);
    }
}
