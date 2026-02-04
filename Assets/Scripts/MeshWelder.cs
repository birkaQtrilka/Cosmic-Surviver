using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshWelder 
{
    public static void CombineAndWeldMeshes(MeshFilter[] oMeshFilters, Transform parent)
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
    
        Mesh combinedMesh = new Mesh();
        combinedMesh.indexFormat = IndexFormat.UInt32; // Allow huge meshes
        combinedMesh.CombineMeshes(combine, true, true);


        for (int i = 0; i < oMeshFilters.Length; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                (int face, int edge) = GridNavigator.CubeFaceConnections[i, j];
                Mesh faceMesh = oMeshFilters[face].sharedMesh;
                // need to modify triangles list instead of vertices, which is more complex
            }
            break;
        }

        // --- STEP 2: Weld Vertices ---
        // This is the step that actually connects the edges
        combinedMesh = WeldVertices(combinedMesh);
    
        // --- STEP 3: Create GameObject ---
        GameObject oceanObj = new GameObject("Combined Ocean");
        oceanObj.transform.SetParent(parent);
        oceanObj.transform.localPosition = Vector3.zero;
        oceanObj.transform.localRotation = Quaternion.identity;
        oceanObj.transform.localScale = Vector3.one;
    
        MeshFilter filter = oceanObj.AddComponent<MeshFilter>();
        filter.sharedMesh = combinedMesh;
    
        MeshRenderer renderer = oceanObj.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = oMeshFilters[0].GetComponent<MeshRenderer>().sharedMaterial;
    }
    
    // automatically copy all interior verts.
    // I can also safely copy all non shore and non ocean vertices
    // the vertices with indeces that are above powResolution-1 
    // AND vertices that are on the edge of grid
    // both need manual check of connection
    
    // i need to keep track of what side each mesh is and only verfi

    // The Algorithm to merge duplicate vertices
    private static Mesh WeldVertices(Mesh originalMesh, float threshold = 0.01f)
    {
        Vector3[] oldVertices = originalMesh.vertices;
        // We assume the ocean uses UVs; if not, you can remove uvs logic
        Vector2[] oldUvs = originalMesh.uv;
        int[] oldTriangles = originalMesh.triangles;
    
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector2> newUvs = new List<Vector2>();
        List<int> newTriangles = new List<int>();
    
        // Map: Position -> Index in the new list
        // This acts as a lookup to find if we've already seen a vertex at this spot
        Dictionary<Vector3, int> vertexMap = new Dictionary<Vector3, int>();
    
        for (int i = 0; i < oldVertices.Length; i++)
        {
            Vector3 pos = oldVertices[i];
    
            // If we haven't seen this position before (or it's not close enough to an existing one)
            // Note: Dictionary check uses exact or very close float matching depending on Unity version.
            // For procedural planets, vertices usually align exactly.
            if (!vertexMap.TryGetValue(pos, out int newIndex))
            {
                // It's a new unique vertex
                newIndex = newVertices.Count;
                newVertices.Add(pos);
                if (oldUvs.Length > 0) newUvs.Add(oldUvs[i]);
    
                vertexMap.Add(pos, newIndex);
            }
    
            // If we HAVE seen it, we skip adding it to the list, 
            // but we might need to handle UVs here if your texture requires seams.
            // For an ocean, usually we want to merge regardless of UVs to ensure smooth water.
        }
    
        // Remap triangles to point to the new, unique vertices
        for (int i = 0; i < oldTriangles.Length; i++)
        {
            Vector3 oldPos = oldVertices[oldTriangles[i]];
            // Find the index of the merged vertex
            newTriangles.Add(vertexMap[oldPos]);
        }
    
        // Apply back to mesh
        Mesh finalMesh = new Mesh();
        finalMesh.indexFormat = IndexFormat.UInt32;
        finalMesh.vertices = newVertices.ToArray();
        finalMesh.triangles = newTriangles.ToArray();
        if (newUvs.Count > 0) finalMesh.uv = newUvs.ToArray();
    
        // CRITICAL: Now that vertices are connected, this will smooth the normals across the seams
        finalMesh.RecalculateNormals();
        finalMesh.RecalculateBounds();
    
        return finalMesh;
    }
}
