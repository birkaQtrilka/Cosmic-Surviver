using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public static class PlanetOceanJobs
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float EvaluateNoise(float3 point, SimpleNoiseSettings settings)
    {
        float noiseValue = 0;
        float frequency = settings.baseRoughness;
        float amplitude = 1;

        for (int i = 0; i < settings.numLayers; i++)
        {
            float v = noise.snoise(point * frequency + settings.center);
            noiseValue += (v + 1) * .5f * amplitude;
            frequency *= settings.roughness;
            amplitude *= settings.persistence;
        }

        noiseValue = math.max(0, noiseValue - settings.minValue);
        return noiseValue * settings.strength;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 GetUnitSpherePoint(int x, int y, int resolution, float3 localUp, float3 axisA, float3 axisB)
    {
        float2 percent = new float2(x, y) / (resolution - 1);
        float3 pointOnCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;

        float3 p = pointOnCube;
        float x2 = p.x * p.x;
        float y2 = p.y * p.y;
        float z2 = p.z * p.z;

        float3 s;
        s.x = p.x * math.sqrt(1f - y2 * 0.5f - z2 * 0.5f + y2 * z2 / 3f);
        s.y = p.y * math.sqrt(1f - z2 * 0.5f - x2 * 0.5f + z2 * x2 / 3f);
        s.z = p.z * math.sqrt(1f - x2 * 0.5f - y2 * 0.5f + x2 * y2 / 3f);
        return math.normalize(s);
    }

    // --- Grid Navigator Logic (Baked into Static Function) ---
    // Maps the 16 cases to lists of actions (Triangulation)
    // 0 = TopLeft, 1 = TopRight, 2 = BottomRight, 3 = BottomLeft
    public static void GetCellInstructions(int contourIndex, ref NativeList<CellAction> actions)
    {
        actions.Clear();
        switch (contourIndex)
        {
            case 0: break; // No triangles
            case 1: // west, south, down
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(2, 3)); actions.Add(new CellAction(3));
                break;
            case 2: // east, downRight, south
                actions.Add(new CellAction(1, 2)); actions.Add(new CellAction(2)); actions.Add(new CellAction(2, 3));
                break;
            case 3: // west, downRight, down | west, east, downRight
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(2)); actions.Add(new CellAction(3));
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(1, 2)); actions.Add(new CellAction(2));
                break;
            case 4: // north, right, east
                actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(1)); actions.Add(new CellAction(1, 2));
                break;
            case 5: // west, south, down | west, north, south | north, east, south | north, right, east
                // This is a complex case (Saddle point?), simplifying to basic quad split or filling
                // Translating directly from provided code:
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(2, 3)); actions.Add(new CellAction(3));
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(2, 3));
                actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(1, 2)); actions.Add(new CellAction(2, 3));
                actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(1)); actions.Add(new CellAction(1, 2));
                break;
            case 6: // north, right, downRight | north, downRight, south
                actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(1)); actions.Add(new CellAction(2));
                actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(2)); actions.Add(new CellAction(2, 3));
                break;
            case 7:
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(2)); actions.Add(new CellAction(3));
                actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(2)); actions.Add(new CellAction(3, 0));
                actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(1)); actions.Add(new CellAction(2));
                break;
            case 8: // west, origin, north
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(0)); actions.Add(new CellAction(0, 1));
                break;
            case 9: // origin, north, south | origin, south, down
                actions.Add(new CellAction(0)); actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(2, 3));
                actions.Add(new CellAction(0)); actions.Add(new CellAction(2, 3)); actions.Add(new CellAction(3));
                break;
            case 10:
                actions.Add(new CellAction(0)); actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(3, 0));
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(1, 2));
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(1, 2)); actions.Add(new CellAction(2, 3));
                actions.Add(new CellAction(2, 3)); actions.Add(new CellAction(1, 2)); actions.Add(new CellAction(2));
                break;
            case 11:
                actions.Add(new CellAction(0)); actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(3));
                actions.Add(new CellAction(0, 1)); actions.Add(new CellAction(1, 2)); actions.Add(new CellAction(3));
                actions.Add(new CellAction(1, 2)); actions.Add(new CellAction(2)); actions.Add(new CellAction(3));
                break;
            case 12: // west, origin, east | origin, right, east
                actions.Add(new CellAction(3, 0)); actions.Add(new CellAction(0)); actions.Add(new CellAction(1, 2));
                actions.Add(new CellAction(0)); actions.Add(new CellAction(1)); actions.Add(new CellAction(1, 2));
                break;
            case 13:
                actions.Add(new CellAction(0)); actions.Add(new CellAction(1)); actions.Add(new CellAction(1, 2));
                actions.Add(new CellAction(0)); actions.Add(new CellAction(1, 2)); actions.Add(new CellAction(2, 3));
                actions.Add(new CellAction(0)); actions.Add(new CellAction(2, 3)); actions.Add(new CellAction(3));
                break;
            case 14:
                actions.Add(new CellAction(0)); actions.Add(new CellAction(1)); actions.Add(new CellAction(3, 0));
                actions.Add(new CellAction(1)); actions.Add(new CellAction(2)); actions.Add(new CellAction(2, 3));
                actions.Add(new CellAction(1)); actions.Add(new CellAction(2, 3)); actions.Add(new CellAction(3, 0));
                break;
            case 15: // origin, right, downRight | origin, downRight, down
                actions.Add(new CellAction(0)); actions.Add(new CellAction(1)); actions.Add(new CellAction(2));
                actions.Add(new CellAction(0)); actions.Add(new CellAction(2)); actions.Add(new CellAction(3));
                break;
        }
    }

    // --- JOBS ---

    // 1. Calculate Raw Data (Fully Parallel)
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    public struct OceanDataJob : IJobParallelFor
    {
        [ReadOnly] public int resolution;
        [ReadOnly] public float3 localUp;
        [ReadOnly] public float3 axisA;
        [ReadOnly] public float3 axisB;
        [ReadOnly] public ShapeData shapeData;
        [ReadOnly, DeallocateOnJobCompletion] public NativeArray<NoiseLayerData> noiseLayers;
        [ReadOnly] public float oceanLevel;

        [WriteOnly] public NativeArray<OceanPointData> result;

        public void Execute(int i)
        {
            int y = i / resolution;
            int x = i % resolution;

            float3 point = GetUnitSpherePoint(x, y, resolution, localUp, axisA, axisB);

            // Re-implement CalculateUnscaledElevation inline
            float firstLayerValue = 0;
            float elevation = 0;
            if (noiseLayers.Length > 0)
            {
                firstLayerValue = EvaluateNoise(point, noiseLayers[0].settings);
                if (noiseLayers[0].enabled) elevation = firstLayerValue;
            }
            for (int k = 1; k < noiseLayers.Length; k++)
            {
                if (noiseLayers[k].enabled)
                {
                    float mask = noiseLayers[k].useFirstLayerAsMask ? firstLayerValue : 1;
                    elevation += EvaluateNoise(point, noiseLayers[k].settings) * mask;
                }
            }
            // Logic from ShapeGenerator: elevation *= elevation < 0 ? 1 : sizeMult;
            elevation *= (elevation < 0) ? 1 : shapeData.sizeMult;

            result[i] = new OceanPointData
            {
                worldPos = point * shapeData.planetRadius,
                unscaledElevation = elevation,
                distToOcean = elevation - oceanLevel,
                isOcean = (elevation - oceanLevel) <= 0
            };
        }
    }

    // 2. Build Topology (Single-Threaded Burst)
    // We use IJob because the "Welding" logic is stateful and difficult to parallelize without
    // race conditions, but Burst makes this extremely fast anyway.
    [BurstCompile(CompileSynchronously = true)]
    public struct OceanTopologyJob : IJob
    {
        [ReadOnly] public int resolution;
        [ReadOnly] public float planetRadius;
        [ReadOnly] public float3 localUp;
        [ReadOnly] public float3 axisA;
        [ReadOnly] public float3 axisB;

        [ReadOnly] public NativeArray<OceanPointData> pointData; // Input from Job 1

        public NativeList<float3> outVertices;
        public NativeList<int> outTriangles;
        public NativeList<float2> outUVs;

        public void Execute()
        {
            // Map: GridIndex -> MeshVertexIndex (for existing corners)
            NativeArray<int> cornerVertexMap = new NativeArray<int>(pointData.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            // Map: EdgeKey -> MeshVertexIndex (for interpolated edge vertices)
            // Key = Unique ID for an edge.
            // Horizontal Edge (x,y)-(x+1,y): ID = y * res + x
            // Vertical Edge (x,y)-(x,y+1): ID = (res*res) + y*res + x
            // To be safe, we use int2 as key: (smallerIndex, largerIndex)
            NativeParallelHashMap<int2, int> edgeVertexMap = new NativeParallelHashMap<int2, int>(pointData.Length, Allocator.Temp);

            // Initialize map with -1
            for (int i = 0; i < cornerVertexMap.Length; i++) cornerVertexMap[i] = -1;

            // Reusable buffer for instructions
            NativeList<CellAction> instructions = new NativeList<CellAction>(20, Allocator.Temp);

            // Loop through cells
            for (int y = 0; y < resolution - 1; y++)
            {
                for (int x = 0; x < resolution - 1; x++)
                {
                    int i = x + y * resolution;

                    // Corner Indices
                    int c0 = i;                  // Top Left
                    int c1 = i + 1;              // Top Right
                    int c2 = i + resolution + 1; // Bottom Right
                    int c3 = i + resolution;     // Bottom Left

                    bool b0 = pointData[c0].isOcean;
                    bool b1 = pointData[c1].isOcean;
                    bool b2 = pointData[c2].isOcean;
                    bool b3 = pointData[c3].isOcean;

                    // Optimization: Skip if all land
                    if (!b0 && !b1 && !b2 && !b3) continue;

                    // Calculate Contour Case (Bitmask)
                    // Original: a*8 + b*4 + c*2 + d*1 -> 0(TL), 1(TR), 2(BR), 3(BL)
                    int contour = (b0 ? 8 : 0) + (b1 ? 4 : 0) + (b2 ? 2 : 0) + (b3 ? 1 : 0);

                    GetCellInstructions(contour, ref instructions);

                    // Execute instructions to build triangles
                    // Instructions come in groups of 3 (triangles)
                    for (int k = 0; k < instructions.Length; k++)
                    {
                        CellAction action = instructions[k];
                        int vertIndex = -1;

                        if (action.type == 0) // Existing Corner
                        {
                            int gridIndex = 0;
                            switch (action.idxA)
                            {
                                case 0: gridIndex = c0; break;
                                case 1: gridIndex = c1; break;
                                case 2: gridIndex = c2; break;
                                case 3: gridIndex = c3; break;
                            }
                            vertIndex = GetOrAddCorner(gridIndex, ref cornerVertexMap);
                        }
                        else // Interpolated Edge
                        {
                            int gA = 0, gB = 0;
                            // Map local 0..3 to global indices
                            switch (action.idxA) { case 0: gA = c0; break; case 1: gA = c1; break; case 2: gA = c2; break; case 3: gA = c3; break; }
                            switch (action.idxB) { case 0: gB = c0; break; case 1: gB = c1; break; case 2: gB = c2; break; case 3: gB = c3; break; }

                            vertIndex = GetOrAddEdge(gA, gB, ref edgeVertexMap);
                        }

                        outTriangles.Add(vertIndex);
                    }
                }
            }

            cornerVertexMap.Dispose();
            edgeVertexMap.Dispose();
            instructions.Dispose();
        }

        private int GetOrAddCorner(int gridIndex, ref NativeArray<int> map)
        {
            if (map[gridIndex] != -1) return map[gridIndex];

            // Add new vertex
            OceanPointData p = pointData[gridIndex];

            // Corner is simply the world pos
            outVertices.Add(p.worldPos);
            outUVs.Add(GetUV(math.normalize(p.worldPos), localUp)); // Simple UV

            int newIndex = outVertices.Length - 1;
            map[gridIndex] = newIndex;
            return newIndex;
        }

        private int GetOrAddEdge(int indexA, int indexB, ref NativeParallelHashMap<int2, int> map)
        {
            // Create a unique key for this edge (order independent)
            int min = math.min(indexA, indexB);
            int max = math.max(indexA, indexB);
            int2 key = new int2(min, max);

            if (map.TryGetValue(key, out int existingIndex))
            {
                return existingIndex;
            }

            // Calculate Interpolated Position
            OceanPointData pA = pointData[indexA];
            OceanPointData pB = pointData[indexB];

            // Logic: LerpCloseToOne
            // a = dist + 1, b = dist + 1. Target is 1.
            float valA = pA.distToOcean + 1f;
            float valB = pB.distToOcean + 1f;
            float t = (1f - valA) / (valB - valA);

            // Interpolate directly on Unit Sphere to maintain curvature, then scale
            float3 spherePosA = math.normalize(pA.worldPos);
            float3 spherePosB = math.normalize(pB.worldPos);
            float3 finalSpherePos = math.normalize(math.lerp(spherePosA, spherePosB, t));

            float3 finalPos = finalSpherePos * planetRadius;

            outVertices.Add(finalPos);
            outUVs.Add(GetUV(finalSpherePos, localUp));

            int newIndex = outVertices.Length - 1;
            map.Add(key, newIndex);
            return newIndex;
        }

        private float2 GetUV(float3 dir, float3 up) 
        {
            // Placeholder UV logic matching original roughly
            // Since we don't have the ColorGenerator here, we return a basic blend
            return new float2(0.5f, 0.5f);
        }
    }
}