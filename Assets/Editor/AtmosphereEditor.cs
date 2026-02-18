using UnityEditor;

[CustomEditor(typeof(PlanetAtmosphere))]
public class AtmosphereEditor : Editor
{
    PlanetAtmosphere atmosphere;

    Editor atmosphereEditor;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        PlanetEditor.DrawSettingsEditor(atmosphere.atmosphereSettings, null, ref atmosphere.atmosphereSettingsFoldout, ref atmosphereEditor);
    }

    private void OnEnable()
    {
        atmosphere = target as PlanetAtmosphere;
    }
}
