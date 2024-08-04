using System.Collections.Generic;
using UnityEngine;
public class TerrainFace
{
    public Mesh Mesh { get; }
    Vector3 LocalUp { get; }
    //axes perpendicular to localUp
    Vector3 AxisA { get; }
    Vector3 AxisB { get; }
    public ShapeGenerator ShapeGenerator { get; }
    public OceanVertData[] BellowZeroVertices { get; private set; }

    readonly int resolution;
    readonly int powResolution;
    Vector3[] vertices;
    readonly float _oceanLevel;
    public TerrainFace(ShapeGenerator shapeGenerator, Mesh mesh, int resolution, Vector3 localUp, float oceanLevel = 0f)
    {
        ShapeGenerator = shapeGenerator;
        Mesh = mesh;
        this.resolution = resolution;
        powResolution =  resolution * resolution;
        LocalUp = localUp;
        _oceanLevel = oceanLevel;
        AxisA = new Vector3(localUp.y, localUp.z, localUp.x);
        AxisB = Vector3.Cross(localUp, AxisA);
    }


    public void ConstructMesh()
    {
        vertices = new Vector3[powResolution];
        int[] triangles = new int[(resolution - 1) * (resolution - 1) * 6];
        Vector2[] uv = new Vector2[powResolution];
        BellowZeroVertices = new OceanVertData[powResolution];
        int triIndex = 0;
        //creates a square with varying elevations depending on the noise settings and "blows it up" in a sphere shape
        //in order to have a uniform distribution of triangles
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;

                Vector3 pointOnUnitSphere = GetUnitSpherePointFromXY(x,y);
                float unscaledElevation = ShapeGenerator.CalculateUnscaledElevation(pointOnUnitSphere);
                float scaledElev = ShapeGenerator.GetScaledElevation(unscaledElevation);

                Vector3 vertex = pointOnUnitSphere * scaledElev;
                vertices[i] = vertex;

                //to avoid another loop in ocean face class, I'm flagging verts as bellow zero here
                BellowZeroVertices[i] = new OceanVertData()
                {
                    isOcean = unscaledElevation <= 0,
                    WorldPos = pointOnUnitSphere * ShapeGenerator.PlanetRadius,
                    VerticesArrayIndex = i,
                    DistanceToOceanLevel = unscaledElevation - _oceanLevel
                } ;//marking ocean verts

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
                Vector3 pointOnUnitCube = LocalUp + (percent.x - .5f) * 2 * AxisA + (percent.y - .5f) * 2 * AxisB;
                Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                
                uv[i].x = colorGenerator.BiomePercentFromPoint(pointOnUnitSphere);
            }
        }
    }

    public Vector3 GetUnitSpherePointFromXY(float x, float y)
    {
        Vector2 percent = new Vector2(x, y) / (resolution - 1);
        Vector3 pointOnUnitCube = LocalUp + (percent.x - .5f) * 2 * AxisA + (percent.y - .5f) * 2 * AxisB;
        return pointOnUnitCube.normalized;
    }
}
