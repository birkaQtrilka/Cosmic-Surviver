using System;
using System.Collections.Generic;
using UnityEngine;
// contains Indeces to triangle array. THEY ARE NOT VERTEX INDECES
// USED FOR CHANGING DATA IN TRIANGLE ARRAY NOT VERTICES ARRAY
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

[Serializable]
public class OceanFace
{
    Mesh _mesh;
    int powResolution;
    int _resolution;
    TerrainFace terrainFace;
    ShapeGenerator shapeGenerator;

    //represents all corners of the current checked cell
    readonly OceanVertData[] _corners = new OceanVertData[4];
    readonly List<(CornerIndexes vert, int vertIndex)> _addedAditionalVerts = new(5);
    TriangleCell[] _previousRow;
    TriangleCell[] _currentRow;

    public GridNavigator navigator;
    //top, right, bottom, left
    TriangleCell[,] _edgeCellTriangles;
    public TriangleCell[,] EdgeCellTriangles => _edgeCellTriangles;
    public List<Vector3> Vertices { get; private set; }
    public List<int> Triangles { get; private set; }
    

    public void Initialize(Mesh mesh, TerrainFace face, int resolution)
    {
        _mesh = mesh;
        terrainFace = face;
        powResolution = resolution * resolution;
        _resolution = resolution;
        shapeGenerator = terrainFace.ShapeGenerator;
        navigator = new GridNavigator(resolution);
        _edgeCellTriangles = new TriangleCell[4, resolution-1];
        _currentRow =  new TriangleCell[resolution - 1];
        _previousRow = new TriangleCell[resolution - 1];

    }

    public Mesh ConstructMesh()
    {
        Vertices = GenerateVertices();

        (List<int> triangles, List<Vector2> uvs) = GenerateTrianglesAndAddAdditionalVertices(Vertices);
        Triangles = triangles;

        _mesh.Clear();
        _mesh.vertices = Vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.uv = uvs.ToArray();
        _mesh.RecalculateNormals();
        return _mesh;
    }

    public Mesh GetMesh()
    {
        return _mesh;
    }

    void ResetRow(ref TriangleCell[] row)
    {
        row = new TriangleCell[row.Length];
    }

    // look if vert is at the edge, then connect to other face
    // for me to connect, I first need to remove triange connections from one of the faces
    List<Vector3> GenerateVertices()
    {
        OceanVertData[] oceanVerts = terrainFace.BellowZeroVertices;
        List<Vector3> vertices = new(powResolution);

        //can be parallel
        //marks the surrounding vertices of bellow zero vertices as "shore", so they can be later used to create the square marching edges
        for (int i = 0; i < oceanVerts.Length; i++)
        {
            int y = i / _resolution;
            int x = i - y * _resolution;
            if (oceanVerts[i].isOcean)
            {
                vertices.Add(oceanVerts[i].WorldPos);
                oceanVerts[i].VerticesArrayIndex = vertices.Count - 1;
                //how far it is from ocean level
                continue;
            }

            // looks if any of the verts surrounding this vert is a bellow zero one (isOcean)
            foreach (int offset in navigator.neighborOffsets)
            {
                int neighborIndex = i + offset;
                int neighborY = neighborIndex / _resolution;
                int neighborX = neighborIndex - neighborY * _resolution;
                // if the difference is bigger than one, they are on diffrent columns / rows 
                if (
                    neighborIndex >= 0 
                    && neighborIndex < oceanVerts.Length // within bounds
                    && Math.Abs(neighborX - x) <= 1 
                    && Math.Abs(neighborY - y) <= 1 
                    && oceanVerts[neighborIndex].isOcean)
                {
                    oceanVerts[i].isShore = true;// don't add them in the vertices list yet because we will create new vertices 
                    //that will be positioned according to the square marching algorithm
                    break; // No need to check further if already marked as shore
                }
            }

        }

        return vertices;
    }

    (List<int>, List<Vector2>) GenerateTrianglesAndAddAdditionalVertices(List<Vector3> vertices)
    {
        CellPoint[][] cellLookup = navigator.InitLookUpTable();
        OceanVertData[] oceanVerts = terrainFace.BellowZeroVertices;
        List<int> triangles = new(terrainFace.Mesh.triangles.Length);
        List<Vector2> uvs = new(powResolution);
        Queue<Vector2> uvQueue = new();

        // Calculate the index of the last cell row/col
        int lastCellIndex = _resolution - 2;
        int previousY = 0;
        for (int i = 0; i < powResolution; i++)
        {
            int y = i / _resolution;
            int x = i - y * _resolution;
            bool shouldChangeRows = previousY != y;
            if (shouldChangeRows)
            {
                _previousRow = _currentRow;
                _currentRow = new TriangleCell[_resolution - 1];
                previousY = y;
            }


            if (oceanVerts[i].isOcean)
            {
                Vector3 dir = terrainFace.GetUnitSpherePointFromXY(x, y);
                Vector2 uv = GetUV(dir, terrainFace.LocalUp);
                uvs.Add(uv);
            }
            if (!oceanVerts[i].isOcean && !oceanVerts[i].isShore) continue;

            // Skip the last vertex row/col as they don't start new cells
            if (x == _resolution - 1 || y == _resolution - 1) continue;

            Vector2 gridPos = new(x, y);

            _corners[0] = oceanVerts[i];
            _corners[1] = oceanVerts[i + navigator.right];
            _corners[2] = oceanVerts[i + navigator.downRight];
            _corners[3] = oceanVerts[i + navigator.down];

            int a = BoolToInt(_corners[0].isOcean);
            int b = BoolToInt(_corners[1].isOcean);
            int c = BoolToInt(_corners[2].isOcean);
            int d = BoolToInt(_corners[3].isOcean);

            int contourHash = GetContour(a, b, c, d);
            _addedAditionalVerts.Clear();
            CellPoint[] cell = cellLookup[contourHash];
            foreach (CellPoint cellVert in cell)
            {
                if (!cellVert.IsAdditional)
                {
                    int vertIndex = oceanVerts[i + cellVert.IndexOffset].VerticesArrayIndex;
                    triangles.Add(vertIndex);

                    AddTriangleToEdgeCells(x, y, lastCellIndex, triangles.Count - 1);
                    continue;
                }
                int sameVertIndex = _addedAditionalVerts.FindIndex(x => x.vert.Equals(cellVert.AdditionalPos));
                int otherVertIndex;
                if (sameVertIndex != -1)
                {
                    int previousVertIndex = _addedAditionalVerts[sameVertIndex].vertIndex;

                    otherVertIndex = ShouldWeldVertex(x, y, vertices[previousVertIndex], vertices);
                    previousVertIndex = otherVertIndex ==-1 ? previousVertIndex : otherVertIndex;
                    triangles.Add(previousVertIndex);

                    AddTriangleToEdgeCells(x, y, lastCellIndex, triangles.Count - 1);
                    AddTriangleToCurrentCell(x, previousVertIndex);
                    continue;
                }


                Vector2 edgePoint = GetLerpedEdgePoint(cellVert, gridPos, _corners);
                Vector3 pointOnUnitSphere = terrainFace.GetUnitSpherePointFromXY(edgePoint.x, edgePoint.y);

                Vector2 uv = GetUV(pointOnUnitSphere, terrainFace.LocalUp);
                Vector3 vertex = pointOnUnitSphere * shapeGenerator.PlanetRadius;

                otherVertIndex = ShouldWeldVertex(x, y, vertex, vertices);
                if (otherVertIndex != -1)
                {
                    triangles.Add(otherVertIndex);
                    _addedAditionalVerts.Add((cellVert.AdditionalPos, otherVertIndex));
                    AddTriangleToCurrentCell(x, otherVertIndex);
                    AddTriangleToEdgeCells(x, y, lastCellIndex, triangles.Count - 1);
                    continue;
                }


                uvQueue.Enqueue(uv);
                vertices.Add(vertex);
                int addedVertIndex = vertices.Count - 1;


                triangles.Add(addedVertIndex);
                _addedAditionalVerts.Add((cellVert.AdditionalPos, addedVertIndex));
                AddTriangleToCurrentCell(x, addedVertIndex);
                AddTriangleToEdgeCells(x, y, lastCellIndex, triangles.Count - 1);
            }

        }

        while (uvQueue.Count > 0)
        {
            uvs.Add(uvQueue.Dequeue());
        }

        return (triangles, uvs);
    }


    int ShouldWeldVertex(int x, int y, Vector3 this_Vert, List<Vector3> vertices)
    {
        if (y != 0)
        {
            int r = FindToWeld(_previousRow[x], this_Vert, vertices);
            if (r != -1) return r;
        }

        if (x == 0) return -1;
        return FindToWeld(_currentRow[x - 1], this_Vert, vertices);
    }

    int FindToWeld(TriangleCell otherCell, Vector3 thisVert, List<Vector3> vertices)
    {
        if (otherCell == null) return -1;
        for (int k = 0; k < otherCell.Count; k++)
        {
            int other_TriangleIndex = otherCell[k];

            Vector3 other_Vert = vertices[other_TriangleIndex];

            if ((thisVert - other_Vert).sqrMagnitude <= 0.001f) return other_TriangleIndex;
        }
        return -1;
    }

    void AddTriangleToCurrentCell(int x, int triangleIndex)
    {
        _currentRow[x] ??= new TriangleCell(15);
        _currentRow[x].Add(triangleIndex);
    }

    void AddTriangleToEdgeCells(int x, int y, int lastCellIndex, int triangleIndex)
    {
        // Side 0: Top Edge
        if (y == 0)
        {
            AddToEdgeCell(0, x, triangleIndex);
        }

        // Side 1: Right Edge
        if (x == lastCellIndex)
        {
            AddToEdgeCell(1, y, triangleIndex);
        }

        // Side 2: Bottom Edge
        if (y == lastCellIndex)
        {
            AddToEdgeCell(2, _resolution - x - 2, triangleIndex);
        }

        // Side 3: Left Edge
        if (x == 0)
        {
            AddToEdgeCell(3, _resolution - y - 2, triangleIndex);
        }
    }

    // Internal helper to safely get/create the cell
    void AddToEdgeCell(int side, int index, int val)
    {
        TriangleCell cell = _edgeCellTriangles[side, index];
        if (cell == null)
        {
            cell = new TriangleCell(15);
            _edgeCellTriangles[side, index] = cell;
        }
        cell.Add(val);
    }

    // get position of vert, based on that set a uv blend
    Vector2 GetUV(Vector3 dir, Vector3 face)
    {
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);
        float absZ = Mathf.Abs(dir.z);

        bool isXPositive = dir.x > 0;
        bool isYPositive = dir.y > 0;
        bool isZPositive = dir.z > 0;

        float u, v;

        if (face.x != 0)
        {
            // Major axis is X
            if (isXPositive)
            {
                // +X face
                u = -dir.z / absX;
                v = -dir.y / absX;
            }
            else
            {
                // -X face
                u = dir.z / absX;
                v = -dir.y / absX;
            }
        }
        else if (face.y != 0)
        {
            // Major axis is Y
            if (isYPositive)
            {
                // +Y face
                u = dir.x / absY;
                v = dir.z / absY;
            }
            else
            {
                // -Y face
                u = dir.x / absY;
                v = -dir.z / absY;
            }
        }
        else
        {
            // Major axis is Z
            if (isZPositive)
            {
                // +Z face
                u = dir.x / absZ;
                v = -dir.y / absZ;
            }
            else
        {
                // -Z face
                u = -dir.x / absZ;
                v = -dir.y / absZ;
            }
        }

        return new Vector2(u, v) * 0.5f + Vector2.one * 0.5f;
    }

    public static Vector2 GetLerpedEdgePoint(CellPoint cellPoint, Vector2 gridPos, OceanVertData[] corners)
    {
        int corner1Index = cellPoint.AdditionalPos.Corner1Offset;
        int corner2Index = cellPoint.AdditionalPos.Corner2Offset;
        //get oceanVertData of corner to use the "DistanceToZero" value
        OceanVertData Corner1VertData = corners[corner1Index];
        OceanVertData Corner2VertData = corners[corner2Index];
        //lerp between these positions to get the edge point
        Vector2 corner1Pos = GridNavigator.PosOffsets[corner1Index] + gridPos;
        Vector2 corner2Pos = GridNavigator.PosOffsets[corner2Index] + gridPos;
        //ocean level is interpreted as 0, but for the interpolation formula to work, I need to interpret it as 1, so I just add one to a and b
        float a = Corner1VertData.DistanceToOceanLevel + 1;
        float b = Corner2VertData.DistanceToOceanLevel + 1;
        return LerpCloseToOne(a , b, corner1Pos, corner2Pos);

    }

    public static Vector2 LerpCloseToOne(float valueA, float valueB, Vector2 a, Vector2 b)
    {
        return Vector2.Lerp(a, b, (1 - valueA) / (valueB - valueA));
    }

    // abcd are the values of a binary number xxxx, they represent the corners of the cell
    //     a   b   <--- one cell
    //     d   c
    // the result is mapped to an entry in the lookup table that returns which countour
    // corresponds to the corners formation
    public static int GetContour(int a, int b, int c, int d)
    {
        return a * 8 + b * 4 + c * 2 + d * 1;
    }

    public static int BoolToInt(bool boolean)
    {
        return boolean ? 1 : 0;
    }
}