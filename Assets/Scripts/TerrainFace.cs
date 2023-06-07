using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class TerrainFace
{
    public Mesh Mesh { get; }
    int resolution;
    int powResolution;
    Vector3 localUp;
    Vector3 axisA;
    Vector3 axisB;
    readonly ShapeGenerator shapeGenerator;
    Vector3[] vertices;
    List<Vector3> verticesB = new();
    List<int> trianglesB = new();
    float radius;
    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp, float radius)
    {
        this.shapeGenerator = shapeGenerator;
        Mesh = mesh;
        this.resolution = resolution;
        powResolution =  resolution * resolution;
        this.localUp = localUp;
        this.radius = radius;
        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }
    public HashSet<int> OceanVertexIndexes;
    public Vector3[] UnscaledVerts;
    public void ConstructTree()
    {
        verticesB.Clear();
        trianglesB.Clear();

        Chunk parentChunk = new (null, null, localUp.normalized, radius, 0, localUp, axisA, axisB);
        parentChunk.GenerateChildren();

        int triangleOffset = 0;
        foreach (Chunk child in parentChunk.GetVisibleChildren())
        {
            (Vector3[] verts, int[] trigs) = child.CalculateVerticesAndTriangles(triangleOffset);
            verticesB.AddRange(verts);
            trianglesB.AddRange(trigs);
        }

        Mesh.Clear();
        Mesh.vertices = verticesB.ToArray();
        Mesh.triangles = trianglesB.ToArray();
        Mesh.RecalculateNormals();
    }
    public void ConstructMesh()
    {
        vertices = new Vector3[powResolution];
        OceanVertexIndexes = new();
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        int triIndex = 0;
        //bool[] waterVerts = new bool[powResolution];
        bool sameUnscaledVerts = UnscaledVerts!=null&& UnscaledVerts.Length == powResolution;
        UnscaledVerts = sameUnscaledVerts ? UnscaledVerts : new Vector3[powResolution];
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

                //i know it's not SOLID but fuck you
                if(!sameUnscaledVerts)
                    UnscaledVerts[i] = pointOnUnitSphere* shapeGenerator.GetScaledElevation(0);
                
                vertices[i] = pointOnUnitSphere * scaledElev;
                uv[i].y = unscaledElevation;

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
        var elevations = new float[9];
        for (int i = 0; i < uv.Length; i++)
        {
            float x, y;
            y = i / resolution;
            x = i - y * resolution;
            if (x == resolution - 1 || y == resolution - 1 || y == 0 || x == 0)
                continue;
            int point = i,
                downRight = i + resolution + 1,
                down = i + resolution,
                downLeft = i + resolution - 1,
                left = i - 1,
                upLeft = i - resolution - 1,
                up = i - resolution,
                upRight = i - resolution + 1,
                right = i + 1;

            elevations[0] = uv[point].y;
            elevations[1] = uv[downRight].y;
            elevations[2] = uv[down].y;
            elevations[3] = uv[downLeft].y;
            elevations[4] = uv[left].y;
            elevations[5] = uv[upLeft].y;
            elevations[6] = uv[up].y;
            elevations[7] = uv[upRight].y;
            elevations[8] = uv[right].y;

            //if one of em is bellow 0, don't add  it
            if (!elevations.Any(n=>n<=0))
                OceanVertexIndexes.Add(point);
            
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
