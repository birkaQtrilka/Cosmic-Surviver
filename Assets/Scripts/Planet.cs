using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planet : MonoBehaviour
{
    [Range(2, 256)]
    public int resolution = 10;
    public bool autoUpdate = true;
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
    OceanFace[] oceanFaces;
    //OceanFace[] oceanFaces;

    void Initialize()
    {
        shapeGenerator.UpdateSettings(shapeSettings) ;
        colorGenerator.UpdateSettings(colorSettings);
        
        if (meshFilters == null || meshFilters.Length == 0)
        {
            meshFilters = new MeshFilter[6];
        }
        terrainFaces = new TerrainFace[6];
        //oceanFaces = new[6];

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i] == null)
            {
                GameObject meshObj = new("mesh");//and new ocean mesh
                meshObj.transform.parent = transform;//same

                meshObj.AddComponent<MeshRenderer>();//same
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();//same
                meshFilters[i].sharedMesh = new Mesh();//same
            }
            meshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.planetMat;//color settings.oceanMat

            terrainFaces[i] = new TerrainFace(shapeGenerator, meshFilters[i].sharedMesh, resolution, directions[i]);//same
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            meshFilters[i].gameObject.SetActive(renderFace);//am schimbat ceva
        }
    }
    void InitializeOcean()
    {
        if (oMeshFilters == null || oMeshFilters.Length == 0)
        {
            oMeshFilters = new MeshFilter[6];
        }
        oceanFaces = new OceanFace[6];
        //oceanFaces = new[6];


        for (int i = 0; i < 6; i++)
        {
            if (oMeshFilters[i] == null)
            {
                GameObject meshObj = new("oceanMesh");//and new ocean mesh
                meshObj.transform.parent = transform;//same

                meshObj.AddComponent<MeshRenderer>();//same
                oMeshFilters[i] = meshObj.AddComponent<MeshFilter>();//same
                oMeshFilters[i].sharedMesh = new Mesh();//same
            }
            oMeshFilters[i].GetComponent<MeshRenderer>().sharedMaterial = colorSettings.oceanMat;//color settings.oceanMat

            oceanFaces[i] = new (oMeshFilters[i].sharedMesh, terrainFaces[i],resolution*resolution,shapeSettings.planetRadius);//same
            bool renderFace = faceRenderMask == FaceRenderMask.All || (int)faceRenderMask - 1 == i;
            oMeshFilters[i].gameObject.SetActive(renderFace);//am schimbat ceva
        }
    }

    void GenerateMesh()
    {
        for (int i = 0; i < 6; i++)
        {
            if (meshFilters[i].gameObject.activeSelf)
                terrainFaces[i].ConstructMesh();
            if (oMeshFilters[i].gameObject.activeSelf)
                oceanFaces[i].ConstructMesh();
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

    public void GeneratePlanet()
    {
        Initialize();
        InitializeOcean();
        GenerateMesh();
        GenerateColours();
    }
    public void OnShapeSettingsUpdate()
    {
        if (!autoUpdate) return;
        Initialize();
        InitializeOcean();

        GenerateMesh();
    }
    public void OnColourSettingsUpdated()
    {
        if (!autoUpdate) return;
        Initialize();
        //InitializeOcean();

        GenerateColours();
    }
}
