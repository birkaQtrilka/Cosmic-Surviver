using UnityEngine;

public struct OceanVertData
{
    public Vector3 WorldPos { get; set; }//point on sphere
    public float DistanceToZero;
    public bool isOcean;
    public bool isShore;
    public int Index { get; set; }
}
