using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public struct MeshWeldJob : IJob
{
    [ReadOnly] public NativeArray<int> triangleOffsets;
    [ReadOnly] public NativeArray<FixedList128Bytes<int>> edgeCellTriangles;

    [ReadOnly] public int resolution;

    [ReadOnly] public NativeArray<float3> vertices;
    public NativeArray<int> triangles;

    public void Execute()
    {
        NativeArray<bool> connectedLookup = new NativeArray<bool>(24, Allocator.Temp);
        NativeArray<int2> cubeFaceConnections = new NativeArray<int2>(24, Allocator.Temp);
        PopulateConnectionData(ref cubeFaceConnections);

        int cellCount = resolution - 1;

        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            for (int edgeIndex = 0; edgeIndex < 4; edgeIndex++)
            {
                ProcessEdgeConnection(
                    faceIndex, edgeIndex,
                    cellCount, connectedLookup, cubeFaceConnections
                );
            }
        }
        connectedLookup.Dispose();
        cubeFaceConnections.Dispose();
    }

    private void ProcessEdgeConnection(
        int faceIndexA, int edgeIndexA,
        int cellCount, NativeArray<bool> connectedLookup, NativeArray<int2> cubeFaceConnections)
    {
        // Determine neighbor
        int2 faceEdge = cubeFaceConnections[faceIndexA * 4 + edgeIndexA];
        int faceIndexB = faceEdge.x;
        int edgeIndexB = faceEdge.y;
        // Check if we already handled this seam from the other side
        if (FacesAreConnectedOrMark(faceIndexA, edgeIndexA, faceIndexB, edgeIndexB, connectedLookup))
            return;

        // Prepare data for the seam
        int offsetA = triangleOffsets[faceIndexA];
        int offsetB = triangleOffsets[faceIndexB];

        // Walk along the seam
        WeldSeam(
            faceIndexA, edgeIndexA, offsetA,
            faceIndexB, edgeIndexB, offsetB,
            cellCount
        );
    }

    private void WeldSeam(
       int faceA, int edgeA, int offsetA,
       int faceB, int edgeB, int offsetB,
       int cellCount)
    {
        for (int i = 0; i < cellCount; i++)
        {
            // Note: Neighbor edge runs in reverse direction relative to current edge
            FixedList128Bytes<int> cellA = GetCell(faceA, edgeA, i);
            FixedList128Bytes<int> cellB = GetCell(faceB, edgeB, cellCount - 1 - i);

            if (cellA.IsEmpty || cellB.IsEmpty) continue;

            WeldMatchingVerticesInCells(
                cellA, offsetA,
                cellB, offsetB
            );
        }
    }

    private FixedList128Bytes<int> GetCell(int faceIndex, int edgeIndex, int cellIndex)
    {
        int cellsPerEdge = resolution - 1;
        int cellsPerFace = 4 * cellsPerEdge;

        int i = (faceIndex * cellsPerFace) + (edgeIndex * cellsPerEdge) + cellIndex;
        return edgeCellTriangles[i];
    }

    private void WeldMatchingVerticesInCells(
        FixedList128Bytes<int> cellA, int offsetA,
        FixedList128Bytes<int> cellB, int offsetB)
    {
        // Compare every vertex in Cell A with every vertex in Cell B
        for (int j = 0; j < cellA.Length; j++)
        {
            int triIndexA = cellA[j] + offsetA;
            float3 posA = vertices[triangles[triIndexA]];

            for (int k = 0; k < cellB.Length; k++)
            {
                int triIndexB = cellB[k] + offsetB;
                float3 posB = vertices[triangles[triIndexB]];

                // The Welding Logic
                float sqrDist = math.lengthsq(posA - posB); ;
                if (sqrDist > 0.001f) continue;
                // Point Triangle B's index to the same vertex Triangle A is using
                triangles[triIndexB] = triangles[triIndexA];
            }
        }
    }

    private bool FacesAreConnectedOrMark(int f1, int e1, int f2, int e2, NativeArray<bool> connectedLookup)
    {
        int id1 = f1 * 4 + e1;
        int id2 = f2 * 4 + e2;

        if (connectedLookup[id1]) return true;

        connectedLookup[id1] = true;
        connectedLookup[id2] = true;
        return false;
    }

    private static void PopulateConnectionData(ref NativeArray<int2> arr)
    {
        // Face 0: { (4,1), (3,0), (5,3), (2,0) }
        arr[0] = new int2(4, 1);
        arr[1] = new int2(3, 0);
        arr[2] = new int2(5, 3);
        arr[3] = new int2(2, 0);

        // Face 1: { (4,3), (2,2), (5,1), (3,2) }
        arr[4] = new int2(4, 3);
        arr[5] = new int2(2, 2);
        arr[6] = new int2(5, 1);
        arr[7] = new int2(3, 2);

        // Face 2: { (0,3), (5,2), (1,1), (4,2) }
        arr[8] = new int2(0, 3);
        arr[9] = new int2(5, 2);
        arr[10] = new int2(1, 1);
        arr[11] = new int2(4, 2);

        // Face 3: { (0,1), (4,0), (1,3), (5,0) }
        arr[12] = new int2(0, 1);
        arr[13] = new int2(4, 0);
        arr[14] = new int2(1, 3);
        arr[15] = new int2(5, 0);

        // Face 4: { (3,1), (0,0), (2,3), (1,0) }
        arr[16] = new int2(3, 1);
        arr[17] = new int2(0, 0);
        arr[18] = new int2(2, 3);
        arr[19] = new int2(1, 0);

        // Face 5: { (3,3), (1, 2), (2, 1), (0, 2) }
        arr[20] = new int2(3, 3);
        arr[21] = new int2(1, 2);
        arr[22] = new int2(2, 1);
        arr[23] = new int2(0, 2);
    }
}