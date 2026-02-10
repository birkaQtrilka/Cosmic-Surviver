using System;
using System.Collections.Generic;
using System.Linq;
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
        List<int> r = new List<int>();
        for (int i = 0; i < Count; i++)
        {
            r.Add(triangleIndeces[i]);
        }
        return r;
        //return triangleIndeces.Take(Count).ToList();
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
    // store edge cell triangles
    // do not remove vertices
    // you already have indeces of existing vertices
    // just repplace them with the vertices of the other face when welding
    (List<int>, List<Vector2>) GenerateTrianglesAndAddAdditionalVertices(List<Vector3> vertices)
    {
        CellPoint[][] cellLookup = navigator.InitLookUpTable();
        OceanVertData[] oceanVerts = terrainFace.BellowZeroVertices;
        List<int> triangles = new(terrainFace.Mesh.triangles.Length);
        List<Vector2> uvs = new(powResolution);
        //adds to current grid position (x, y) to get desirable corner of cell
        Queue<Vector2> uvQueue = new();

        for (int i = 0; i < powResolution; i++)
        {
            int y = i / _resolution;
            int x = i - y * _resolution;
            if (oceanVerts[i].isOcean)
            {
                Vector3 dir = terrainFace.GetUnitSpherePointFromXY(x, y);
                Vector2 uv = GetUV(dir, terrainFace.LocalUp);
                uvs.Add(uv);
            }
            //else
            //{
            //    //uvs.Add(Vector2.zero);//placeholder,
            //}

            if (!oceanVerts[i].isOcean && !oceanVerts[i].isShore) continue;//check only the shore or ocean verts for optimization

            if (x == _resolution - 1 || y == _resolution - 1) continue;// the edges don't form cells, so skip
            Vector2 gridPos = new(x, y);

            //to later retrieve the points of the line that the additional vertex will sit on
            _corners[0] = oceanVerts[i];
            _corners[1] = oceanVerts[i + navigator.right];
            _corners[2] = oceanVerts[i + navigator.downRight];
            _corners[3] = oceanVerts[i + navigator.down];

            //checking the corners of cell to see what type of contour I need
            int a = BoolToInt(_corners[0].isOcean);
            int b = BoolToInt(_corners[1].isOcean);
            int c = BoolToInt(_corners[2].isOcean);
            int d = BoolToInt(_corners[3].isOcean);

            int contourHash = GetContour(a, b, c, d);

            TriangleCell currentEdgeCell = GetCurrentTriangleCell(x, y);

            CellPoint[] cell = cellLookup[contourHash];
            foreach (CellPoint cellVert in cell)
            {
                if (!cellVert.IsAdditional)
                {
                    //goes through the cells 
                    int vertIndex = oceanVerts[i + cellVert.IndexOffset].VerticesArrayIndex;
                    triangles.Add(vertIndex);
                    currentEdgeCell?.Add(triangles.Count - 1);
                    continue;
                }

                //the point that sits in between two vertices of the current cell
                Vector2 edgePoint = GetLerpedEdgePoint(cellVert, gridPos, _corners);
                Vector3 pointOnUnitSphere = terrainFace.GetUnitSpherePointFromXY(edgePoint.x, edgePoint.y);

                Vector2 uv = GetUV(pointOnUnitSphere, terrainFace.LocalUp);
                Vector3 vertex = pointOnUnitSphere * shapeGenerator.PlanetRadius;
                
                uvQueue.Enqueue(uv);
                vertices.Add(vertex);
                triangles.Add(vertices.Count-1);
                currentEdgeCell?.Add(triangles.Count - 1);

            }

        }
        //find out if uv line goes 
        while (uvQueue.Count > 0)
        {
            uvs.Add(uvQueue.Dequeue());
        }

        return (triangles, uvs);
    }
    


    // the negative is so the cells grow consistently clockwise in the edge arrays
    bool TryGetEdgeCell(int x, int y, out Vector2Int edgeCellIndeces, out Vector2Int otherEdgeCellIndeces)
    {
        // the negative means that cell index goes in reverse order
        int lastCellIndex = _resolution - 2;
        if (y == 0) // top edge
        {
            edgeCellIndeces = new Vector2Int(0, x);
            if (x == 0) // left
            {
                otherEdgeCellIndeces = new Vector2Int(3, lastCellIndex);

            }
            else if (x == lastCellIndex) // right
            { 
                otherEdgeCellIndeces = new Vector2Int(1, 0);
            }
            else
            {
                otherEdgeCellIndeces = new Vector2Int(-1, -1);
            }
            return true;
        }
        if (x == lastCellIndex) // right edge
        {
            edgeCellIndeces = new Vector2Int(1, y);
            if (y == lastCellIndex)
            {
                otherEdgeCellIndeces = new Vector2Int(2, -lastCellIndex);
            }
            else
            {
                otherEdgeCellIndeces = new Vector2Int(-1, -1);
            }
            return true;
        }
        if(y == lastCellIndex) // bottom edge
        {
            edgeCellIndeces = new Vector2Int(2, -x);
            
            if (x == 0)
            {
                otherEdgeCellIndeces = new Vector2Int(3, -lastCellIndex-1);
            }
            else
            {
                otherEdgeCellIndeces = new Vector2Int(-1, -1);
            }
            return true;
        }
        if (x == 0) // left edge
        {
            edgeCellIndeces = new Vector2Int(3, -y-1);
            otherEdgeCellIndeces = new Vector2Int(-1, -1);
            return true;
        }
        edgeCellIndeces = new Vector2Int(-1, -1);
        otherEdgeCellIndeces = new Vector2Int(-1, -1);

        return false;
    }

    TriangleCell GetCurrentTriangleCell(int x, int y)
    {
        bool isEdgeCell = TryGetEdgeCell(x, y, out Vector2Int edgeCellIndeces, out Vector2Int otherEdgeCellIndeces);
        if (!isEdgeCell) return null;

        if (edgeCellIndeces.y < 0) { edgeCellIndeces.y = _resolution + edgeCellIndeces.y - 1; }

        TriangleCell currentEdgeCell = _edgeCellTriangles[edgeCellIndeces.x, edgeCellIndeces.y];
        if (currentEdgeCell == null)
        {
            currentEdgeCell = new TriangleCell(15);
            _edgeCellTriangles[edgeCellIndeces.x, edgeCellIndeces.y] = currentEdgeCell;
        }

        if (otherEdgeCellIndeces.x == -1) return currentEdgeCell;
        if (otherEdgeCellIndeces.y < 0) { otherEdgeCellIndeces.y = _resolution + otherEdgeCellIndeces.y - 1; }

        var other = _edgeCellTriangles[otherEdgeCellIndeces.x, otherEdgeCellIndeces.y];
        if (other != null) return currentEdgeCell;
        
        _edgeCellTriangles[otherEdgeCellIndeces.x, otherEdgeCellIndeces.y] = currentEdgeCell;
        return currentEdgeCell;
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