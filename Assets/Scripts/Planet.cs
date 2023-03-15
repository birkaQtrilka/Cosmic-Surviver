using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
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

        oceanFaces ??= new OceanFace[6];
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
            terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i], oceanLevelRiser);
            meshFilters[i].gameObject.SetActive(renderFace);

            oceanFaces[i] ??= new();
            oceanFaces[i].Initialize(oMeshFilters[i].sharedMesh, terrainFaces[i], resolution * resolution);
            oMeshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.oceanMat;
            oMeshFilters[i].gameObject.SetActive(renderFace);
        }
    
    }
    void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
                terrainFaces[i].ConstructMesh();
            
        }

        colorGenerator.UpdateElevation(shapeGenerator.elevationMinMax);
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
    public void GenerateOcean()
    {
        for (int i = 0; i < 6; i++)
        
            if (oMeshFilters[i].gameObject.activeSelf)
                oceanFaces[i].ConstructMesh();
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
