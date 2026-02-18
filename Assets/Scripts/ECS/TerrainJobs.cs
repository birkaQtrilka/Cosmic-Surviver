using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public static class PlanetTerrainJobs
{
    // 1. Calculate Raw Data (Fully Parallel)
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct TerrainGenJob : IJob
    {
        [ReadOnly] public int resolution;
        [ReadOnly] public float3 localUp;
        [ReadOnly] public float3 axisA;
        [ReadOnly] public float3 axisB;
        [ReadOnly] public ShapeData shapeData;
        [ReadOnly, NativeDisableContainerSafetyRestriction] public NativeArray<NoiseLayerData> noiseLayers;
        [ReadOnly, NativeDisableContainerSafetyRestriction] public BiomeData biomeData;

        public NativeArray<float3> vertices;
        public NativeArray<int> triangles;
        public NativeArray<float2> uvs;
        public float2 minMax;

        void InitMinMax()
        {
            minMax = new float2(float.MinValue, float.MaxValue);
        }

        void AddMinMAx(float v)
        {
            if (v < minMax.x)
                minMax.x = v;
            if (v > minMax.y)
                minMax.y = v;
        }

        public float BiomePercentFromPoint(float3 pointOnSphere)
        {
            float heightPersent = (pointOnSphere.y + 1) / 2f;
            heightPersent += (PlanetOceanJobs.EvaluateNoise(pointOnSphere, biomeData.noiseData) - biomeData.noiseOffset) * biomeData.noiseStrength;
            float biomeIndex = 0;
            int numBiomes = biomeData.startHeights.Length;
            float blendRange = biomeData.blendAmount / 2 + .001f;

            for (int i = 0; i < numBiomes; i++)
            {
                float dist = heightPersent - biomeData.startHeights[i];
                float weight = math.unlerp(-blendRange, blendRange, dist);
                biomeIndex *= (1 - weight);
                biomeIndex += i * weight;

            }
            return biomeIndex / math.max(1, (numBiomes - 1));
        }

        public void Execute()
        {
            int triIndex = 0;
            InitMinMax();
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int i = x + y * resolution;


                    float3 point = PlanetOceanJobs.GetUnitSpherePoint(x, y, resolution, localUp, axisA, axisB);

                    float elevation = PlanetOceanJobs.CalculateLayeredElevation(point, noiseLayers);
                    elevation *= (elevation < 0) ? 1 : shapeData.sizeMult;
                    elevation -= 0.005f; // TODO: substract ocean level
                    AddMinMAx(elevation);
                    float scaledElevation = PlanetOceanJobs.GetScaledElevation(elevation, shapeData.planetRadius);

                    float3 vertex = scaledElevation * point;
                    vertices[i] = vertex;
                    uvs[i] = new float2(BiomePercentFromPoint(point), elevation);

                    if (x == resolution - 1 || y == resolution - 1) continue;

                    triangles[triIndex] = i;
                    triangles[triIndex + 1] = i + resolution + 1;
                    triangles[triIndex + 2] = i + resolution;

                    triangles[triIndex + 3] = i;
                    triangles[triIndex + 4] = i + 1;
                    triangles[triIndex + 5] = i + resolution + 1;
                    triIndex += 6;
                }
            }
        }
    }
}
