using UnityEngine;
using UnityEditor;

// This targets the generic wrapper class
[CustomPropertyDrawer(typeof(ReversedList<>))]
public class ReversedListDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // 1. Fetch the actual list inside the wrapper
        SerializedProperty listProperty = property.FindPropertyRelative("list");

        // Basic height for the header/foldout
        float height = EditorGUIUtility.singleLineHeight;

        // If the Wrapper is expanded, calculate the height of the list internals
        if (property.isExpanded)
        {
            height += EditorGUIUtility.standardVerticalSpacing;

            // Height for "Add" Button
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Height for "Size" field
            height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // Calculate height of all children
            if (listProperty != null && listProperty.isArray)
            {
                for (int i = 0; i < listProperty.arraySize; i++)
                {
                    SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                    height += EditorGUI.GetPropertyHeight(element, true) + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            // Bottom padding
            height += EditorGUIUtility.standardVerticalSpacing;
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // 1. Fetch the actual list inside the wrapper
        SerializedProperty listProperty = property.FindPropertyRelative("list");

        // 2. Draw the Foldout (using the label of the variable name)
        Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

        // 3. If expanded, draw the list contents manually
        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            Rect currentRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

            // A. Draw "Add New" Button at Top
            if (GUI.Button(currentRect, "Add New Entry (Top)"))
            {
                listProperty.arraySize++;
                SerializedProperty newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
                ResetValue(newElement);
            }
            currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // B. Draw Size Field
            EditorGUI.PropertyField(currentRect, listProperty.FindPropertyRelative("Array.size"));
            currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // C. Draw Elements in REVERSE Loop
            if (listProperty.isArray)
            {
                for (int i = listProperty.arraySize - 1; i >= 0; i--)
                {
                    SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                    float elementHeight = EditorGUI.GetPropertyHeight(element, true);

                    currentRect.height = elementHeight;

                    // Manual Label to ensure "Element 0" stays "Element 0" visually even if drawn last
                    GUIContent elementLabel = new GUIContent($"Element {i}");

                    EditorGUI.PropertyField(currentRect, element, elementLabel, true);

                    currentRect.y += elementHeight + EditorGUIUtility.standardVerticalSpacing;
                }
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void ResetValue(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer: property.intValue = 0; break;
            case SerializedPropertyType.Boolean: property.boolValue = false; break;
            case SerializedPropertyType.Float: property.floatValue = 0f; break;
            case SerializedPropertyType.String: property.stringValue = ""; break;
            case SerializedPropertyType.Color: property.colorValue = Color.white; break;
            case SerializedPropertyType.ObjectReference: property.objectReferenceValue = null; break;
            case SerializedPropertyType.Vector3: property.vector3Value = Vector3.zero; break;
        }
    }
}