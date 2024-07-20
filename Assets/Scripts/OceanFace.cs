using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OceanFace 
{
    Mesh _mesh;
    int powResolution;
    int _resolution;
    TerrainFace terrainFace;
    ShapeGenerator shapeGenerator;

    int downRight ;
    int down;
    int downLeft;
    int left;
    int upLeft;
    int up;
    int upRight;
    int right;
    int origin;

    struct IsoLine
    {
        public bool IsAdditional;
        public int OceanVertIndexOffset;
        public (int,int) AdditionalPos;

        public readonly static (int, int) north = (0,1) ;
        public readonly static (int, int) east = (1,2) ;
        public readonly static (int, int) south = (2,3) ;
        public readonly static (int, int) west = (3,0) ;

        public IsoLine(int offset)
        {
            IsAdditional = false;
            OceanVertIndexOffset = offset;
            AdditionalPos = (0,0);
        }

        public IsoLine((int,int) additionalPos)
        {
            IsAdditional = true;
            OceanVertIndexOffset = -999;
            AdditionalPos = additionalPos;
        }
    }

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
        //List<Vector2> uv = new(powResolution);


        List<int> triangles = GenerateTriangles(vertices);

        _mesh.Clear();
        _mesh.vertices = vertices.ToArray();
        _mesh.triangles = triangles.ToArray();
        _mesh.RecalculateNormals();
        //_mesh.uv = uv.ToArray();
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
                oceanVerts[i].Index = vertices.Count - 1;
                //how far it is from ocean level
                //uv[i] = new Vector2(x / (float)_resolution, y / (float)_resolution);
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
    IsoLine[][] InitLookUpTable()
    {
        //relative to topleft corner of cell (origin)
        return new IsoLine[][]
        {
            new IsoLine[] { /*no triangles*/},//0
            new IsoLine[] { // 1
                //first triangle
                new(IsoLine.west), new(IsoLine.south), new(down),
            },
            new IsoLine[] { //2
                new(IsoLine.east), new(downRight), new(IsoLine.south),
            },
            new IsoLine[] { //3
                //first triangle
                new(IsoLine.west), new(downRight), new(down),
                //second triangle
                new(IsoLine.west), new(IsoLine.east), new(downRight),
            },
            new IsoLine[] { //4
                new(IsoLine.north), new(right), new(IsoLine.east),
            },
            new IsoLine[] { //5
                new(IsoLine.west), new(IsoLine.south), new(down),
                new(IsoLine.west), new(IsoLine.north), new(IsoLine.south),
                new(IsoLine.north), new(IsoLine.east), new(IsoLine.south),
                new(IsoLine.north), new(right), new(IsoLine.east),
            },
            new IsoLine[] { //6
                new(IsoLine.north), new(right), new(downRight),
                new(IsoLine.north), new(downRight), new(IsoLine.south),
            },
            new IsoLine[] { //7
                new(IsoLine.west), new(downRight), new(down),
                new(IsoLine.north), new(downRight), new(IsoLine.west),
                new(IsoLine.north), new(right), new(downRight),
            },
            new IsoLine[] { //8
                new(IsoLine.west), new(origin), new(IsoLine.north),
            },
            new IsoLine[] { //9
                new(origin), new(IsoLine.north), new(IsoLine.south),
                new(origin), new(IsoLine.south), new(down),
            },
            new IsoLine[] { //10
                new(origin), new(IsoLine.north), new(IsoLine.west),
                new(IsoLine.west), new(IsoLine.north), new(IsoLine.east),
                new(IsoLine.west), new(IsoLine.east), new(IsoLine.south),
                new(IsoLine.south), new(IsoLine.east), new(downRight),
            },
            new IsoLine[] { //11
                new(origin), new(IsoLine.north), new(down),
                new(IsoLine.north), new(IsoLine.east), new(down),
                new(IsoLine.east), new(downRight), new(down),
            },
            new IsoLine[] { //12
                new(IsoLine.west), new(origin), new(IsoLine.east),
                new(origin), new(right), new(IsoLine.east),
            },
            new IsoLine[] { //13
                new(origin), new(right), new(IsoLine.east),
                new(origin), new(IsoLine.east), new(IsoLine.south),
                new(origin), new(IsoLine.south), new(down),
            },
             new IsoLine[] { //14
                new(origin), new(right), new(IsoLine.west),
                new(right), new(downRight), new(IsoLine.south),
                new(right), new(IsoLine.south), new(IsoLine.west),
            },
            new IsoLine[] { //15
                new(origin), new(right), new(downRight),
                new(origin), new(downRight), new(down)
            }
        };
    }

    List<int> GenerateTriangles(List<Vector3> vertices)
    {
        IsoLine[][] shoreContours = InitLookUpTable();
        OceanVertData[] oceanVerts = terrainFace.BellowZeroVertices;
        List<int> triangles = new(terrainFace.Mesh.triangles.Length);
        OceanVertData[] verts = new OceanVertData[4];
        Vector2[] offsets = new Vector2[]
        {
            new(0,0),
            new(1,0),
            new(1,1),
            new(0,1)
        };
        for (int i = 0; i < powResolution; i++)
        {
            if (!oceanVerts[i].isOcean && !oceanVerts[i].isShore) continue;//check only the shore or ocean verts for optimization
            
            int y = i / _resolution;
            int x = i - y * _resolution;

            if (x == _resolution - 1 || y == _resolution - 1) continue;// the edges don't form cells, so skip

            verts[0] = oceanVerts[i];
            verts[1] = oceanVerts[i + right];
            verts[2] = oceanVerts[i + downRight];
            verts[3] = oceanVerts[i + down];

            //checking the corners of cell to see what type of contour I need
            int a = verts[0].isOcean ? 1 : 0;
            int b = verts[1].isOcean ? 1 : 0;
            int c = verts[2].isOcean ? 1 : 0;
            int d = verts[3].isOcean ? 1 : 0;

            int shoreVertContour = GetContour(a, b, c, d);
            int addedVertIndex = vertices.Count;
            foreach (IsoLine isoLine in shoreContours[shoreVertContour])//need to iterate by 2 verts
            {
                if (!isoLine.IsAdditional)
                {
                    triangles.Add(oceanVerts[i + isoLine.OceanVertIndexOffset].Index);
                    continue;
                }

                OceanVertData point1 = verts[isoLine.AdditionalPos.Item1];
                OceanVertData point2 = verts[isoLine.AdditionalPos.Item2];
                Vector2 offset1 = offsets[isoLine.AdditionalPos.Item1];
                Vector2 offset2 = offsets[isoLine.AdditionalPos.Item2];

                Vector2 edgePoint = ApproximateContour(point1.DistanceToZero, point2.DistanceToZero, new(x + offset1.x, y + offset1.y), new(x + offset2.x, y + offset2.y));
                Vector3 pointOnUnitSphere = terrainFace.GetUnitSpherePointFromXY(edgePoint.x, edgePoint.y);
                Vector3 vertex = pointOnUnitSphere * shapeGenerator.GetScaledElevation(0);
                
                vertices.Add(vertex);
                triangles.Add(addedVertIndex++);
            }

        }
        return triangles;
    }

    Vector2 ApproximateContour(float valueA, float valueB, Vector2 a, Vector2 b)
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
}
