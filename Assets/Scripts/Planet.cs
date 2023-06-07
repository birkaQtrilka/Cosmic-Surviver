using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    //public static float size = 10;
    public static Transform player;

    public readonly static Dictionary<int, float> detailLevelDistances = new()
    {
        { 0, Mathf.Infinity},
        { 1, 60f},
        { 2, 25f},
        { 3, 10f},
        { 4, 4f},
        { 5, 1.5f},
        { 6, .7f},
        { 7, .3f},
        { 8, .1f},
    };
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        Initialize();
        GenerateMesh();

        StartCoroutine(PlayerGenerationLoop());

    }
    void OnValidate()
    {
        if (player == null)
            player = Camera.main.transform;
    }
    IEnumerator PlayerGenerationLoop()
    {
        var wait = new WaitForSeconds(1);
        while (true)
        {
            yield return wait;
            GenerateMesh();
        }
    }
    [Range(2, 256)]
    public int resolution = 10;
    public bool autoUpdate = true;
    public float oceanLevelRiser = 0.2f;
    public enum FaceRenderMask
    {
        All,Top,Bottom,Left,Right,Fron,Back
    }
    public FaceRenderMask faceRenderMask = FaceRenderMask.All;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
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
    
    
    void Initialize()
    {
        shapeGenerator.UpdateSettings(shapeSettings) ;
        colorGenerator.UpdateSettings(colorSettings);
        
        if (meshFilters == null || meshFilters.Length == 0)
            meshFilters = new MeshFilter[6];
        if (oMeshFilters == null || oMeshFilters.Length == 0)
            oMeshFilters = new MeshFilter[6];
        if (oceanFaces == null || oceanFaces.Length == 0)
            oceanFaces = new OceanFace[6];
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

            if (oMeshFilters[i] == null)
            {
                GameObject meshObj = new("oceanMesh");
                meshObj.transform.parent = transform;

                meshObj.AddComponent<MeshRenderer>();
                oMeshFilters[i] = meshObj.AddComponent<MeshFilter>();
                oMeshFilters[i].sharedMesh = new Mesh();
            }
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;

            meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.planetMat;
            terrainFaces[i] = new(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i], shapeSettings.planetRadius);
            meshFilters[i].gameObject.SetActive(renderFace);

            oceanFaces[i] ??= new();
            oMeshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.oceanMat;
            oceanFaces[i].Initialize(oMeshFilters[i].sharedMesh, terrainFaces[i], resolution * resolution);
            oMeshFilters[i].gameObject.SetActive(renderFace);
        }
    
    }
    void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
                terrainFaces[i].ConstructTree();
            
        }

        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
    }
    public void GenerateOcean()
    {
        for (int i = 0; i < 6; i++)
            if (oMeshFilters[i].gameObject.activeSelf)
                oceanFaces[i].ConstructMesh();
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
