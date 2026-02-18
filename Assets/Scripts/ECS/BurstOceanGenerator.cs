using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class BurstOceanGenerator : Generator
{
    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    public MeshFilter[] terrainFilters; // Assign the 6 mesh filters
    public MeshFilter oceanMeshFilter;
    public int resolution = 32;
    public float oceanLevel = 0f;

    private NativeArray<NoiseLayerData> _noiseLayers;
    private BiomeData _biomeData;
    readonly Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
    readonly ShapeGenerator shapeGenerator = new();
    readonly ColorGenerator colorGenerator = new();

    void OnDestroy()
    {
        if (_noiseLayers.IsCreated) _noiseLayers.Dispose();
    }

    private void OnDisable()
    {
        if (_noiseLayers.IsCreated) _noiseLayers.Dispose();

        if (_biomeData.startHeights.IsCreated) _biomeData.startHeights.Dispose();
    }

    [ContextMenu("Generate")]
    public override void GeneratePlanet()
    {
        shapeGenerator.UpdateSettings(shapeSettings);
        colorGenerator.UpdateSettings(colorSettings);
        Initialize();
        UpdateNoiseLayerData();
        UpdateColorSettings();

        int numFaces = 6;
        int numPoints = resolution * resolution;
        ShapeData shapeData = new ShapeData { planetRadius = shapeSettings.planetRadius, sizeMult = shapeSettings.sizeMult };

        // We need a list of handles to wait for at the end
        NativeList<JobHandle> allHandles = new NativeList<JobHandle>(numFaces * 2, Allocator.Temp);

        // Arrays to hold the data per face
        var oceanPointData = new NativeArray<OceanPointData>[numFaces];
        var oceanVerts = new NativeList<float3>[numFaces];
        var oceanTriangles = new NativeList<int>[numFaces];
        var oceanUVs = new NativeList<float2>[numFaces];
        var edgeCellTriangles = new NativeArray<FixedList128Bytes<int>>(numFaces * (resolution - 1) * 4, Allocator.TempJob);
        var vertexCounts = new NativeArray<int>(numFaces, Allocator.Temp);

        var terrainVerts = new NativeArray<float3>[numFaces];
        var terrainTriangles = new NativeArray<int>[numFaces];
        var terrainUVs = new NativeArray<float2>[numFaces];

        for (int i = 0; i < numFaces; i++)
        {
            Vector3 localUp = directions[i];
            Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            Vector3 axisB = Vector3.Cross(localUp, axisA);

            if (terrainFilters[i].gameObject.activeSelf)
            {
                terrainVerts[i] = new NativeArray<float3>(numPoints, Allocator.TempJob);
                terrainTriangles[i] = new NativeArray<int>((resolution - 1) * (resolution - 1) * 6, Allocator.TempJob);
                terrainUVs[i] = new NativeArray<float2>(numPoints, Allocator.TempJob);
                var terrainJob = new PlanetTerrainJobs.TerrainGenJob
                {
                    resolution = resolution,
                    localUp = localUp,
                    axisA = axisA,
                    axisB = axisB,
                    shapeData = shapeData,
                    noiseLayers = _noiseLayers,
                    biomeData = _biomeData,

                    triangles = terrainTriangles[i],
                    vertices = terrainVerts[i],
                    uvs = terrainUVs[i],
                };

                // update uvs
                allHandles.Add(terrainJob.Schedule());
            }
            // OCEAN
            oceanPointData[i] = new NativeArray<OceanPointData>(numPoints, Allocator.TempJob);
            oceanVerts[i] = new NativeList<float3>(numPoints, Allocator.TempJob);
            oceanTriangles[i] = new NativeList<int>(numPoints * 6, Allocator.TempJob);
            oceanUVs[i] = new NativeList<float2>(numPoints, Allocator.TempJob);

            var oceanDataJob = new PlanetOceanJobs.OceanDataJob
            {
                resolution = resolution,
                localUp = localUp,
                axisA = axisA,
                axisB = axisB,
                shapeData = shapeData,
                noiseLayers = _noiseLayers,
                oceanLevel = oceanLevel,
                result = oceanPointData[i]
            };


            JobHandle dataHandle = oceanDataJob.Schedule(numPoints, 64);

            // --- JOB 2: BUILD MESH ---
            var meshJob = new PlanetOceanJobs.OceanMeshBuilderJob
            {
                resolution = resolution,
                faceIndex = i,
                planetRadius = shapeData.planetRadius,
                pointData = oceanPointData[i],
                
                vertices = oceanVerts[i],
                triangles = oceanTriangles[i],
                uvs = oceanUVs[i],

                edgeCellTriangles = edgeCellTriangles
            };
            JobHandle meshHandle = meshJob.Schedule(dataHandle);
            allHandles.Add(meshHandle);
            
        }

        // Wait for all faces to finish
        JobHandle.CompleteAll(allHandles.AsArray());

        for (int i = 0; i < numFaces; i++)
        {
            if (terrainVerts[i].IsCreated)
            {
                ApplyToMesh(terrainVerts[i], terrainTriangles[i], terrainUVs[i], terrainFilters[i]);
                terrainVerts[i].Dispose();
                terrainTriangles[i].Dispose();
                terrainUVs[i].Dispose();
                
            }

            if (!oceanPointData[i].IsCreated) continue;
            oceanPointData[i].Dispose();
        }

        for (int i = 0; i < numFaces; i++) vertexCounts[i] = oceanVerts[i].Length;
        (NativeArray<int> triangleOffsets, int totalTriCount) = GetTriangleOffsetsAndTotalTriangleCount(oceanTriangles);


        NativeArray<float3> combinedVertices = ConcatAndDisposeLists(oceanVerts, Allocator.TempJob);
        NativeArray<float2> combinedUvs = ConcatAndDisposeLists(oceanUVs, Allocator.TempJob);
        NativeArray<int> combinedTriangles = ConcatAndDisposeTriangles(totalTriCount, oceanTriangles, vertexCounts);
        
        var meshWeldJob = new MeshWeldJob { 
            edgeCellTriangles = edgeCellTriangles,
            resolution = resolution,
            triangleOffsets = triangleOffsets,
            triangles = combinedTriangles,
            vertices = combinedVertices,
        };
        JobHandle weldHandle = meshWeldJob.Schedule();
        weldHandle.Complete();

        edgeCellTriangles.Dispose();
        allHandles.Dispose();

        ApplyToMesh(combinedVertices, combinedTriangles, combinedUvs, oceanMeshFilter, true);

        combinedVertices.Dispose();
        combinedTriangles.Dispose();
        combinedUvs.Dispose();
        triangleOffsets.Dispose();
        vertexCounts.Dispose();
    }
    // can make this a job

    (NativeArray<int> triangleOffsets, int totalTriCount) GetTriangleOffsetsAndTotalTriangleCount(NativeList<int>[] oceanTriangles)
    {
        int numFaces = 6;
        NativeArray<int> triangleOffsets = new NativeArray<int>(numFaces, Allocator.TempJob);
        int totalTriCount = 0;
        int currentTriOffset = 0;
        for (int i = 0; i < oceanTriangles.Length; i++)
        {
            triangleOffsets[i] = currentTriOffset;
            int len = oceanTriangles[i].Length;
            currentTriOffset += len;
            totalTriCount += len;
        }
        return (triangleOffsets, totalTriCount);
    }

    NativeArray<int> ConcatAndDisposeTriangles(int totalTriCount, NativeList<int>[] oceanTriangles, NativeArray<int> vertexCounts)
    {
        NativeArray<int> combinedTriangles = new NativeArray<int>(totalTriCount, Allocator.TempJob);

        int writeIndex = 0;
        int globalVertexOffset = 0;
        int numFaces = 6;
        for (int i = 0; i < numFaces; i++)
        {
            NativeArray<int> sourceTris = oceanTriangles[i].AsArray();
            for (int k = 0; k < sourceTris.Length; k++)
            {
                // Shift the index to point to the correct spot in combinedVertices
                combinedTriangles[writeIndex++] = sourceTris[k] + globalVertexOffset;
            }

            // Prepare offset for next face
            globalVertexOffset += vertexCounts[i];

            // Dispose the source list
            oceanTriangles[i].Dispose();
        }
        return combinedTriangles;
    }

    NativeArray<T> ConcatAndDisposeLists<T>(NativeList<T>[] lists, Allocator allocator)
    where T : unmanaged
    {
        int totalLength = 0;
        for (int i = 0; i < lists.Length; i++)
            totalLength += lists[i].Length;

        NativeArray<T> combined = new NativeArray<T>(totalLength, allocator);

        int offset = 0;

        for (int i = 0; i < lists.Length; i++)
        {
            NativeArray<T>.Copy(lists[i].AsArray(), 0, combined, offset, lists[i].Length);
            offset += lists[i].Length;

            lists[i].Dispose(); // Dispose OWNER here
        }

        return combined;
    }

    void Initialize()
    {
        oceanMeshFilter = SetupMeshObject(oceanMeshFilter, "oceanMesh", colorSettings.oceanMat, false);

        if (terrainFilters == null || terrainFilters.Length == 0)
            terrainFilters = new MeshFilter[6];
        for (int i = 0; i < 6; i++)
        {
            terrainFilters[i] = SetupMeshObject(terrainFilters[i], "terrainMesh", colorSettings.planetMat, false);
        }
    }

    void ApplyToMesh(NativeArray<float3> vertices, NativeArray<int> triangles, NativeArray<float2> uvs, MeshFilter filter, bool bigMesh = false)
    {
        Mesh mesh = filter.sharedMesh;
        if (mesh == null) mesh = new Mesh();
        mesh.Clear();
        if(bigMesh) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        mesh.SetVertices(vertices);
        mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
        mesh.SetUVs(0, uvs);

        mesh.RecalculateNormals();
        filter.sharedMesh = mesh;
    }

    void UpdateNoiseLayerData()
    {
        if (_noiseLayers.IsCreated) _noiseLayers.Dispose();

        if (shapeSettings == null || shapeSettings.noiseLayers == null) return;

        _noiseLayers = new NativeArray<NoiseLayerData>(shapeSettings.noiseLayers.Length, Allocator.Persistent);
        for (int i = 0; i < shapeSettings.noiseLayers.Length; i++)
        {
            var s = shapeSettings.noiseLayers[i];
            var simpleSettings = s.noiseSettings.filterType == NoiseSettings.FilterType.Rigid ? s.noiseSettings.rigidNoiseSettings : s.noiseSettings.simpleNoiseSettings;
            _noiseLayers[i] = new NoiseLayerData
            {
                enabled = s.enabled,
                useFirstLayerAsMask = s.useFirstLayerAsMask,
                settings = new SimpleNoiseSettings
                {
                    strength = simpleSettings.strength,
                    baseRoughness = simpleSettings.baseRoughness,
                    roughness = simpleSettings.roughness,
                    persistence = simpleSettings.persistence,
                    center = simpleSettings.centre,
                    minValue = simpleSettings.minValue,
                    numLayers = simpleSettings.numLayers,

                    isRigid = s.noiseSettings.filterType == NoiseSettings.FilterType.Rigid,
                    weightMultiplier = s.noiseSettings.rigidNoiseSettings.weightMultiplier,
                }
            };
        }
    }

    void UpdateColorSettings()
    {
        if(_biomeData.startHeights.IsCreated) _biomeData.startHeights.Dispose();
        if (colorSettings == null) return;
        ColorSettings.BiomeColorSettings s = colorSettings.biomeColorSettings;
        _biomeData.startHeights = new NativeArray<float>(s.biomes.Length, Allocator.Persistent);
        for (int i = 0; i < s.biomes.Length; i++)
        {
            _biomeData.startHeights[i] = s.biomes[i].startHeight;
        }
        var simpleSettings = s.noise.filterType == NoiseSettings.FilterType.Rigid ? s.noise.rigidNoiseSettings : s.noise.simpleNoiseSettings;

        _biomeData.noiseData = new SimpleNoiseSettings
        {
            strength = simpleSettings.strength,
            baseRoughness = simpleSettings.baseRoughness,
            roughness = simpleSettings.roughness,
            persistence = simpleSettings.persistence,
            center = simpleSettings.centre,
            minValue = simpleSettings.minValue,
            numLayers = simpleSettings.numLayers,

            isRigid = s.noise.filterType == NoiseSettings.FilterType.Rigid,
            weightMultiplier = s.noise.rigidNoiseSettings.weightMultiplier,
        };

        _biomeData.noiseOffset = s.noiseOffset;
        _biomeData.noiseStrength = s.noiseStrength;
        _biomeData.blendAmount = s.blendAmount;
    }

    MeshFilter SetupMeshObject(MeshFilter existingFilter, string objName, Material material, bool hasCollider)
    {
        MeshCollider meshCollider = null;
        if (existingFilter == null)
        {
            GameObject meshObj = new(objName);
            meshObj.transform.parent = transform;
            meshObj.AddComponent<MeshRenderer>();
            if (hasCollider)
            {
                meshCollider = meshObj.AddComponent<MeshCollider>();
            }
            existingFilter = meshObj.AddComponent<MeshFilter>();
            existingFilter.sharedMesh = new Mesh();
        }

        existingFilter.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (hasCollider)
        {
            if (meshCollider == null) meshCollider = existingFilter.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null) meshCollider = existingFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = existingFilter.sharedMesh;
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(existingFilter);
        UnityEditor.EditorUtility.SetDirty(existingFilter.gameObject);
#endif
        return existingFilter;
    }

    
}