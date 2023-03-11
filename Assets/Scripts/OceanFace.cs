using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class OceanFace 
{
    //figure out where to construct the mesh via terrain height
    Mesh mesh;
    readonly int powResolution;
    TerrainFace terrainFace;
    float elevation;
    public OceanFace(Mesh mesh,TerrainFace face, int powResolution, float scaledElevation)
    {
        this.mesh = mesh;
        terrainFace = face;
        this.powResolution = powResolution;
        elevation = scaledElevation;
    }
    public void ConstructMesh()
    {
        var uv = (mesh.uv.Length == powResolution) ? mesh.uv : new Vector2[powResolution];

        mesh.Clear();
        mesh.vertices = terrainFace.Mesh.vertices.Select(v => v.normalized * elevation).ToArray() ;
        mesh.triangles = terrainFace.Mesh.triangles;//maybe i shouldn't set this everytime (cache it in constructor)
        mesh.RecalculateNormals();

        mesh.uv = uv;

    }
}
