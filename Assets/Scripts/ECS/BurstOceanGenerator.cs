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

        // We need a list of handles to wait for at the end
        NativeArray<JobHandle> handles = new NativeArray<JobHandle>(numFaces, Allocator.Temp);

        // Arrays to hold the data per face
        var allPointData = new NativeArray<OceanPointData>[numFaces];
        var allVertices = new NativeList<float3>[numFaces];
        var allTriangles = new NativeList<int>[numFaces];
        var allUVs = new NativeList<float2>[numFaces];

        ShapeData shapeData = new ShapeData { planetRadius = settings.planetRadius, sizeMult = settings.sizeMult };

        for (int i = 0; i < numFaces; i++)
        {
            if (!oceanFilters[i].gameObject.activeSelf)
            {
                handles[i] = default; // Mark unused handles as completed/default
                continue;
            }

            Vector3 localUp = directions[i];
            Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
            Vector3 axisB = Vector3.Cross(localUp, axisA);

            // --- ALLOCATE MEMORY ---
            allPointData[i] = new NativeArray<OceanPointData>(numPoints, Allocator.TempJob);
            // FIX: Initialize these lists!
            allVertices[i] = new NativeList<float3>(numPoints, Allocator.TempJob);
            allTriangles[i] = new NativeList<int>(numPoints * 6, Allocator.TempJob);
            allUVs[i] = new NativeList<float2>(numPoints, Allocator.TempJob);

            // --- JOB 1: CALCULATE DATA ---
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

            JobHandle dataHandle = dataJob.Schedule(numPoints, 64);

            // --- JOB 2: BUILD MESH ---
            // Dependent on dataHandle finishing
            var meshJob = new PlanetOceanJobs.OceanMeshBuilderJob
            {
                resolution = resolution,
                pointData = allPointData[i],
                vertices = allVertices[i],
                triangles = allTriangles[i],
                uvs = allUVs[i]
            };

            // Schedule single-threaded mesh build, dependent on the calculation
            handles[i] = meshJob.Schedule(dataHandle);
        }

        // Wait for all faces to finish
        JobHandle.CompleteAll(handles);

        // --- APPLY TO UNITY MESHES ---
        for (int i = 0; i < numFaces; i++)
        {
            // FIX: Check if the list was actually allocated (activeSelf check)
            if (allVertices[i].IsCreated == false) continue;

            Mesh mesh = oceanFilters[i].sharedMesh;
            if (mesh == null) mesh = new Mesh();
            mesh.Clear();

            // Set data (using AsArray to view NativeList as an array)
            mesh.SetVertices(allVertices[i].AsArray());
            mesh.SetIndices(allTriangles[i].AsArray(), MeshTopology.Triangles, 0);
            mesh.SetUVs(0, allUVs[i].AsArray());

            mesh.RecalculateNormals();
            // Optional: mesh.RecalculateBounds();

            oceanFilters[i].sharedMesh = mesh;

            // --- CLEANUP ---
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