using System;
using System.Collections.Generic;
/// <summary>
/// contains Indeces to triangle array. THEY ARE NOT VERTEX INDECES
/// USED FOR CHANGING DATA IN TRIANGLE ARRAY NOT VERTICES ARRAY
/// </summary>
public class TriangleCell
{
    public int Count { get; private set; }
    readonly int[] triangleIndeces;
    readonly int capacity;

    public TriangleCell(int pCapacity)
    {
        triangleIndeces = new int[pCapacity];
        capacity = pCapacity;
        Count = 0;
    }

    public void Add(int vert)
    {
        if(Count >= capacity)
        {
            throw new InvalidOperationException("TriangleCell capacity exceeded. Cannot add more vertices.");
        }
        triangleIndeces[Count++] = vert;
    }

    public int this[int index] { get => triangleIndeces[index]; set => triangleIndeces[index] = value; }

    public List<int> GetListCopy() {
        List<int> r = new(Count);
        for (int i = 0; i < Count; i++) r.Add(triangleIndeces[i]);
        return r;
    }
}
