using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class OceanFace
{
    //the algorithm goes cell by cell, this data structure is to represent 2 corners of a cell
    //0-top left corner, 1-top right, 2-bottom left, 3-bottom right
    readonly struct CornerIndexes
    {
        public readonly int Corner1Offset;
        public readonly int Corner2Offset;

        public CornerIndexes(int corner1, int corner2)
        {
            Corner1Offset = corner1;
            Corner2Offset = corner2;
        }
    }

    readonly struct CellPoint
    {
        //additional position means another point between the already existing ocean vertices
        public readonly bool IsAdditional;
        public readonly CornerIndexes AdditionalPos;

        public readonly int IndexOffset;
        //the numbers mean between what points of the cell is the additional position.
        //0-top left corner, 1-top right, 2-bottom left, 3-bottom right
        public readonly static CornerIndexes north = new(0, 1);
        public readonly static CornerIndexes east = new(1, 2);
        public readonly static CornerIndexes south = new(2, 3);
        public readonly static CornerIndexes west = new(3, 0);

        public CellPoint(int offset)
        {
            IsAdditional = false;
            IndexOffset = offset;
            AdditionalPos = new(0, 0);
        }

        public CellPoint(CornerIndexes additionalPos)
        {
            IsAdditional = true;
            IndexOffset = int.MinValue;
            AdditionalPos = additionalPos;
        }
    }

    Mesh _mesh;
    int powResolution;
    int _resolution;
    TerrainFace terrainFace;
    ShapeGenerator shapeGenerator;

    //used to move through the vertices array
    int downRight;
    int down;
    int downLeft;
    int left;
    int upLeft;
    int up;
    int upRight;
    int right;
    int origin;
    //represents all corners of the current checked cell
    readonly Vector2[] _gridPosOffsets = new Vector2[]
    {
        new(0,0),
        new(1,0),
        new(1,1),
        new(0,1)
    };
    readonly OceanVertData[] _corners = new OceanVertData[4];

    public void Initialize(Mesh mesh, TerrainFace face, int resolution)
    {
        _mesh = mesh;
        terrainFace = face;
        powResolution = resolution * resolution;
        _resolution = resolution;
        shapeGenerator = terrainFace.ShapeGenerator;

        downRight = _resolution + 1;
        down = _resolution;
        downLeft = _resolution - 1;
        left = -1;
        upLeft = -_resolution - 1;
        up = -_resolution;
        upRight = -_resolution + 1;
        right = 1;
        origin = 0;
    }

    public void ConstructMesh()
    {
        List<Vector3> vertices = GenerateVertices();

        (List<int> triangles, List<Vector2> uvs) = GenerateTrianglesAndAddAdditionalVertices(vertices);

        _mesh.Clear();
        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.uv = uvs.ToArray();
        //_mesh.normals = GenerateNormals(vertices).ToArray();
        _mesh.RecalculateNormals();
    }

    List<Vector3> GenerateVertices()
    {

        OceanVertData[] oceanVerts = terrainFace.BellowZeroVertices;
        List<Vector3> vertices = new(powResolution);

        int[] neighborOffsets = new int[]//for convenience
        {
            downRight,
            down,
            downLeft,
            left,
            upLeft,
            up,
            upRight,
            right
        };

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
            foreach (int offset in neighborOffsets)
            {
                int neighborIndex = i + offset;
                int neighborY = neighborIndex / _resolution;
                int neighborX = neighborIndex - neighborY * _resolution;
                // if the difference is bigger than one, they are on diffrent columns/rows 
                if (neighborIndex >= 0 && neighborIndex < oceanVerts.Length &&
                    Math.Abs(neighborX - x) <= 1 && Math.Abs(neighborY - y) <= 1 &&
                    oceanVerts[neighborIndex].isOcean)
                {
                    oceanVerts[i].isShore = true;// don't add them in the vertices list yet because we will create new vertices 
                    //that will be positioned according to the square marching algorithm
                    break; // No need to check further if already marked as shore
                }
            }

        }

        return vertices;
    }

    //the 16 ways that a cell might look like mapped to how to order the verts in the triangles array
    //if it's a pole coordinate, that means it's a vert that should be created in between the existing vertices
    CellPoint[][] InitLookUpTable()
    {
        //relative to topleft corner of cell (origin)
        return new CellPoint[][]
        {
            new CellPoint[] { /*no triangles*/},//0
            new CellPoint[] { // 1
                //first triangle
                new(CellPoint.west), new(CellPoint.south), new(down),
            },
            new CellPoint[] { //2
                new(CellPoint.east), new(downRight), new(CellPoint.south),
            },
            new CellPoint[] { //3
                //first triangle
                new(CellPoint.west), new(downRight), new(down),
                //second triangle
                new(CellPoint.west), new(CellPoint.east), new(downRight),
            },
            new CellPoint[] { //4
                new(CellPoint.north), new(right), new(CellPoint.east),
            },
            new CellPoint[] { //5
                new(CellPoint.west), new(CellPoint.south), new(down),
                new(CellPoint.west), new(CellPoint.north), new(CellPoint.south),
                new(CellPoint.north), new(CellPoint.east), new(CellPoint.south),
                new(CellPoint.north), new(right), new(CellPoint.east),
            },
            new CellPoint[] { //6
                new(CellPoint.north), new(right), new(downRight),
                new(CellPoint.north), new(downRight), new(CellPoint.south),
            },
            new CellPoint[] { //7
                new(CellPoint.west), new(downRight), new(down),
                new(CellPoint.north), new(downRight), new(CellPoint.west),
                new(CellPoint.north), new(right), new(downRight),
            },
            new CellPoint[] { //8
                new(CellPoint.west), new(origin), new(CellPoint.north),
            },
            new CellPoint[] { //9
                new(origin), new(CellPoint.north), new(CellPoint.south),
                new(origin), new(CellPoint.south), new(down),
            },
            new CellPoint[] { //10
                new(origin), new(CellPoint.north), new(CellPoint.west),
                new(CellPoint.west), new(CellPoint.north), new(CellPoint.east),
                new(CellPoint.west), new(CellPoint.east), new(CellPoint.south),
                new(CellPoint.south), new(CellPoint.east), new(downRight),
            },
            new CellPoint[] { //11
                new(origin), new(CellPoint.north), new(down),
                new(CellPoint.north), new(CellPoint.east), new(down),
                new(CellPoint.east), new(downRight), new(down),
            },
            new CellPoint[] { //12
                new(CellPoint.west), new(origin), new(CellPoint.east),
                new(origin), new(right), new(CellPoint.east),
            },
            new CellPoint[] { //13
                new(origin), new(right), new(CellPoint.east),
                new(origin), new(CellPoint.east), new(CellPoint.south),
                new(origin), new(CellPoint.south), new(down),
            },
             new CellPoint[] { //14
                new(origin), new(right), new(CellPoint.west),
                new(right), new(downRight), new(CellPoint.south),
                new(right), new(CellPoint.south), new(CellPoint.west),
            },
            new CellPoint[] { //15
                new(origin), new(right), new(downRight),
                new(origin), new(downRight), new(down)
            }
        };
    }

    (List<int>, List<Vector2>) GenerateTrianglesAndAddAdditionalVertices(List<Vector3> vertices)
    {
        CellPoint[][] cellLookup = InitLookUpTable();
        OceanVertData[] oceanVerts = terrainFace.BellowZeroVertices;
        List<int> triangles = new(terrainFace.Mesh.triangles.Length);
        List<Vector2> uvs = new();
        //adds to current grid position (x, y) to get desirable corner of cell
        Queue<Vector2> uvQueue = new();

        for (int i = 0; i < powResolution; i++)
        {
            int y = i / _resolution;
            int x = i - y * _resolution;
            if (oceanVerts[i].isOcean)
            {
                Vector3 dir = terrainFace.GetUnitSpherePointFromXY(x, y);
                Vector2 uv = GetUV(dir);
                uvs.Add(uv);
            }
            if (!oceanVerts[i].isOcean && !oceanVerts[i].isShore) continue;//check only the shore or ocean verts for optimization

            if (x == _resolution - 1 || y == _resolution - 1) continue;// the edges don't form cells, so skip
            Vector2 gridPos = new(x, y);

            //to later retrieve the points of the line that the additional vertex will sit on
            _corners[0] = oceanVerts[i];
            _corners[1] = oceanVerts[i + right];
            _corners[2] = oceanVerts[i + downRight];
            _corners[3] = oceanVerts[i + down];

            //checking the corners of cell to see what type of contour I need
            int a = BoolToInt(_corners[0].isOcean);
            int b = BoolToInt(_corners[1].isOcean);
            int c = BoolToInt(_corners[2].isOcean);
            int d = BoolToInt(_corners[3].isOcean);

            int contourHash = GetContour(a, b, c, d);
            int addedVertIndex = vertices.Count;
            foreach (CellPoint cellVert in cellLookup[contourHash])
            {
                
                if (!cellVert.IsAdditional)
                {
                    //goes through the cells 
                    triangles.Add(oceanVerts[i + cellVert.IndexOffset].VerticesArrayIndex);
                    continue;
                }
                //the point that sits in between two vertices of the current cell
                Vector2 edgePoint = GetLerpedEdgePoint(cellVert, gridPos);
                Vector3 pointOnUnitSphere = terrainFace.GetUnitSpherePointFromXY(edgePoint.x, edgePoint.y);

                Vector2 uv = GetUV(pointOnUnitSphere);
                Vector3 vertex = pointOnUnitSphere * shapeGenerator.PlanetRadius;
                
                uvQueue.Enqueue(uv);
                vertices.Add(vertex);
                triangles.Add(addedVertIndex++);
            }
            
        }
        //find out if uv line goes 
        while (uvQueue.Count > 0)
        {
            uvs.Add(uvQueue.Dequeue());
        }
        // goes through every triangle and checks if it spans the 0/1 seam in uv space
        for (int i = 0; i < triangles.Count; i += 3)
        {
            FixTriangle
            (
                i, i + 1, i + 2,
                triangles[i], triangles[i + 1], triangles[i + 2],
                uvs, vertices, triangles
            );
        }

        return (triangles, uvs);
    }

    
    void FixTriangle(int t1, int t2, int t3, int v1, int v2, int v3, List<Vector2> uvs, List<Vector3> vertices, List<int> triangles)
    {
        Vector2 uv1 = uvs[v1];
        Vector2 uv2 = uvs[v2];
        Vector2 uv3 = uvs[v3];

        //float min12 = Mathf.Min(uv1.x, uv2.x);
        //float max12 = Mathf.Max(uv1.x, uv2.x);

        //float min20 = Mathf.Min(uv2.x, uv0.x);
        //float max20 = Mathf.Max(uv2.x, uv0.x);

        //if(uv1.x - uv2.x > .5f)
        //{
        //    vertices.Add(vertices[v1]);
        //    uvs.Add(new Vector2(0, 1));
        //    triangles[t1] = vertices.Count - 1;
        //}
        //if (uv2.x - uv1.x > .5f)
        //{
        //    vertices.Add(vertices[v2]);
        //    uvs.Add(new Vector2(0, 1));
        //    triangles[t2] = vertices.Count - 1;
        //}


        //if (uv2.x - uv3.x > .5f)
        //{
        //    vertices.Add(vertices[v2]);
            
        //    uvs.Add(new Vector2(0, 1));
        //    triangles[t2] = vertices.Count - 1;
        //}
        //if (uv3.x - uv2.x > .5f)
        //{
        //    vertices.Add(vertices[v3]);
        //    uvs.Add(new Vector2(0, 1));
        //    triangles[t3] = vertices.Count - 1;
        //}
        //if (max12 - min12 > .5f)
        //{
        //    uvs[v2] = new Vector2(0, 0);
        //    //uvs[v3] = new Vector2(0, 0);
        //}
        //if (max20 - min20 > .5f)
        //{
        //    uvs[v3] = new Vector2(0, 0);
        //    //uvs[v1] = new Vector2(0, 0);
        //}
        // If the triangle spans the 0/1 seam
        //if (maxU - minU > 0.5f)
        //{
        //    uvs[v1] = new Vector2(0, 0);
        //    uvs[v2] = new Vector2(0, 0);
        //    uvs[v3] = new Vector2(0, 0);

        //    //// Fix wrap by duplicating vertices with corrected UVs
        //    //v1 = FixWrappedUV(v1, uvs, vertices);
        //    //v2 = FixWrappedUV(v2, uvs, vertices);
        //    //v3 = FixWrappedUV(v3, uvs, vertices);
        //}

        //triangles[t1] = v1;
        //triangles[t2] = v2;
        //triangles[t3] = v3;
    }

    int FixWrappedUV(int index, List<Vector2> uvs, List<Vector3> vertices)
    {
        Vector3 v = vertices[index];
        Vector2 uv = uvs[index];
        bool xWrap = uv.x < 0.5f;
        bool yWrap = uv.y < 0.5f;
        if (xWrap || yWrap)
        {
            // Need to shift U up to unwrap the seam
            if(xWrap) uv.x += 1f;
            if(yWrap) uv.y += 1f;

            vertices.Add(v);
            uvs.Add(uv);
            return vertices.Count - 1; // return new index
        }
        else
        {
            return index; // no change
        }
    }

    // polar coordinates uv mapping for sphere
    Vector2 GetUV2(Vector3 dir)
    {
        float u = 0.5f + Mathf.Atan2(dir.z, dir.x) / (2 * Mathf.PI);
        float v = 0.5f - Mathf.Asin(dir.y) / Mathf.PI;
        return new Vector2(u, v);
    }
    //for future experimenting
    Vector2 GetUV(Vector3 dir)
    {
        float absX = Mathf.Abs(dir.x);
        float absY = Mathf.Abs(dir.y);
        float absZ = Mathf.Abs(dir.z);

        bool isXPositive = dir.x > 0;
        bool isYPositive = dir.y > 0;
        bool isZPositive = dir.z > 0;

        float u, v;

        if (absX >= absY && absX >= absZ)
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
        else if (absY >= absX && absY >= absZ)
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

    List<Vector3> GenerateNormals(List<Vector3> vertices)
    {
        List<Vector3> normals = new(vertices.Count);
        foreach (Vector3 vert in vertices)
        {
            normals.Add(vert.normalized);
        }

        return normals;
    }

    Vector2 GetLerpedEdgePoint(CellPoint cellPoint, Vector2 gridPos)
    {
        int corner1Index = cellPoint.AdditionalPos.Corner1Offset;
        int corner2Index = cellPoint.AdditionalPos.Corner2Offset;
        //get oceanVertData of corner to use the "DistanceToZero" value
        OceanVertData Corner1VertData = _corners[corner1Index];
        OceanVertData Corner2VertData = _corners[corner2Index];
        //lerp between these positions to get the edge point
        Vector2 corner1Pos = _gridPosOffsets[corner1Index] + gridPos;
        Vector2 corner2Pos = _gridPosOffsets[corner2Index] + gridPos;
        //ocean level is interpreted as 0, but for the interpolation formula to work, I need to interpret it as 1, so I just add one to a and b
        float a = Corner1VertData.DistanceToOceanLevel + 1;
        float b = Corner2VertData.DistanceToOceanLevel + 1;
        return LerpCloseToOne(a , b, corner1Pos, corner2Pos);

    }

    Vector2 LerpCloseToOne(float valueA, float valueB, Vector2 a, Vector2 b)
    {
        return Vector2.Lerp(a, b, (1 - valueA) / (valueB - valueA));
    }

    // abcd are the values of a binary number xxxx, they represent the corners of the cell
    //     a   b   <--- one cell
    //     d   c
    // the result is mapped to an entry in the lookup table that returns which countour
    // corresponds to the corners formation
    int GetContour(int a, int b, int c, int d)
    {
        return a * 8 + b * 4 + c * 2 + d * 1;
    }

    int BoolToInt(bool boolean)
    {
        return boolean ? 1 : 0;
    }
}