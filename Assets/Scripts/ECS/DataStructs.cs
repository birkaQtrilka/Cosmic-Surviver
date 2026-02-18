using Unity.Collections;
using Unity.Mathematics;

[System.Serializable]
public struct SimpleNoiseSettings
{
    public float strength;
    public float baseRoughness;
    public float roughness;
    public float persistence;
    public float minValue;
    public int numLayers;
    public float3 center;
    // a valid value means that is a RigidNoiseSettings
    public bool isRigid;
    public float weightMultiplier;
}

public struct BiomeData
{
    public SimpleNoiseSettings noiseData;
    public float noiseOffset;
    public float noiseStrength;
    public float blendAmount;
    public NativeArray<float> startHeights;
}

public struct WeldData
{
    public int triangleStart;
    // 2d array. to get position
    public NativeArray<NativeList<int>> edgeCellTriangles;

    public static NativeList<int> GetCell(int x, int y,   NativeArray<NativeList<int>> flattenedGridArray, int resolution)
    {
        return flattenedGridArray[y * resolution + x];
    }
}

[System.Serializable]
public struct NoiseLayerData
{
    public bool enabled;
    public bool useFirstLayerAsMask;
    public SimpleNoiseSettings settings;
}

public struct ShapeData
{
    public float planetRadius;
    public float sizeMult;
}

// Holds the calculated data for a single point (Pass 1)
public struct OceanPointData
{
    public float3 worldPos;
    public float unscaledElevation;
    public float distToOcean;
    public bool isOcean;
}
// Represents an instruction from the GridNavigator lookup
// Type 0 = Use existing Corner (0=TL, 1=TR, 2=BR, 3=BL)
// Type 1 = Create Vertex on Edge between CornerA and CornerB
public struct CellAction
{
    public byte type;
    public byte idxA;
    public byte idxB;

    public CellAction(int cornerIndex)
    { type = 0; idxA = (byte)cornerIndex; idxB = 0; }

    public CellAction(int cornerA, int cornerB)
    { type = 1; idxA = (byte)cornerA; idxB = (byte)cornerB; }
}