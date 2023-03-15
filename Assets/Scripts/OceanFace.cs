using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
[System.Serializable]
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

        int trianglesL = _mesh.triangles.Length;
        int vertLength = _mesh.vertices.Length;
        if (trianglesDisabled == null || trianglesL > trianglesDisabled.Length)
            trianglesDisabled = new bool[trianglesL];
        if (trisWithVertex == null || vertLength > trisWithVertex.Length)
            trisWithVertex = new List<int>[vertLength];

        for (int i = 0; i < vertLength; i++)
            trisWithVertex[i] = _mesh.triangles.IndexOf(i);

        Parallel.ForEach(terrainFace.OceanVertexIndexes, coord =>
        {
            for (int j = 0; j < trisWithVertex[coord].Count; ++j)
            {
                int value = trisWithVertex[coord][j];
                int remainder = value % 3;
                trianglesDisabled[value - remainder] = true;
                trianglesDisabled[value - remainder + 1] = true;
                trianglesDisabled[value - remainder + 2] = true;

            }
        });

        _mesh.triangles = _mesh.triangles.RemoveAllSpecifiedIndicesFromArray(trianglesDisabled,trianglesL).ToArray();
        for (int i = 0; i < trianglesL; ++i)
            trianglesDisabled[i] = false;
      

    }


    

    
    

    

    
    
}
