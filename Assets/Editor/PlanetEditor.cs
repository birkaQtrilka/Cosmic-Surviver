using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(Planet))]
public class PlanetEditor : Editor
{
    Planet planet;
    Editor shapeEditor;
    Editor colourEditor;

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
            planet.GenerateOcean();
            if (planet.autoSaveTexture)
            {
                planet.SaveColorTexture();
            }
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
            planet.DisableOceanMeshes();

            EditorUtility.SetDirty(planet.colorSettings);

        }
        if (GUILayout.Button("Hide Biomes") && !planet.colorSettings.cleared)
        {
            Undo.RecordObject(planet.colorSettings, "Show Biomes");
            planet.shapeSettings.SetActiveAllNoises(true);
            planet.colorSettings.RestoreTintState();
            planet.colorSettings.cleared = true;
            planet.GeneratePlanet();
            planet.EnableOceanMeshes();

            EditorUtility.SetDirty(planet.colorSettings);
        }
        if (GUILayout.Button("Save Color Texture"))
        {
            planet.SaveColorTexture();
        }


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
