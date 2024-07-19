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
        public Vector2 AdditionalPos;

        public readonly static Vector2 north = new (.5f, 0);
        public readonly static Vector2 east = new (1f,.5f);
        public readonly static Vector2 south = new (.5f,1f);
        public readonly static Vector2 west = new (0,.5f);
        public IsoLine(int offset)
        {
            IsAdditional = false;
            OceanVertIndexOffset = offset;
            AdditionalPos = new Vector2();
        }
        public IsoLine(Vector2 additionalPos)
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
        this.powResolution = resolution * resolution;
        _resolution = resolution;
        shapeGenerator = terrainFace.shapeGenerator;

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
        OceanVertData[] oceanVerts = terrainFace.VerticesToRemove;
        List<Vector3> vertices = new(powResolution);

        int[] neighborOffsets = new int[]
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

        //mark shore verts in parallel NOT YET PARALLEl
        for (int i = 0; i < oceanVerts.Length; i++)
        {
            int y = i / _resolution;
            int x = i - y * _resolution;

            if (oceanVerts[i].isOcean)  // Skip ocean points
            {
                vertices.Add(oceanVerts[i].WorldPos);
                oceanVerts[i].index = vertices.Count - 1;
                //uv[i] = new Vector2(x / (float)_resolution, y / (float)_resolution);
                continue;
            }

            // Check each neighboring point
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
                    oceanVerts[i].isShore = true;
                    break; // No need to check further if already marked as shore
                }
            }

        }

        return vertices;
    }

    IsoLine[][] InitLookUpTable()
    {
        //relative to topleft corner of cell (origin)
        return new IsoLine[][]
        {
            new IsoLine[] { /*no triangles*/},//0
            new IsoLine[] { // 1
                /*first triangle*/
                new(IsoLine.west), new(IsoLine.south), new(down),
            },
            new IsoLine[] { //2
                new(IsoLine.east), new(downRight), new(IsoLine.south),
            },
            new IsoLine[] { //3
                /*first triangle*/
                new(IsoLine.west), new(downRight), new(down),
                /*second triangle*/
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
        IsoLine[][] lookUpTable = InitLookUpTable();
        OceanVertData[] oceanVerts = terrainFace.VerticesToRemove;
        Vector3 localUp = terrainFace.localUp;
        Vector3 axisA = terrainFace.axisA;
        Vector3 axisB = terrainFace.axisB;
        List<int> triangles = new(terrainFace.Mesh.triangles.Length);

        //make triangles, use square marching algorithm, can be done in parrallel
        for (int i = 0; i < powResolution; i++)
        {
            if (!oceanVerts[i].isOcean && !oceanVerts[i].isShore) continue;
            
            int y = i / _resolution;
            int x = i - y * _resolution;

            if (x == _resolution - 1 || y == _resolution - 1) continue;

            int a = oceanVerts[i].isOcean ? 1 : 0;
            int b = oceanVerts[i + right].isOcean ? 1 : 0;
            int c = oceanVerts[i + downRight].isOcean ? 1 : 0;
            int d = oceanVerts[i + down].isOcean ? 1 : 0;

            int shoreVertState = GetState(a, b, c, d);
            int addedVertIndex = vertices.Count;

            foreach (IsoLine isoLine in lookUpTable[shoreVertState])
            {
                if (isoLine.IsAdditional)//maybe I need to add all additionals then do the rest
                {
                    Vector2 percent = new Vector2(x + isoLine.AdditionalPos.x, y + isoLine.AdditionalPos.y) / (_resolution - 1);//add substract .5f to get north/west/east.. points
                    Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
                    Vector3 pointOnUnitSphere = pointOnUnitCube.normalized;
                    Vector3 vertex = pointOnUnitSphere * shapeGenerator.GetScaledElevation(0);
                    
                    vertices.Add(vertex);
                    triangles.Add(addedVertIndex++);
                    continue;
                }
                triangles.Add(oceanVerts[i + isoLine.OceanVertIndexOffset].index);
            }

        }
        return triangles;
    }

    int GetState(int a, int b, int c, int d)
    {
        return a * 8 + b * 4 + c * 2 + d * 1;
    }
}
