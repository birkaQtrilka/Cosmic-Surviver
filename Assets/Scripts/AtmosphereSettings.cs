using UnityEngine;
[CreateAssetMenu()]
public class AtmosphereSettings : ScriptableObject
{
    public Color color = new Color(0.4f, 0.6f, 1.0f);
    public Color darkColor = new Color(0.4f, 0.6f, 1.0f);
    public float thickness = 100f;
    [Range(0,1)] public float intensity = 1f;

}
