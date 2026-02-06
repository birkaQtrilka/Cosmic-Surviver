using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshWelder 
{
    readonly static List<Vector3> _debug = new(); 
    public static List<Vector3> Test => _debug;
    public static List<(Vector3, Vector3)> Test2 = new();
    public static List<Vector3> Test3 = new();
    public static bool EnableDebug;

    public static Mesh WeldMeshes(Mesh combinedMesh, OceanFace[] faces, int resolution)
    {
        _debug.Clear();
        Test2.Clear();
        Test3.Clear();

        int[] allTriangles = combinedMesh.triangles;
        Vector3[] allVertices = combinedMesh.vertices;
        bool[] connectedLookup = new bool[24];// 6 faces * 4 edges each, to keep track of which edges have already been connected
        // avoid already connected edges;
        for (int this_FaceIndex = 0; this_FaceIndex < 6; this_FaceIndex++)
        {
            for (int this_EdgeIndex = 0; this_EdgeIndex < 4; this_EdgeIndex++)
            {
                //if (FacesAreConnectedOrMark(connectedLookup, this_FaceIndex, this_EdgeIndex)) continue;// need to also look for other edge or make a lookup for both edges

                (int other_FaceIndex, int other_EdgeIndex) = GridNavigator.CubeFaceConnections[this_FaceIndex, this_EdgeIndex];
                OceanFace this_Face = faces[this_FaceIndex];
                OceanFace other_Face = faces[other_FaceIndex];

                // this should be the relative 0 or starting point of the triangles of this face
                int this_combinedMeshTrianglesOffset = GetTrianglesGlobalOffset(faces, this_FaceIndex); 
                int other_combinedMeshTrianglesOffset = GetTrianglesGlobalOffset(faces, other_FaceIndex);
                int cellCount = resolution - 1;
                
                for (int i = 0; i < cellCount; i++)
                {
                    TriangleCell this_Cell = this_Face.EdgeCellTriangles[this_EdgeIndex, i];
                    TriangleCell other_Cell = other_Face.EdgeCellTriangles[other_EdgeIndex, cellCount - 1 - i ];
                    if(this_Cell == null || other_Cell == null) continue;

                    for (int j = 0; j < this_Cell.Count; j++)
                    {
                        // not good, need to use triange array to acces the vert
                        int this_globalTriangleIndex = this_Cell[j] + this_combinedMeshTrianglesOffset ;
                        if (this_EdgeIndex == 3)
                        {
                            this_globalTriangleIndex ++;
                        }
                        Vector3 this_Vert = allVertices[allTriangles[this_globalTriangleIndex]];
                        if (( i == 59))
                        {
                            Test3.Add(this_Vert);
                        }
                        for (int k = 0; k < other_Cell.Count; k++)
                        {
                            int other_globalTriangleIndex = other_Cell[k] + other_combinedMeshTrianglesOffset;
                            
                            Vector3 other_Vert = allVertices[allTriangles[other_globalTriangleIndex]];
                            if ((this_EdgeIndex == 3 && i >50) || (this_EdgeIndex == 0 && i < 10))
                            {
                                Test2.Add((this_Vert, other_Vert));
                            }
                            if (math.distance(this_Vert, other_Vert) > 0.01f) continue;
                            allTriangles[other_globalTriangleIndex] = allTriangles[this_globalTriangleIndex];
                            if(EnableDebug) _debug.Add(this_Vert);
                        }

                    }
                }
            }
            break;
        }
        combinedMesh.triangles = allTriangles;

        // 5. Recalculate internals for smooth lighting
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();

        return combinedMesh;
    }
    
    public static Mesh CombineMeshes(MeshFilter[] oMeshFilters, Transform parent)
    {
        CombineInstance[] combine = new CombineInstance[oMeshFilters.Length];

        for (int i = 0; i < oMeshFilters.Length; i++)
        {
            if (oMeshFilters[i] == null || oMeshFilters[i].sharedMesh == null) continue;

            combine[i].mesh = oMeshFilters[i].sharedMesh;
            // Transform to local space of the planet manager
            combine[i].transform = parent.worldToLocalMatrix * oMeshFilters[i].transform.localToWorldMatrix;

            oMeshFilters[i].gameObject.SetActive(false);
        }

        Mesh combinedMesh = new();
        combinedMesh.indexFormat = IndexFormat.UInt32; // Allow huge meshes
        combinedMesh.CombineMeshes(combine, true, true);

        return combinedMesh;
    }


    static bool FacesAreConnectedOrMark(bool[] connectedLookup, int f_1, int e_1)
    {
        if (connectedLookup[6 * f_1 + e_1]) return true;

        connectedLookup[6 * f_1 + e_1] = true;
        return false;
    }

    static int GetTrianglesGlobalOffset(OceanFace[] faces, int faceIndex)
    {
        int result = 0;
        for (int i = 0; i < faceIndex; i++) {
            result += faces[i].GetMesh().triangles.Length;
        }
        return result;
    }
   
}
