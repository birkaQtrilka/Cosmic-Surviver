using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReversedList<>))]
public class ReversedListDrawer : PropertyDrawer
{
    const float DragHandleWidth = 18f;

    static readonly Dictionary<string, int> dragIndexByProperty = new();
    static readonly Dictionary<string, Rect[]> elementRectsByProperty = new();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty listProperty = property.FindPropertyRelative("list");

        float height = EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded) return height;

        height += EditorGUIUtility.standardVerticalSpacing;
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Add Button
        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // Size Field

        if (listProperty != null && listProperty.isArray)
        {
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
                height += EditorGUI.GetPropertyHeight(element, true) + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        height += EditorGUIUtility.standardVerticalSpacing;
        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        DrawArray(position, property, label);
        EditorGUI.EndProperty();
    }

    void DrawArray(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty listProperty = property.FindPropertyRelative("list");

        // Foldout
        Rect headerRect = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

        if (!property.isExpanded) return;

        EditorGUI.indentLevel++;
        float horizontalSpacing = EditorGUIUtility.standardVerticalSpacing * 2;
        Rect currentRect = new(
            position.x,
            position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
            position.width,
            EditorGUIUtility.singleLineHeight
        );

        Rect buttonRect = new Rect(
            currentRect.x,
            currentRect.y,
            currentRect.width / 2 - horizontalSpacing,
            currentRect.height
        );

        if (GUI.Button(buttonRect, "Add"))
        {
            OnAddPress(listProperty);
        }

        buttonRect.x += buttonRect.width + horizontalSpacing * 2;

        if (GUI.Button(buttonRect, "Remove") && listProperty.arraySize > 0)
        {
            OnRemovePress(listProperty, property);
        }

        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // Size Field
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.PropertyField(currentRect, listProperty.FindPropertyRelative("Array.size"));
        EditorGUI.EndDisabledGroup();
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 4;

        if (!listProperty.isArray)
        {
            EditorGUI.indentLevel--;
            return;
        }

        Rect[] elementRects = new Rect[listProperty.arraySize];
        elementRectsByProperty[property.propertyPath] = elementRects;

        // Draw Elements in REVERSE Loop
        for (int i = listProperty.arraySize - 1; i >= 0; i--)
        {
            DrawArrayElement(listProperty, property, ref currentRect, elementRects, i);
        }

        DragAndDrop(property, listProperty);
        EditorGUI.indentLevel--;
    }

    void DrawArrayElement(SerializedProperty listProperty, SerializedProperty property, ref Rect currentRect, Rect[] rectsCache, int i)
    {
        SerializedProperty element = listProperty.GetArrayElementAtIndex(i);
        float elementHeight = EditorGUI.GetPropertyHeight(element, true);

        currentRect.height = elementHeight;
        rectsCache[i] = currentRect;

        Rect handleRect = new(
            currentRect.x,
            currentRect.y,
            DragHandleWidth,
            currentRect.height
        );

        Rect contentRect = new(
            currentRect.x + DragHandleWidth + 4,
            currentRect.y,
            currentRect.width - DragHandleWidth - 4,
            currentRect.height
        );

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (handleRect.Contains(e.mousePosition) && !EditorGUIUtility.editingTextField)
            {
                SetDragIndex(property, i);
                GUI.FocusControl(null);
                e.Use();
            }
        }

        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        EditorGUI.LabelField(handleRect, "=", EditorStyles.centeredGreyMiniLabel);
        EditorGUI.indentLevel = oldIndent;

        GUIContent elementLabel = new($"Element {i}");

        GUI.SetNextControlName(GetItemControlName(property, i));

        EditorGUI.PropertyField(contentRect, element, elementLabel, true);

        EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.Pan);
        currentRect.y += elementHeight + EditorGUIUtility.standardVerticalSpacing;
    }

    void OnRemovePress(SerializedProperty listProperty, SerializedProperty property)
    {
        int indexToRemove = -1;
        string focusedControl = GUI.GetNameOfFocusedControl();

        // Check if the focused control matches one of our elements
        for (int i = 0; i < listProperty.arraySize; i++)
        {
            if (focusedControl == GetItemControlName(property, i))
            {
                indexToRemove = i;
                break;
            }
        }

        // Fallback: If nothing is focused, remove the LAST element (which is visually at the top in a reversed list)
        if (indexToRemove == -1)
        {
            indexToRemove = listProperty.arraySize - 1;
        }

        // Safety check
        if (indexToRemove < 0 || indexToRemove >= listProperty.arraySize) return;

        SerializedProperty element = listProperty.GetArrayElementAtIndex(indexToRemove);

        // Unity Object Reference Quirk Check
        if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
        {
            listProperty.DeleteArrayElementAtIndex(indexToRemove);
        }

        listProperty.DeleteArrayElementAtIndex(indexToRemove);
    }

    // Helper to generate unique names for fields
    string GetItemControlName(SerializedProperty property, int index)
    {
        return $"{property.propertyPath}_Item_{index}";
    }

    void DragAndDrop(SerializedProperty property, SerializedProperty listProperty)
    {
        Event e = Event.current;
        int dragIndex = GetDragIndex(property);

        if (dragIndex == -1) return;

        if (e.type == EventType.MouseDrag) { e.Use(); }

        if (e.type != EventType.MouseUp) return;
        Rect[] rects = elementRectsByProperty[property.propertyPath];

        for (int target = 0; target < rects.Length; target++)
        {
            if (!rects[target].Contains(e.mousePosition)) continue;
            if (target != dragIndex)
            {
                listProperty.MoveArrayElement(dragIndex, target);
                GUI.FocusControl(null); // Clear focus after moving to prevent accidental deletion of wrong index
            }
            break;
        }

        SetDragIndex(property, -1);
        e.Use();
    }

    void OnAddPress(SerializedProperty listProperty)
    {
        listProperty.arraySize++;
        SerializedProperty newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
        ResetValue(newElement);
    }

    void DrawDragVisual(Rect currentRect, SerializedProperty property, int i)
    {
        if (GetDragIndex(property) != i) return;
        EditorGUI.DrawRect(
            currentRect,
            new Color(1f, 1f, 1f, 0.08f)
        );
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

    int GetDragIndex(SerializedProperty property)
    {
        return dragIndexByProperty.TryGetValue(property.propertyPath, out int i) ? i : -1;
    }

    void SetDragIndex(SerializedProperty property, int index)
    {
        dragIndexByProperty[property.propertyPath] = index;
    }
}