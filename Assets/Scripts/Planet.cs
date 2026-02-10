using UnityEngine;

[SelectionBase]
public class Planet : MonoBehaviour
{

    public static Transform player;

    [Range(2, 256)]
    public int resolution = 10;
    public bool autoUpdate = true;
    [SerializeField, Range(0,1)] float _oceanLevel = 0.2f;
    public enum FaceRenderMask
    {
        All,Top,Bottom,Left,Right,Fron,Back
    }
    public FaceRenderMask faceRenderMask = FaceRenderMask.All;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    public bool HasOceanMesh = true;
    public bool autoSaveTexture = true;

    [HideInInspector]
    public bool shapeSettingsFoldout;
    [HideInInspector]
    public bool colourSettingsFoldout;
    
    ShapeGenerator shapeGenerator= new();
    ColorGenerator colorGenerator= new();

    [SerializeField, HideInInspector] 
    MeshFilter[] meshFilters;
    TerrainFace[] terrainFaces;

    [SerializeField, HideInInspector]
    MeshFilter[] oMeshFilters;
    [SerializeField,HideInInspector]
    OceanFace[] oceanFaces;
    Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

    public bool IsActiveOceanMesh
    {
        get => GetCombinedMesh().activeSelf;// (oMeshFilters != null && oMeshFilters.Length > 0 && oMeshFilters[0] != null && oMeshFilters[0].gameObject.activeSelf);
    }

    public bool IsActivePlanetMesh
    {
        get => meshFilters != null && meshFilters.Length > 0 && meshFilters[0] != null && meshFilters[0].gameObject.activeSelf;
    }
    
    public TerrainFace[] TerrainFaces => terrainFaces;
    public OceanFace[] OceanFaces => oceanFaces;
    public ShapeGenerator ShapeGenerator => shapeGenerator;

    public void SaveColorTexture()
    {
        colorGenerator.SaveTexture();
    }
    
    GameObject GetCombinedMesh()
    {
        var found = transform.Find("Combined Ocean");
        if (found)
        {
            return found.gameObject;
        }
        return null;
    }

    void Initialize()
    {
        DestroyImmediate(GetCombinedMesh());

        var oldPos = transform.position;
        transform.position = Vector3.zero;

        shapeGenerator.UpdateSettings(shapeSettings) ;
        colorGenerator.UpdateSettings(colorSettings);
        
        if (meshFilters == null || meshFilters.Length == 0)
            meshFilters = new MeshFilter[6];
        
        if(HasOceanMesh)
        {
            if (oMeshFilters == null || oMeshFilters.Length == 0)
                oMeshFilters = new MeshFilter[6];
            if (oceanFaces == null || oceanFaces.Length == 0)
                oceanFaces = new OceanFace[6];
        }
        
        terrainFaces = new TerrainFace[6];


        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new("mesh");
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>();
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshFilters[i].sharedMesh = new Mesh();


            }

            if (HasOceanMesh && oMeshFilters[i] == null)
            {
                GameObject meshObj = new("oceanMesh");
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>();
                oMeshFilters[i] = meshObj.AddComponent<MeshFilter>();
                oMeshFilters[i].sharedMesh = new Mesh();
            }
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;

            meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.planetMat;
            terrainFaces[i] = new(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i], _oceanLevel);
            meshFilters[i].gameObject.SetActive(renderFace);
            if (!HasOceanMesh) continue;

            oceanFaces[i] ??= new();
            oMeshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.oceanMat;
            oceanFaces[i].Initialize(oMeshFilters[i].sharedMesh, terrainFaces[i], resolution );
            oMeshFilters[i].gameObject.SetActive(renderFace);
        }
        transform.position = oldPos;

    }

    public void SetActiveOceanMesh(bool active)
    {
        //if (oMeshFilters == null || oMeshFilters.Length == 0 || oMeshFilters[0] == null) return;
        //for (int i = 0; i < 6; i++)
        //    oMeshFilters[i].gameObject.SetActive(active);
        GetCombinedMesh()?.SetActive(active);

    }

    public void SetActivePlanetMesh(bool active)
    {
        if (meshFilters == null || meshFilters.Length == 0 || meshFilters[0] == null) return;
        for (int i = 0; i < 6; i++)
            meshFilters[i].gameObject.SetActive(active);
    }

    void GenerateMesh()
    {
        var oldPos = transform.position;
        transform.position = Vector3.zero;
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
                terrainFaces[i].ConstructMesh();
            meshFilters[i].transform.position = Vector3.zero;
        }

        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);

        if (!HasOceanMesh) return;

        for (int i = 0; i < 6; i++)
            if (oMeshFilters[i].gameObject.activeSelf)
            {
                oceanFaces[i].ConstructMesh();
                oMeshFilters[i].transform.position = Vector3.zero;

            }
        transform.position = oldPos;

        WeldOceanMesh();
    }

    public void WeldOceanMesh()
    {
        Mesh combinedMesh = MeshWelder.CombineMeshes(oMeshFilters, transform);
        Mesh weldedMesh = MeshWelder.WeldMeshes(combinedMesh, oceanFaces, resolution);

        GameObject oceanObj = new("Combined Ocean");
        oceanObj.transform.SetParent(transform);
        oceanObj.transform.localPosition = Vector3.zero;
        oceanObj.transform.localRotation = Quaternion.identity;
        oceanObj.transform.localScale = Vector3.one;

        MeshFilter filter = oceanObj.AddComponent<MeshFilter>();
        filter.sharedMesh = weldedMesh;

        MeshRenderer renderer = oceanObj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = oMeshFilters[0].GetComponent<MeshRenderer>().sharedMaterial;
    }

    void OnValidate()
    {
        if (player == null)
            player = Camera.main.transform;
        
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
