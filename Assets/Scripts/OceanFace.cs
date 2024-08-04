using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OceanFace
{
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
        public readonly bool IsAdditional;
        public readonly int OceanVertIndexOffset;
        public readonly CornerIndexes AdditionalPos;

        public readonly static CornerIndexes north = new(0, 1);
        public readonly static CornerIndexes east = new(1, 2);
        public readonly static CornerIndexes south = new(2, 3);
        public readonly static CornerIndexes west = new(3, 0);

        public CellPoint(int offset)
        {
            IsAdditional = false;
            OceanVertIndexOffset = offset;
            AdditionalPos = new(0, 0);
        }

        public CellPoint(CornerIndexes additionalPos)
        {
            IsAdditional = true;
            OceanVertIndexOffset = -999;
            AdditionalPos = additionalPos;
        }
    }

    Mesh _mesh;
    int powResolution;
    int _resolution;
    TerrainFace terrainFace;
    ShapeGenerator shapeGenerator;

    int downRight;
    int down;
    int downLeft;
    int left;
    int upLeft;
    int up;
    int upRight;
    int right;
    int origin;

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

        List<int> triangles = GenerateTriangles(vertices);

        _mesh.Clear();
        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
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

    List<int> GenerateTriangles(List<Vector3> vertices)
    {
        CellPoint[][] shoreContours = InitLookUpTable();
        OceanVertData[] oceanVerts = terrainFace.BellowZeroVertices;
        List<int> triangles = new(terrainFace.Mesh.triangles.Length);
        //adds to current grid position (x, y) to get desirable corner of cell
        

        for (int i = 0; i < powResolution; i++)
        {
            if (!oceanVerts[i].isOcean && !oceanVerts[i].isShore) continue;//check only the shore or ocean verts for optimization

            int y = i / _resolution;
            int x = i - y * _resolution;

            if (x == _resolution - 1 || y == _resolution - 1) continue;// the edges don't form cells, so skip
            Vector2 gridPos = new(x, y);

            //to later retrieve the points of the line that the additional vertex will sit on
            _corners[0] = oceanVerts[i];
            _corners[1] = oceanVerts[i + right];
            _corners[2] = oceanVerts[i + downRight];
            _corners[3] = oceanVerts[i + down];

            //checking the corners of cell to see what type of contour I need
            int a = CastToInt(_corners[0].isOcean);
            int b = CastToInt(_corners[1].isOcean);
            int c = CastToInt(_corners[2].isOcean);
            int d = CastToInt(_corners[3].isOcean);

            int shoreVertContour = GetContour(a, b, c, d);
            int addedVertIndex = vertices.Count;
            foreach (CellPoint cellPoint in shoreContours[shoreVertContour])
            {
                if (!cellPoint.IsAdditional)
                {
                    triangles.Add(oceanVerts[i + cellPoint.OceanVertIndexOffset].VerticesArrayIndex);
                    continue;
                }
                Vector2 edgePoint = GetLerpedEdgePoint(cellPoint, gridPos);
                Vector3 pointOnUnitSphere = terrainFace.GetUnitSpherePointFromXY(edgePoint.x, edgePoint.y);
                Vector3 vertex = pointOnUnitSphere * shapeGenerator.PlanetRadius;

                vertices.Add(vertex);
                triangles.Add(addedVertIndex++);
            }

        }
        return triangles;
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

    int CastToInt(bool boolean)
    {
        return boolean ? 1 : 0;
    }
}