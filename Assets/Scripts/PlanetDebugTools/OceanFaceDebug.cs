using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class OceanFaceDebug : MonoBehaviour
{
    [SerializeField, Range(0,5)] int _faceIndex = 0;
    
    [SerializeField] bool _debug = false;
    [SerializeField] bool _initDebug;
    [SerializeField] bool _step;
    [SerializeField] bool _stepUntilVert;
    [SerializeField] bool _autoInitOnError;

    [Header("visualisation")]
    [SerializeField] int debugStep = 0;
    [SerializeField] int lookupStep = 0;
    [SerializeField, ReadOnly] ReversedList<int> triangles;

    Planet planet;
    readonly OceanVertData[] _corners = new OceanVertData[4];

    List<Vector3> vertices;
    List<Vector3> invalidVertices;
    List<Vector3> addedVertices;
    CellPoint[][] cellLookup;
    OceanVertData[] oceanVerts;
    GridNavigator navigator;
    TerrainFace terrainFace => planet.TerrainFaces[_faceIndex];

    void Start()
    {
        planet = GetComponent<Planet>();
    }

    void OnValidate()
    {
        if (_initDebug)
        {
            _initDebug = false;
            InitDebug();
        }
    }

    public void InitDebug()
    {
        oceanVerts = terrainFace.BellowZeroVertices;
        triangles = new();
        vertices = terrainFace.BellowZeroVertices.Select(v => v.WorldPos).ToList();
        invalidVertices = new List<Vector3>();
        navigator = new GridNavigator(planet.resolution);
        cellLookup = navigator.LookupTable;

        addedVertices = new();
        debugStep = 0;
        lookupStep = 0;
    }

    void OnDrawGizmos()
    {
        try
        {
            Frame();
        }
        catch(Exception e)
        {
            _step = false;

            Debug.LogWarning("Cannot proceed because: " + e);
            if (_autoInitOnError)
            {
                Debug.Log("Auto Initializing the planet");

                planet.GeneratePlanet();
                InitDebug();
            }
        }
    }

    void Frame()
    {
        if (_step)
        {
            if (!_stepUntilVert) _step = false;

            DoStep();
        }

        if (triangles == null || addedVertices == null) return;

        DrawLastAddedVertex();
        DrawAddedVertices();
        DrawInvalidVertices();
        DrawTriangles();
        
    }

    public void DoStep()
    {
        var (addedTriangle, addedVert, exitedLookupLoop) = Step(debugStep, lookupStep, vertices);
        if (addedTriangle != null)
        {
            triangles.Add(addedTriangle.Value);
        }
        if (addedVert != null)
        {
            if (_stepUntilVert) _step = false;

            addedVertices.Add(addedVert.Value);
        }
        if (exitedLookupLoop)
        {
            lookupStep = 0;
            debugStep++;
        }
        else
        {
            lookupStep++;
        }
        if (addedTriangle == null && addedVert == null && !exitedLookupLoop)
        {
            lookupStep = 0;
            debugStep++;

            invalidVertices.Add(oceanVerts[debugStep].WorldPos);
        }
    }

    //(added triangles entry, added vertices entry, exited lookup loop)
    public (int?, Vector3?, bool) Step(int i, int j, List<Vector3> vertices)
    {
        int y = i / planet.resolution;
        int x = i - y * planet.resolution;
        if (!oceanVerts[i].isOcean && !oceanVerts[i].isShore) return (null, null, false);//check only the shore or ocean verts for optimization

        if (x == planet.resolution - 1 || y == planet.resolution - 1) return (null, null, false);// the edges don't form cells, so skip
        Vector2 gridPos = new(x, y);

        //to later retrieve the points of the line that the additional vertex will sit on
        _corners[0] = oceanVerts[i];
        _corners[1] = oceanVerts[i + navigator.right];
        _corners[2] = oceanVerts[i + navigator.downRight];
        _corners[3] = oceanVerts[i + navigator.down];

        //checking the corners of cell to see what type of contour I need
        int a = OceanFace.BoolToInt(_corners[0].isOcean);
        int b = OceanFace.BoolToInt(_corners[1].isOcean);
        int c = OceanFace.BoolToInt(_corners[2].isOcean);
        int d = OceanFace.BoolToInt(_corners[3].isOcean);

        int contourHash = OceanFace.GetContour(a, b, c, d);
        int current_j = 0;
        CellPoint[] cell = cellLookup[contourHash];
        foreach (CellPoint cellVert in cell)
        {
            current_j++;
            if (!cellVert.IsAdditional)
            {
                //see triangle connections in debug
                if (current_j - 1 == j)
                    return (oceanVerts[i + cellVert.IndexOffset].VerticesArrayIndex, null, current_j == cell.Length);
                continue;
            }
            if (current_j - 1 != j) continue;
            Vector2 edgePoint = OceanFace.GetLerpedEdgePoint(cellVert, gridPos, _corners);
            Vector3 pointOnUnitSphere = terrainFace.GetUnitSpherePointFromXY(edgePoint.x, edgePoint.y);

            Vector3 vertex = pointOnUnitSphere * planet.ShapeGenerator.PlanetRadius;
            vertices.Add(vertex);

            return (vertices.Count - 1, vertex, current_j == cell.Length);
        }

        return (null, null, true);
    }

    void DrawLastAddedVertex()
    {
        if (triangles.Count == 0) return;
        Gizmos.color = Color.green;
        int lastIndex = triangles[^1];
        if (oceanVerts != null && lastIndex < oceanVerts.Length)
            Gizmos.DrawSphere(oceanVerts[lastIndex].WorldPos, 0.02f);

    }

    void DrawAddedVertices()
    {
        Gizmos.color = Color.blue;
        foreach (var v in addedVertices)
        {
            Gizmos.DrawSphere(v, 0.01f);
        }
    }

    void DrawInvalidVertices()
    {
        Gizmos.color = Color.yellow;
        foreach (var v in invalidVertices)
        {
            Gizmos.DrawSphere(v, 0.01f);
        }
    }

    void DrawTriangles()
    {
        Gizmos.color = Color.red;
        for (int i = 1; i < triangles.Count; i++)
        {
            if (i % 3 == 0) continue;

            if ((i + 1) % 3 == 0)
            {
                Gizmos.DrawLine(vertices[triangles[i - 2]], vertices[triangles[i]]);
            }
            Gizmos.DrawLine(vertices[triangles[i - 1]], vertices[triangles[i]]);
        }
    }
}
