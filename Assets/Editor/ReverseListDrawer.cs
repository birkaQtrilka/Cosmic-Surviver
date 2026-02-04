using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReversedList<>))]
public class ReversedListDrawer : PropertyDrawer
{
    const float DragHandleWidth = 18f;
    static readonly Dictionary<string, int> selectedIndexByProperty = new ();
    static readonly Dictionary<string, int> dragIndexByProperty = new();
    static readonly Dictionary<string, Rect[]> elementRectsByProperty = new();

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty listProperty = property.FindPropertyRelative("list");

        float height = EditorGUIUtility.singleLineHeight;

        if (!property.isExpanded) return height;

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
        Rect headerRect = new (position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

        if (!property.isExpanded) return;

        EditorGUI.indentLevel++;
        float horizontalSpacing = EditorGUIUtility.standardVerticalSpacing * 2;
        Rect currentRect = new (
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
        
        buttonRect.x += buttonRect.width + horizontalSpacing*2;

        if (GUI.Button(buttonRect, "Remove") && listProperty.arraySize > 0)
        {
            OnRemovePress(listProperty, property);
        }

        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        // B. Draw Size Field
        EditorGUI.BeginDisabledGroup(true);
        EditorGUI.PropertyField(currentRect, listProperty.FindPropertyRelative("Array.size"));
        EditorGUI.EndDisabledGroup(); 
        currentRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing * 4;

        // Draw Elements in REVERSE Loop
        if (!listProperty.isArray)
        {
            EditorGUI.indentLevel--;
            return;
        }

        Rect[] elementRects = new Rect[listProperty.arraySize];
        elementRectsByProperty[property.propertyPath] = elementRects;

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
            if(handleRect.Contains(e.mousePosition) && !EditorGUIUtility.editingTextField) {
                SetDragIndex(property, i);
                GUI.FocusControl(null);
                e.Use();
            } if (currentRect.Contains(e.mousePosition) && !EditorGUIUtility.editingTextField)
            {
                SetSelectedIndex(property, i);
            }

            
        }


        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        EditorGUI.LabelField(handleRect, "=", EditorStyles.centeredGreyMiniLabel);
        EditorGUI.indentLevel = oldIndent;

        GUIContent elementLabel = new($"Element {i}");
        EditorGUI.PropertyField(contentRect, element, elementLabel, true);

        EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.Pan);
        currentRect.y += elementHeight + EditorGUIUtility.standardVerticalSpacing;
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
                SetSelectedIndex(property, target);
            }
            break;
        }

        SetDragIndex(property, -1);
        e.Use();
    }

    void OnRemovePress(SerializedProperty listProperty, SerializedProperty property)
    {
        int selected = GetSelectedIndex(property);

        int indexToRemove =
            (selected >= 0 && selected < listProperty.arraySize)
            ? selected
            : 0;

        SerializedProperty element = listProperty.GetArrayElementAtIndex(indexToRemove);

        // If it is a non-null Unity Object reference, the first Delete call only sets it to null.
        // We strictly check for this specific case before double-deleting.
        if (element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null)
        {
            listProperty.DeleteArrayElementAtIndex(indexToRemove);
        }

        // Actually delete the element (or the now-null placeholder)
        listProperty.DeleteArrayElementAtIndex(indexToRemove);

        SetSelectedIndex(property, -1);
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

    int GetSelectedIndex(SerializedProperty property)
    {
        return selectedIndexByProperty.TryGetValue(property.propertyPath, out int i)
            ? i
            : -1;
    }

    void SetSelectedIndex(SerializedProperty property, int index)
    {
        selectedIndexByProperty[property.propertyPath] = index;
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