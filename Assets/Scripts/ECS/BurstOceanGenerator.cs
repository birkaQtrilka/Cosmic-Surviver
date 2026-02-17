using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class BurstOceanGenerator : MonoBehaviour
{
    public ShapeSettings shapeSettings;
    public ColorSettings colorSettings;
    public MeshFilter[] oceanFilters; // Assign the 6 mesh filters
    public MeshFilter[] terrainFilters; // Assign the 6 mesh filters
    public int resolution = 32;
    public float oceanLevel = 0f;

    private NativeArray<NoiseLayerData> _noiseLayers;
    readonly Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
    readonly ShapeGenerator shapeGenerator = new();
    readonly ColorGenerator colorGenerator = new();

    void OnValidate()
    {
        if (Application.isPlaying) GenerateOcean();
    }

    void OnDestroy()
    {
        if (_noiseLayers.IsCreated) _noiseLayers.Dispose();
    }

    [ContextMenu("Generate Ocean")]
    public void GenerateOcean()
    {
        Initialize();
        UpdateNoiseLayerData();
        shapeGenerator.UpdateSettings(shapeSettings);
        colorGenerator.UpdateSettings(colorSettings);

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

                    triangles = terrainTriangles[i],
                    vertices = terrainVerts[i],
                    uvs = terrainUVs[i],
                };
                allHandles.Add(terrainJob.Schedule());
            }

            if (terrainFilters[i].gameObject.activeSelf)
            {
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
                    planetRadius = shapeData.planetRadius,
                    pointData = oceanPointData[i],
                    vertices = oceanVerts[i],
                    triangles = oceanTriangles[i],
                    uvs = oceanUVs[i]
                };
                JobHandle meshHandle = meshJob.Schedule(dataHandle);
                allHandles.Add(meshHandle);
            }
            

        }

        // Wait for all faces to finish
        JobHandle.CompleteAll(allHandles.AsArray());
        allHandles.Dispose();

        for (int i = 0; i < numFaces; i++)
        {
            if (terrainVerts[i].IsCreated)
            {
                ApplyToMesh(i, terrainVerts[i], terrainTriangles[i], terrainUVs[i], terrainFilters[i]);
                terrainVerts[i].Dispose();
                terrainTriangles[i].Dispose();
                terrainUVs[i].Dispose();
                
            }

            if (!oceanVerts[i].IsCreated) continue;
            ApplyToMesh(i, oceanVerts[i].AsArray(), oceanTriangles[i].AsArray(), oceanUVs[i].AsArray(), oceanFilters[i]);

            oceanVerts[i].Dispose();
            oceanTriangles[i].Dispose();
            oceanUVs[i].Dispose();
            oceanPointData[i].Dispose();
        }

    }

    void Initialize()
    {
        if (oceanFilters == null || oceanFilters.Length == 0)
            oceanFilters = new MeshFilter[6];
        for (int i = 0; i < 6; i++)
        {
            oceanFilters[i] = SetupMeshObject(oceanFilters[i], "oceanMesh", colorSettings.oceanMat, false);
        }

        if (terrainFilters == null || terrainFilters.Length == 0)
            terrainFilters = new MeshFilter[6];
        for (int i = 0; i < 6; i++)
        {
            terrainFilters[i] = SetupMeshObject(terrainFilters[i], "terrainMesh", colorSettings.planetMat, false);
        }
    }

    void ApplyToMesh(int i, NativeArray<float3> vertices, NativeArray<int> triangles, NativeArray<float2> uvs, MeshFilter filter)
    {
        Mesh mesh = filter.sharedMesh;
        if (mesh == null) mesh = new Mesh();
        mesh.Clear();

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