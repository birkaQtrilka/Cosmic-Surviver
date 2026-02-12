using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(Planet))]
public class PlanetEditor : Editor
{
    Planet planet;
    Editor shapeEditor;
    Editor colourEditor;
    Editor atmosphereEditor;

    public override void OnInspectorGUI()
    {
        using (var check = new EditorGUI.ChangeCheckScope())
        {
            base.OnInspectorGUI();

            if(check.changed && planet.autoUpdate)
            {
                planet.GeneratePlanet();
            }
        }

        if (GUILayout.Button("Generate Planet"))
        {
            planet.GeneratePlanet();
        }
        
        if (GUILayout.Button("Show Biomes"))
        {
            Undo.RecordObject(planet.colorSettings, "Show Biomes");
            planet.shapeSettings.SetActiveAllNoises(false);
            if(planet.colorSettings.cleared)
            {
                planet.colorSettings.CacheTintState();
            }
            planet.colorSettings.MaximizeAllTints(true);
            planet.colorSettings.cleared = false;
            planet.GeneratePlanet();
            planet.SetActiveOceanMesh(false);

            EditorUtility.SetDirty(planet.colorSettings);

        }
        if (GUILayout.Button("Hide Biomes") && !planet.colorSettings.cleared)
        {
            Undo.RecordObject(planet.colorSettings, "Show Biomes");
            planet.shapeSettings.SetActiveAllNoises(true);
            planet.colorSettings.RestoreTintState();
            planet.colorSettings.cleared = true;
            planet.GeneratePlanet();
            planet.SetActiveOceanMesh(true);

            EditorUtility.SetDirty(planet.colorSettings);
        }
        if (GUILayout.Button("Save Color Texture"))
        {
            planet.SaveColorTexture();
        }
        if (GUILayout.Button("Toggle Planet Mesh"))
        {
            planet.SetActivePlanetMesh(!planet.IsActivePlanetMesh);
        }
        if (GUILayout.Button("Toggle Ocean Mesh"))
        {
            planet.SetActiveOceanMesh(!planet.IsActiveOceanMesh);
        }
        DrawSettingsEditor(planet.atmosphereSettings, null, ref planet.atmosphereSettingsFoldout, ref atmosphereEditor);
        DrawSettingsEditor(planet.shapeSettings, planet.OnShapeSettingsUpdate, ref planet.shapeSettingsFoldout, ref shapeEditor);
        DrawSettingsEditor(planet.colorSettings, planet.OnColourSettingsUpdated,ref planet.colourSettingsFoldout, ref colourEditor);
    }

    void DrawSettingsEditor(Object settings, System.Action onSettingsUpdated,ref bool foldout,ref Editor editor)
    {
        if (settings == null) return;
        foldout=EditorGUILayout.InspectorTitlebar(foldout, settings);
        using var check = new EditorGUI.ChangeCheckScope();
        if (foldout)
        {
            CreateCachedEditor(settings, null, ref editor);
            editor.OnInspectorGUI();
            if (check.changed)
            {
                onSettingsUpdated?.Invoke();
            }
        }
    }

    void OnEnable()
    {
        planet = (Planet)target;
    }
}
