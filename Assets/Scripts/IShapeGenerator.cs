using UnityEngine;

public interface IShapeGenerator 
{
    public void UpdateSettings(ShapeSettings settings);
    public float CalculateUnscaledElevation(Vector3 pointOnUnitSphere);
    public float GetScaledElevation(float unscaledElevation);
}
