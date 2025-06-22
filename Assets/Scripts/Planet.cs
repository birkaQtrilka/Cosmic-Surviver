using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class Planet : MonoBehaviour
{
    public static Transform player;

    [Range(2, 256)]
    public int resolution = 10;
    public bool autoUpdate = true;
    [SerializeField] float _oceanLevel = 0.2f;
    public enum FaceRenderMask
    {
        All,Top,Bottom,Left,Right,Fron,Back
    }
    public FaceRenderMask faceRenderMask = FaceRenderMask.All;

    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    public bool HasOceanMesh = true;

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

    void OnValidate()
    {
        if(player==null)
            player = Camera.main.transform;
    }
    
    
    void Initialize()
    {
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

    public void DisableOceanMeshes()
    {
        for (int i = 0; i < 6; i++)
            oMeshFilters[i].gameObject.SetActive(false);
    }

    public void EnableOceanMeshes()
    {
        for (int i = 0; i < 6; i++)
            oMeshFilters[i].gameObject.SetActive(true);

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
        transform.position = oldPos;
    }

    public void GenerateOcean()
    {
        if (!HasOceanMesh) return;

        var oldPos = transform.position;
        transform.position = Vector3.zero;
        for (int i = 0; i < 6; i++)
            if (oMeshFilters[i].gameObject.activeSelf)
            {
                oceanFaces[i].ConstructMesh();
                oMeshFilters[i].transform.position = Vector3.zero;

            }
        transform.position = oldPos;
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
