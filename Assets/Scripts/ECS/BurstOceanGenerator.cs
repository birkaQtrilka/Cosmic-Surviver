using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class BurstOceanGenerator : MonoBehaviour
{
    public ShapeSettings settings;
    public ColorSettings colorSettings;
    public MeshFilter[] oceanFilters; // Assign the 6 mesh filters
    public int resolution = 32;
    public float oceanLevel = 0f;

    private NativeArray<NoiseLayerData> _noiseLayers;

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
        Initialzie();
        UpdateNoiseLayerData();

        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        int numFaces = 6;
        int numPoints = resolution * resolution;

        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(numFaces, Allocator.Temp);

        // Buffers per face
        var allPointData = new NativeArray<OceanPointData>[numFaces];
        var allVertices = new NativeList<float3>[numFaces];
        var allTriangles = new NativeList<int>[numFaces];
        var allUVs = new NativeList<float2>[numFaces];
        ShapeData shapeData = new ShapeData { planetRadius = settings.planetRadius, sizeMult = settings.sizeMult };

        // 1. Schedule Data Jobs (Parallel)
        JobHandle allDataJobsHandle = default;

        for (int i = 0; i < numFaces; i++)
        {
            if (!oceanFilters[i].gameObject.activeSelf) continue;

            Vector3 localUp = directions[i];
            Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            Vector3 axisB = Vector3.Cross(localUp, axisA);

            allPointData[i] = new NativeArray<OceanPointData>(numPoints, Allocator.TempJob);

            var dataJob = new PlanetOceanJobs.OceanDataJob
            {
                resolution = resolution,
                localUp = localUp,
                axisA = axisA,
                axisB = axisB,
                shapeData = shapeData,
                noiseLayers = _noiseLayers,
                oceanLevel = oceanLevel,
                result = allPointData[i]
            };

            // Schedule all data jobs in parallel (no dependency between them)
            handles[i] = dataJob.Schedule(numPoints, 64);
        }

        // Combine all handles into one
        allDataJobsHandle = JobHandle.CombineDependencies(handles);
        allDataJobsHandle.Complete();

        // 2. Schedule Topology Jobs (Single Threaded Burst per face)
        // We run these after data is complete. 
        // We could chain dependencies, but manual loops are clearer for array management here.

        for (int i = 0; i < numFaces; i++)
        {
            if (!allPointData[i].IsCreated) continue;

            allVertices[i] = new NativeList<float3>(numPoints, Allocator.TempJob);
            allTriangles[i] = new NativeList<int>(numPoints * 6, Allocator.TempJob);
            allUVs[i] = new NativeList<float2>(numPoints, Allocator.TempJob);

            Vector3 localUp = directions[i];
            Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            Vector3 axisB = Vector3.Cross(localUp, axisA);

            var topoJob = new PlanetOceanJobs.OceanTopologyJob
            {
                resolution = resolution,
                planetRadius = settings.planetRadius,
                localUp = localUp,
                axisA = axisA,
                axisB = axisB,
                pointData = allPointData[i],
                outVertices = allVertices[i],
                outTriangles = allTriangles[i],
                outUVs = allUVs[i]
            };

            handles[i] = topoJob.Schedule();
        }

        JobHandle.CompleteAll(handles);

        // 3. Apply to Meshes
        for (int i = 0; i < numFaces; i++)
        {
            if (!allVertices[i].IsCreated) continue;

            Mesh mesh = oceanFilters[i].sharedMesh;
            if (mesh == null) mesh = new Mesh();
            mesh.Clear();

            // NativeList can be cast to Array/Slice for SetVertices

            mesh.SetVertices(allVertices[i].AsArray());
            mesh.SetIndices(
                allTriangles[i].AsArray(),
                MeshTopology.Triangles,
                0
            );
            mesh.SetUVs(0, allUVs[i].AsArray());

            mesh.RecalculateNormals();
            oceanFilters[i].sharedMesh = mesh;

            // Dispose
            allPointData[i].Dispose();
            allVertices[i].Dispose();
            allTriangles[i].Dispose();
            allUVs[i].Dispose();
        }

        handles.Dispose();
    }

    void Initialzie()
    {
        if (oceanFilters == null || oceanFilters.Length == 0)
            oceanFilters = new MeshFilter[6];
        for (int i = 0; i < 6; i++)
        {
            oceanFilters[i] = SetupMeshObject(oceanFilters[i], "oceanMesh", colorSettings.oceanMat, false);
        }
    }

    void UpdateNoiseLayerData()
    {
        if (_noiseLayers.IsCreated) _noiseLayers.Dispose();

        if (settings == null || settings.noiseLayers == null) return;

        _noiseLayers = new NativeArray<NoiseLayerData>(settings.noiseLayers.Length, Allocator.Persistent);
        for (int i = 0; i < settings.noiseLayers.Length; i++)
        {
            var s = settings.noiseLayers[i];
            var simpleSettings = s.noiseSettings.simpleNoiseSettings;
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
                    numLayers = simpleSettings.numLayers
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