using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ReversedList<T>
{
    // We hide the internal list from default Inspector drawing 
    // because our CustomDrawer will handle it.
    [SerializeField]
    public List<T> list = new List<T>();

    // Helper to make using the wrapper feel like using a List
    public void Add(T item) => list.Add(item);
    public T this[int index] { get => list[index]; set => list[index] = value; }
    public int Count => list.Count;
}