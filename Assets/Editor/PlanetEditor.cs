using System.Collections;
using System.Collections.Generic;
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
            if(check.changed)
            {
                planet.GeneratePlanet();
            }
        }

        if (GUILayout.Button("Generate Planet"))
            planet.GeneratePlanet();
        if (GUILayout.Button("Generate Ocean"))
            planet.GenerateOcean();
        if (GUILayout.Button("Show Biomes"))
        {
            planet.shapeSettings.SetActiveAllNoises(false);
            planet.colorSettings.MaximizeAllTints(true);
            planet.GeneratePlanet();

        }
        if (GUILayout.Button("Hide Biomes"))
        {
            planet.shapeSettings.SetActiveAllNoises(true);
            planet.colorSettings.MaximizeAllTints(false);
            planet.GeneratePlanet();

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
