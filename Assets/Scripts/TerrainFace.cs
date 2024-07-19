using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;
public struct OceanVertData
{
    public Vector3 WorldPos { get; set; }//point on sphere
    public bool isOcean;
    public bool isShore;//to be able to do it in parallel
    public int index { get; set; }
}
public class TerrainFace
{
    public Mesh Mesh { get; }
    int resolution;
    int powResolution;
    public Vector3 localUp;
    public Vector3 axisA;
    public Vector3 axisB;
    public ShapeGenerator shapeGenerator;
    Vector3[] vertices;
    List<Vector3> verticesB;
    List<int> trianglesB;
    public OceanVertData[] VerticesToRemove;

    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp)
    {
        this.shapeGenerator = shapeGenerator;
        Mesh = mesh;
        this.resolution = resolution;
        powResolution =  resolution * resolution;
        this.localUp = localUp;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    public void ConstructTree()
    {
        verticesB.Clear();
        trianglesB.Clear();


    }

    public void ConstructMesh()
    {
        vertices = new Vector3[powResolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;
        //bool[] waterVerts = new bool[powResolution];
        Vector2[] uv = new Vector2[powResolution];
        VerticesToRemove = new OceanVertData[powResolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;

                float unscaledElevation = shapeGenerator.CalculateUnscaledElevation(pointOnUnitSphere);
                float scaledElev = shapeGenerator.GetScaledElevation(unscaledElevation);

                //i know it's not SOLID but fuck you

                Vector3 vertex = pointOnUnitSphere * scaledElev;
                vertices[i] = vertex;
                VerticesToRemove[i] = new OceanVertData() { isOcean = unscaledElevation <= 0 , WorldPos = pointOnUnitSphere * shapeGenerator.GetScaledElevation(0), index = i} ;//marking ocean verts

                uv[i].y = unscaledElevation;
                if (x == resolution - 1 || y == resolution - 1) continue;
                
                triangles[triIndex] = i;
                triangles[triIndex + 1] = i + resolution + 1;
                triangles[triIndex + 2] = i + resolution;

                triangles[triIndex + 3] = i;
                triangles[triIndex + 4] = i + 1;
                triangles[triIndex + 5] = i + resolution + 1;
                triIndex += 6;
            }
        }
        //it's all the heights of points in the surrounding verticles, icluding the center one (which is point)
        
        Mesh.Clear();
        Mesh.vertices = vertices;
        Mesh.triangles = triangles;
        Mesh.RecalculateNormals();
        Mesh.uv = uv;
    }
    
    public void UpdateUVs(ColorGenerator colorGenerator)
    {
        Vector2[] uv = Mesh.uv;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                
                uv[i].x = colorGenerator.BiomePercentFromPoint(pointOnUnitSphere);
            }
        }
    }
}
