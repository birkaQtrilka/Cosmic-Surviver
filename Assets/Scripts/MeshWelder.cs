using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshWelder
{
    public static bool EnableDebug;
    readonly static List<Vector3> _weldedVertices = new();
    public static List<Vector3> WeldedVertices => _weldedVertices;

    public static Mesh CombineMeshes(MeshFilter[] oMeshFilters, Transform parent)
    {
        CombineInstance[] combine = new CombineInstance[oMeshFilters.Length];

        for (int i = 0; i < oMeshFilters.Length; i++)
        {
            if (oMeshFilters[i] == null || oMeshFilters[i].sharedMesh == null) continue;

            combine[i].mesh = oMeshFilters[i].sharedMesh;
            combine[i].transform = parent.worldToLocalMatrix * oMeshFilters[i].transform.localToWorldMatrix;
            //oMeshFilters[i].gameObject.SetActive(false);
            GameObject.DestroyImmediate(oMeshFilters[i].gameObject);
        }

        Mesh combinedMesh = new();
        combinedMesh.indexFormat = IndexFormat.UInt32;
        combinedMesh.CombineMeshes(combine, true, true);
        return combinedMesh;
    }

    public static Mesh WeldMeshes(Mesh combinedMesh, OceanFace[] faces, int resolution)
    {
        ClearDebugData();

        int[] allTriangles = combinedMesh.triangles;
        Vector3[] allVertices = combinedMesh.vertices;
        bool[] connectedLookup = new bool[24]; // 6 faces * 4 edges
        int cellCount = resolution - 1;

        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            for (int edgeIndex = 0; edgeIndex < 4; edgeIndex++)
            {
                ProcessEdgeConnection(
                    faceIndex, edgeIndex,
                    faces, connectedLookup,
                    allVertices, allTriangles,
                    cellCount
                );
            }
        }
        combinedMesh.triangles = allTriangles;
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();

        return combinedMesh;
    }

    private static void ProcessEdgeConnection(
        int faceIndexA, int edgeIndexA,
        OceanFace[] faces, bool[] connectedLookup,
        Vector3[] allVertices, int[] allTriangles,
        int cellCount)
    {
        // Determine neighbor
        (int faceIndexB, int edgeIndexB) = GridNavigator.CubeFaceConnections[faceIndexA, edgeIndexA];

        // Check if we already handled this seam from the other side
        if (FacesAreConnectedOrMark(connectedLookup, faceIndexA, edgeIndexA, faceIndexB, edgeIndexB))
            return;

        // Prepare data for the seam
        OceanFace faceA = faces[faceIndexA];
        OceanFace faceB = faces[faceIndexB];
        int offsetA = GetTrianglesGlobalOffset(faces, faceIndexA);
        int offsetB = GetTrianglesGlobalOffset(faces, faceIndexB);

        // Walk along the seam
        WeldSeam(
            faceA, edgeIndexA, offsetA,
            faceB, edgeIndexB, offsetB,
            allVertices, allTriangles,
            cellCount
        );
    }

    private static void WeldSeam(
        OceanFace faceA, int edgeA, int offsetA,
        OceanFace faceB, int edgeB, int offsetB,
        Vector3[] vertices, int[] triangles,
        int cellCount)
    {
        for (int i = 0; i < cellCount; i++)
        {
            // Note: Neighbor edge runs in reverse direction relative to current edge
            TriangleCell cellA = faceA.EdgeCellTriangles[edgeA, i];
            TriangleCell cellB = faceB.EdgeCellTriangles[edgeB, cellCount - 1 - i];

            if (cellA == null || cellB == null) continue;

            WeldMatchingVerticesInCells(
                cellA, offsetA,
                cellB, offsetB,
                vertices, triangles
            );
        }
    }

    private static void WeldMatchingVerticesInCells(
        TriangleCell cellA, int offsetA,
        TriangleCell cellB, int offsetB,
        Vector3[] vertices, int[] triangles)
    {
        // Compare every vertex in Cell A with every vertex in Cell B
        for (int j = 0; j < cellA.Count; j++)
        {
            int triIndexA = cellA[j] + offsetA;
            Vector3 posA = vertices[triangles[triIndexA]];

            for (int k = 0; k < cellB.Count; k++)
            {
                int triIndexB = cellB[k] + offsetB;
                Vector3 posB = vertices[triangles[triIndexB]];

                // The Welding Logic
                float sqrDist = (posA - posB).sqrMagnitude;
                if (sqrDist > 0.001f) continue;
                // Point Triangle B's index to the same vertex Triangle A is using
                triangles[triIndexB] = triangles[triIndexA];

                if (EnableDebug) _weldedVertices.Add(posA);
            }
        }
    }

    private static bool FacesAreConnectedOrMark(bool[] connectedLookup, int f1, int e1, int f2, int e2)
    {
        int id1 = f1 * 4 + e1;
        int id2 = f2 * 4 + e2;

        if (connectedLookup[id1]) return true;

        connectedLookup[id1] = true;
        connectedLookup[id2] = true;
        return false;
    }

    private static int GetTrianglesGlobalOffset(OceanFace[] faces, int faceIndex)
    {
        int result = 0;
        for (int i = 0; i < faceIndex; i++)
        {
            result += faces[i].Triangles.Count;
        }
        return result;
    }

    private static void ClearDebugData()
    {
        _weldedVertices.Clear();
    }
}