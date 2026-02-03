using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(MaterialSwitch))]
public class MaterialSwitchEditor : Editor
{
    private ReorderableList list;
    private SerializedProperty switchesProp;

    private void OnEnable()
    {
        switchesProp = serializedObject.FindProperty("switches");

        list = new ReorderableList(
            serializedObject,
            switchesProp,
            draggable: true,
            displayHeader: true,
            displayAddButton: true,
            displayRemoveButton: true
        );

        list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Material Switches");
        };

        list.elementHeight = EditorGUIUtility.singleLineHeight * 5 + 12;
        list.onAddCallback = l =>
        {
            if(switchesProp == null) switchesProp = serializedObject.FindProperty("switches");
            serializedObject.Update();

            switchesProp.arraySize++;

            SerializedProperty element =
                switchesProp.GetArrayElementAtIndex(switchesProp.arraySize - 1);

            element.FindPropertyRelative("planetMat").objectReferenceValue = null;
            element.FindPropertyRelative("oceanMat").objectReferenceValue = null;

            serializedObject.ApplyModifiedProperties();
        };

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            SerializedProperty element = switchesProp.GetArrayElementAtIndex(index);
            SerializedProperty planetMat = element.FindPropertyRelative("planetMat");
            SerializedProperty oceanMat = element.FindPropertyRelative("oceanMat");
            SerializedProperty name = element.FindPropertyRelative("name");
            SerializedProperty planetShow = element.FindPropertyRelative("activeOceanMesh");
            SerializedProperty oceanShow = element.FindPropertyRelative("activePlanetMesh");

            Rect nameRect = new Rect(
                rect.x,
                rect.y,
                rect.width - 70,
                EditorGUIUtility.singleLineHeight
            );
            rect.y += EditorGUIUtility.singleLineHeight;
            Rect planetRect = new Rect(
                rect.x,
                rect.y,
                rect.width - 70,
                EditorGUIUtility.singleLineHeight
            );

            Rect oceanRect = new Rect(
                rect.x,
                rect.y + EditorGUIUtility.singleLineHeight + 2,
                rect.width - 70,
                EditorGUIUtility.singleLineHeight
            );
            Rect oceanShowRect = new Rect(
                rect.x,
                rect.y + EditorGUIUtility.singleLineHeight * 2,
                rect.width - 70,
                EditorGUIUtility.singleLineHeight
            );
            Rect planetShowRect = new Rect(
                rect.x,
                rect.y + EditorGUIUtility.singleLineHeight * 3,
                rect.width - 70,
                EditorGUIUtility.singleLineHeight
            );
            Rect buttonRect = new Rect(
                rect.x + rect.width - 60,
                rect.y,
                60,
                EditorGUIUtility.singleLineHeight * 2 + 2
            );
            EditorGUI.PropertyField(nameRect, name, new GUIContent(string.IsNullOrEmpty(name.stringValue) ? "Name" : name.stringValue));
            EditorGUI.PropertyField(planetRect, planetMat, new GUIContent("Planet"));
            EditorGUI.PropertyField(oceanRect, oceanMat, new GUIContent("Ocean"));
            EditorGUI.PropertyField(oceanShowRect, oceanShow, new GUIContent("ShowOcean"));
            EditorGUI.PropertyField(planetShowRect, planetShow, new GUIContent("ShowPlanet"));

            if (GUI.Button(buttonRect, "Apply"))
            {
                Apply(index);
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        list.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private void Apply(int index)
    {
        MaterialSwitch materialSwitch = (MaterialSwitch)target;
        SwitchData data = materialSwitch.switches[index];

        Undo.RecordObject(materialSwitch.Planet, "Apply Material Switch");
        materialSwitch.ApplySwitch(data);

        EditorUtility.SetDirty(materialSwitch.Planet);
    }
}
