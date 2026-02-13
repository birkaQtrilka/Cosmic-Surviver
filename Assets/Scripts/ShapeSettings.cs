using UnityEngine;
[CreateAssetMenu()]
public class ShapeSettings : ScriptableObject
{
    public float planetRadius = 1;
    public float sizeMult = .5f;
    public NoiseLayer[] noiseLayers;
    [System.Serializable]
    public class NoiseLayer
    {
        public bool enabled = true;
        public bool useFirstLayerAsMask;
        public NoiseSettings noiseSettings;
    }
    public void SetActiveAllNoises(bool flag)
    {
        foreach (var noiseLayer in noiseLayers)
            noiseLayer.enabled = flag;
    }

}
