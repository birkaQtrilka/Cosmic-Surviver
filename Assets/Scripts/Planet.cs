using UnityEngine;

[SelectionBase]
public class Planet : MonoBehaviour
{
    private const string CombinedOceanName = "Combined Ocean";
    private const string TerrainMeshName = "terrainMesh";
    private const string OceanMeshName = "oceanMesh";

    public static Transform player;

    [Range(2, 256)]
    public int resolution = 10;
    public bool autoUpdate = true;

    [SerializeField, Range(0, 1)]
    float _oceanLevel = 0.2f;

    public enum FaceRenderMask
    {
        All, Top, Bottom, Left, Right, Front, Back
    }
    public FaceRenderMask faceRenderMask = FaceRenderMask.All;

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

    [SerializeField, HideInInspector] MeshFilter[] meshFilters;
    [SerializeField, HideInInspector] MeshFilter[] oceanMeshFilters;

    TerrainFace[] terrainFaces;
    OceanFace[] oceanFaces;

    readonly Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

    public bool IsActiveOceanMesh => GetCombinedMesh()?.activeSelf ?? false;

    public bool IsActivePlanetMesh => meshFilters != null && meshFilters.Length > 0 && meshFilters[0] != null && meshFilters[0].gameObject.activeSelf;

    public TerrainFace[] TerrainFaces => terrainFaces;
    public OceanFace[] OceanFaces => oceanFaces;
    public ShapeGenerator ShapeGenerator => shapeGenerator;

    public void SaveColorTexture() => colorGenerator.SaveTexture();

    GameObject GetCombinedMesh()
    {
        var found = transform.Find(CombinedOceanName);
        return found ? found.gameObject : null;
    }

    void Initialize()
    {
        // Execute initialization while at Vector3.zero to ensure local/world space consistency
        RunActionAtZeroPosition(() =>
        {
            DestroyImmediate(GetCombinedMesh());

            shapeGenerator.UpdateSettings(shapeSettings);
            colorGenerator.UpdateSettings(colorSettings);

            if (meshFilters == null || meshFilters.Length == 0)
                meshFilters = new MeshFilter[6];

            terrainFaces = new TerrainFace[6];

            if (HasOceanMesh)
            {
                if (oceanMeshFilters == null || oceanMeshFilters.Length == 0)
                    oceanMeshFilters = new MeshFilter[6];
                if (oceanFaces == null || oceanFaces.Length == 0)
                    oceanFaces = new OceanFace[6];
            }

            for (int i = 0; i < 6; i++)
            {
                bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;

                // Setup Terrain
                meshFilters[i] = SetupMeshObject(meshFilters[i], TerrainMeshName, colorSettings.planetMat, renderFace);
                terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i], _oceanLevel);

                // Setup Ocean
                if (!HasOceanMesh) continue;
                oceanFaces[i] ??= new OceanFace();
                oceanMeshFilters[i] = SetupMeshObject(oceanMeshFilters[i], OceanMeshName, colorSettings.oceanMat, renderFace);
                oceanFaces[i].Initialize(oceanMeshFilters[i].sharedMesh, terrainFaces[i], resolution);
            }
        });
    }

    /// <summary>
    /// Helper to create or update the mesh game objects to reduce duplication.
    /// </summary>
    MeshFilter SetupMeshObject(MeshFilter existingFilter, string objName, Material material, bool isActive)
    {
        if (existingFilter == null)
        {
            GameObject meshObj = new(objName);
            meshObj.transform.parent = transform;
            meshObj.AddComponent<MeshRenderer>();
            existingFilter = meshObj.AddComponent<MeshFilter>();
            existingFilter.sharedMesh = new Mesh();
        }

        existingFilter.GetComponent<MeshRenderer>().sharedMaterial = material;
        existingFilter.gameObject.SetActive(isActive);

        // Reset local position just in case
        existingFilter.transform.localPosition = Vector3.zero;

        return existingFilter;
    }

    public void SetActiveOceanMesh(bool active) => GetCombinedMesh()?.SetActive(active);

    public void SetActivePlanetMesh(bool active)
    {
        if (meshFilters == null) return;
        foreach (var filter in meshFilters)
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
                if (meshFilters[i].gameObject.activeSelf)
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

        Material oceanMaterial = oceanMeshFilters[0].GetComponent<MeshRenderer>().sharedMaterial;
        Mesh combinedMesh = MeshWelder.CombineMeshes(oceanMeshFilters, transform);
        Mesh weldedMesh = MeshWelder.WeldMeshes(combinedMesh, oceanFaces, resolution);

        GameObject oceanObj = new(CombinedOceanName);
        oceanObj.transform.SetParent(transform);
        oceanObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        oceanObj.transform.localScale = Vector3.one;

        MeshFilter filter = oceanObj.AddComponent<MeshFilter>();
        filter.sharedMesh = weldedMesh;

        MeshRenderer renderer = oceanObj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = oceanMaterial;
    }

    void OnValidate()
    {
        if (player == null)
            player = Camera.main != null ? Camera.main.transform : null;
    }

    void GenerateColours()
    {
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
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
}