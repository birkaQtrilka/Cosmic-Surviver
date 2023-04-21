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
    TerrainFace terrainFace;

    bool[] trianglesDisabled;
    List<int>[] trisWithVertex;
    public void Initialize(Mesh mesh, TerrainFace face, int powResolution)
    {
        _mesh = mesh;
        terrainFace = face;
        this.powResolution = powResolution;
    }

    public void ConstructMesh()
    {
        _mesh.Clear();
        _mesh.vertices = terrainFace.UnscaledVerts;
        _mesh.triangles = terrainFace.Mesh.triangles;//maybe i should copy it, not just reference
        _mesh.RecalculateNormals();
        _mesh.uv = _mesh.uv.Length == powResolution ? _mesh.uv : new Vector2[powResolution];

        var trigs = _mesh.triangles;
        int trianglesL = trigs.Length;
        int vertLength = trigs.Length;
        if (trianglesDisabled == null || trianglesL > trianglesDisabled.Length)
            trianglesDisabled = new bool[trianglesL];
        if (trisWithVertex == null || vertLength > trisWithVertex.Length)
            trisWithVertex = new List<int>[vertLength];

        int trigsLength = trigs.Length;
        for (int i = 0; i < vertLength; i++)
        {
            var result = new List<int>();
            for (int j = 0; j < trigsLength; j++)
                if (trigs[j] == i)
                    result.Add(j);

            trisWithVertex[i] = result;
        }
        
        int deletedTrigs = 0; 
        Parallel.ForEach(terrainFace.OceanVertexIndexes, coord =>
        {
            for (int j = 0; j < trisWithVertex[coord].Count; ++j)
            {
                int value = trisWithVertex[coord][j];
                int remainder = value % 3;
                trianglesDisabled[value - remainder] = true;
                trianglesDisabled[value - remainder + 1] = true;
                trianglesDisabled[value - remainder + 2] = true;
                deletedTrigs += 3;
            }
        });

        var b = new List<int>();

        for (int i = 0; i < trianglesL; ++i)
            if (trianglesDisabled[i])
                trianglesDisabled[i] = false;
            else
                b.Add( _mesh.triangles[i]);        


        _mesh.triangles = b.ToArray();

    }


}
