using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
[Serializable]
public class OceanFace 
{
    Mesh _mesh;
    int powResolution;
    int _resolution;
    TerrainFace terrainFace;

    public void Initialize(Mesh mesh, TerrainFace face, int resolution)
    {
        _mesh = mesh;
        terrainFace = face;
        this.powResolution = resolution * resolution;
        _resolution = resolution;
    }

    public void ConstructMesh()
    {
        Vector3[] terrainVerts = terrainFace.UnscaledVerts;

        Vector2[] terrainUV = terrainFace.Mesh.uv;
        List<int> triangles = new(terrainFace.Mesh.triangles.Length);
        List<Vector3> vertices = new(terrainFace.Mesh.vertices.Length);

        //I still need to delete
        for (int i = 0; i < terrainUV.Length; i++)
        {
            float x, y;
            y = i / _resolution;
            x = i - y * _resolution;
            if (x >= _resolution - 1 || y >= _resolution - 1)
                continue;
            int point = i,
                downRight = i + _resolution + 1,
                down = i + _resolution,
                right = i + 1;
            bool isBellowOceanLevel = 
            terrainUV[point].y <= 0 ||
            terrainUV[downRight].y <= 0 ||
            terrainUV[down].y <= 0 ||
            terrainUV[right].y <= 0;

            if (!isBellowOceanLevel) continue;

            vertices.Add(terrainVerts[point]);//-3
            vertices.Add(terrainVerts[right]);//-2
            vertices.Add(terrainVerts[downRight]);//-1
            vertices.Add(terrainVerts[down]);//0

            int lastVertexIndex = vertices.Count - 1;

            triangles.Add(lastVertexIndex - 3);
            triangles.Add(lastVertexIndex - 2);
            triangles.Add(lastVertexIndex - 1);

            triangles.Add(lastVertexIndex - 3);
            triangles.Add(lastVertexIndex - 1);
            triangles.Add(lastVertexIndex);
        }

        _mesh.Clear();
        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.RecalculateNormals();
        Vector2[] uv = new Vector2[vertices.Count];

        _mesh.uv = uv;
    }


}
