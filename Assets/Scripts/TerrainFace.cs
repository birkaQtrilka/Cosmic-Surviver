using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFace
{
    public Mesh Mesh { get; }
    int resolution;
    int powResolution;

    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    ShapeGenerator shapeGenerator;
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
    
    public void ConstructMesh()
    {
        Vector3[] vertices = new Vector3[powResolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;
        //bool[] waterVerts = new bool[powResolution];

        Vector2[] uv = (Mesh.uv.Length == powResolution) ?Mesh.uv:new Vector2[powResolution];

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
                vertices[i] = pointOnUnitSphere * scaledElev;
                
                //copy the vertecies under 0 for ocean info
                //water mesh generation algorithm (I should search for a better one)
                
                uv[i].y = unscaledElevation;
                //unscaledElevationForOcean, maybe 0?
                if (x != resolution - 1 && y != resolution - 1)
                {
                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }
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
        Mesh.uv = uv;
    }
}






//if (scaledElev <= 0)
//{
//    int minusRes = i - resolution;
//    int plusRes = i + resolution;
//    waterVerts[i] = true;
//    if (i - 1 >= 0)
//    {
//        waterVerts[i - 1] = true;
//        if (minusRes >= 0)
//        {
//            waterVerts[minusRes] = true;
//            if (minusRes - 1 >= 0)
//                waterVerts[minusRes - 1] = true;

//        }


//    }

//    if (i + 1 < powResolution)
//    {
//        waterVerts[i + 1] = true;
//        if (plusRes < powResolution)
//        {
//            waterVerts[plusRes] = true;
//            if (plusRes + 1 < powResolution)
//                waterVerts[plusRes + 1] = true;
//        }
//    }
//    //if()

//}
