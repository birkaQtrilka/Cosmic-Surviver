using System;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase, ExecuteAlways]
public class Planet : MonoBehaviour
{
    public static List<Planet> ActivePlanets = new();

    private const string CombinedOceanName = "Combined Ocean";
    private const string TerrainMeshName = "terrainMesh";
    private const string OceanMeshName = "oceanMesh";
    readonly Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

    [Range(2, 256)]
    public int resolution = 10;
    public bool autoUpdate = true;

    [SerializeField, Range(0, 1)]
    float _oceanLevel = 0.2f;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    public AtmosphereSettings atmosphereSettings;
    public bool isLightSource;

    public bool HasOceanMesh = true;
    public bool autoSaveTexture = true;

    [HideInInspector] public bool shapeSettingsFoldout;
    [HideInInspector] public bool colourSettingsFoldout;
    [HideInInspector] public bool atmosphereSettingsFoldout;

    readonly ShapeGenerator shapeGenerator = new();
    readonly ColorGenerator colorGenerator = new();

    [SerializeField] MeshFilter[] terrainMeshFilters;
    [SerializeField] MeshFilter combinedMesh;
    [SerializeField, HideInInspector] MeshFilter[] oceanMeshFilters;

    TerrainFace[] terrainFaces;
    OceanFace[] oceanFaces;

    public bool IsActiveOceanMesh => combinedMesh != null && combinedMesh.gameObject.activeSelf;

    public bool IsActivePlanetMesh => terrainMeshFilters != null && terrainMeshFilters.Length > 0 && terrainMeshFilters[0] != null && terrainMeshFilters[0].gameObject.activeSelf;

    public TerrainFace[] TerrainFaces => terrainFaces;
    public OceanFace[] OceanFaces => oceanFaces;
    public ShapeGenerator ShapeGenerator => shapeGenerator;

    [SerializeField] bool _toggleAtmosphere;

    [SerializeField, ReadOnly] bool _atmosphereActive = true;

    private void OnValidate()
    {
        if (_toggleAtmosphere)
        {
            _toggleAtmosphere = false;
            _atmosphereActive = !_atmosphereActive;
            if (_atmosphereActive && !ActivePlanets.Contains(this)) ActivePlanets.Add(this);
            else if (!_atmosphereActive) ActivePlanets.Remove(this);

#if UNITY_EDITOR
            // Queue a player loop update (Game View)
            if (!Application.isPlaying)
            {
                UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                // Force Scene View repaint
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }
    }

    public void SaveColorTexture() => colorGenerator.SaveTexture();

    public List<MeshFilter> GetAllMeshFilters()
    {
        List<MeshFilter> meshFilters = new(terrainMeshFilters);
        if(!HasOceanMesh) return meshFilters;
        meshFilters.Add(combinedMesh);
        return meshFilters;
    }

    void Initialize()
    {
        // Execute initialization while at Vector3.zero to ensure local/world space consistency
        RunActionAtZeroPosition(() =>
        {
            shapeGenerator.UpdateSettings(shapeSettings);
            colorGenerator.UpdateSettings(colorSettings);

            if (terrainMeshFilters == null || terrainMeshFilters.Length == 0)
                terrainMeshFilters = new MeshFilter[6];

            terrainFaces = new TerrainFace[6];

            if (HasOceanMesh)
            {
                if (oceanMeshFilters == null || oceanMeshFilters.Length == 0)
                    oceanMeshFilters = new MeshFilter[6];
                if (oceanFaces == null || oceanFaces.Length == 0)
                    oceanFaces = new OceanFace[6];
                combinedMesh = SetupMeshObject(combinedMesh, CombinedOceanName, colorSettings.oceanMat, false);

            }

            for (int i = 0; i < 6; i++)
            {
                // Setup Terrain
                terrainMeshFilters[i] = SetupMeshObject(terrainMeshFilters[i], TerrainMeshName, colorSettings.planetMat, true);
                terrainFaces[i] = new TerrainFace(shapeGenerator, terrainMeshFilters[i].sharedMesh, resolution, directions[i], _oceanLevel);

                // Setup Ocean
                if (!HasOceanMesh) continue;
                oceanFaces[i] ??= new OceanFace();
                oceanMeshFilters[i] = SetupMeshObject(oceanMeshFilters[i], OceanMeshName, colorSettings.oceanMat, false);
                oceanFaces[i].Initialize(oceanMeshFilters[i].sharedMesh, terrainFaces[i], resolution);
            }
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif

        });
    }

    /// <summary>
    /// Helper to create or update the mesh game objects to reduce duplication.
    /// </summary>
    MeshFilter SetupMeshObject(MeshFilter existingFilter, string objName, Material material, bool hasCollider)
    {
        MeshCollider meshCollider = null;
        if (existingFilter == null)
        {
            GameObject meshObj = new(objName);
            meshObj.transform.parent = transform;
            meshObj.AddComponent<MeshRenderer>();
            if (hasCollider) 
            {
                meshCollider = meshObj.AddComponent<MeshCollider>();
            }
            existingFilter = meshObj.AddComponent<MeshFilter>();
            existingFilter.sharedMesh = new Mesh();
        }

        existingFilter.GetComponent<MeshRenderer>().sharedMaterial = material;
        if(hasCollider)
        {
            if (meshCollider == null) meshCollider = existingFilter.GetComponent<MeshCollider>();
            meshCollider.sharedMesh = existingFilter.sharedMesh;
        }

// Reset local position just in case
existingFilter.transform.localPosition = Vector3.zero;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(existingFilter);
        UnityEditor.EditorUtility.SetDirty(existingFilter.gameObject);
#endif
        return existingFilter;
    }

    public void EmptyMeshFilterCache()
    {
        for (int i = 0; i < 6; i++)
        {
            if(oceanMeshFilters.Length > i) oceanMeshFilters[i] = null;
            if (terrainMeshFilters.Length > i) terrainMeshFilters[i] = null;
        }

        combinedMesh = null;
    }

    public void SetActiveOceanMesh(bool active)
    {
        if (combinedMesh == null) return;
        combinedMesh.gameObject.SetActive(active);
    }

    public void SetActivePlanetMesh(bool active)
    {
        if (terrainMeshFilters == null) return;
        foreach (var filter in terrainMeshFilters)
        {
            if (filter != null) filter.gameObject.SetActive(active);
        }
    }

    void GenerateMesh()
    {
        RunActionAtZeroPosition(() =>
        {
            for (int i = 0; i < 6; i++)
            {
                if (terrainMeshFilters[i].gameObject.activeSelf)
                    terrainFaces[i].ConstructMesh();
            }

            colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);

            if (!HasOceanMesh) return;
            for (int i = 0; i < 6; i++)
            {
                if (oceanMeshFilters[i].gameObject.activeSelf)
                    oceanFaces[i].ConstructMesh();
            }
        });

        if (HasOceanMesh)
        {
            WeldOceanMesh();
        }
    }

    /// <summary>
    /// Temporarily moves the planet to (0,0,0), runs the action, and moves it back.
    /// Needed because procedural mesh generation often relies on world space calculations behaving like local space.
    /// </summary>
    void RunActionAtZeroPosition(System.Action action)
    {
        var oldPos = transform.position;
        transform.position = Vector3.zero;
        try
        {
            action?.Invoke();
        }
        finally
        {
            transform.position = oldPos;
        }
    }

    public void WeldOceanMesh()
    {
        // Ensure we have filters to weld
        if (oceanMeshFilters == null || oceanMeshFilters.Length == 0 || oceanMeshFilters[0] == null) return;

        Mesh combinedMesh = MeshWelder.CombineMeshes(oceanMeshFilters, transform);
        Mesh weldedMesh = MeshWelder.WeldMeshes(combinedMesh, oceanFaces, resolution);

        this.combinedMesh.sharedMesh = weldedMesh;

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    void GenerateColours()
    {
        for (int i = 0; i < 6; i++)
        {
            if (terrainMeshFilters[i].gameObject.activeSelf)
                terrainFaces[i].UpdateUVs(colorGenerator);
        }
        colorGenerator.UpdateColors();
    }

    public void GeneratePlanet()
    {
        Initialize();
        GenerateMesh();
        GenerateColours();

        if (autoSaveTexture)
        {
            SaveColorTexture();
        }
    }

    public void OnShapeSettingsUpdate()
    {
        if (!autoUpdate) return;
        Initialize();
        GenerateMesh();
    }

    public void OnColourSettingsUpdated()
    {
        if (!autoUpdate) return;
        Initialize();
        GenerateColours();
    }

    private void OnEnable()
    {
        if (!ActivePlanets.Contains(this))
        {
            ActivePlanets.Add(this);
        }
    }

    private void OnDisable()
    {
        ActivePlanets.Remove(this);
    }
}