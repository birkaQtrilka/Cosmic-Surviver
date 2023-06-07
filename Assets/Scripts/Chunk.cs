using System.Collections.Generic;
using UnityEngine;

public class Chunk 
{
    public Chunk[] children;
    public Chunk parent;
    public Vector3 position;
    public float radius;
    public int detailLevel;
    public Vector3 localUp;
    public Vector3 axisA;
    public Vector3 axisB;

    public Chunk(Chunk[] children, Chunk parent, Vector3 position, float radius, int detailLevel, Vector3 localUp, Vector3 axisA, Vector3 axisB)
    {
        this.children = children;
        this.parent = parent;
        this.position = position;
        this.radius = radius;
        this.detailLevel = detailLevel;
        this.localUp = localUp;
        this.axisA = axisA;
        this.axisB = axisB;
    }

    public void GenerateChildren()
    {
        if (detailLevel > 8 || detailLevel < 0) return;

        if (Vector3.Distance(position.normalized, Planet.player.position) > Planet.detailLevelDistances[detailLevel])
            return;
        float halfRadius = .5f * radius;
        children = new Chunk[4];
        children[0] = new(new Chunk[0], this, position + halfRadius * axisA + halfRadius * axisB, halfRadius, detailLevel + 1, localUp, axisA, axisB); ;
        children[1] = new(new Chunk[0], this, position + halfRadius * axisA - halfRadius * axisB, halfRadius, detailLevel + 1, localUp, axisA, axisB); ;
        children[2] = new(new Chunk[0], this, position - halfRadius * axisA + halfRadius * axisB, halfRadius, detailLevel + 1, localUp, axisA, axisB); ;
        children[3] = new(new Chunk[0], this, position - halfRadius * axisA - halfRadius * axisB, halfRadius, detailLevel + 1, localUp, axisA, axisB); ;
    
        foreach (Chunk child in children)
            child.GenerateChildren();
    }
    public Chunk[] GetVisibleChildren()
    {
        List<Chunk> toBeRendered = new ();
        if (children.Length > 0)
            foreach (Chunk child in children)
                toBeRendered.AddRange(child.GetVisibleChildren());
        else
            toBeRendered.Add(this);
        return toBeRendered.ToArray();
    }
    public (Vector3[], int[]) CalculateVerticesAndTriangles(int triangleOffset)
    {
        int resolution = 8, triIndex=0;
        var vertices = new Vector3[resolution*resolution];
        var triangles = new int[(resolution-1)*(resolution-1)*6];
        for (int y = 0;  y < resolution; y++)
            for (int x = 0; x < resolution; x++)
            {
                int i = x + y * resolution;
                Vector2 percent = new Vector2(x, y) / (resolution - 1);
                Vector3 pointonUnitCube = position + ((percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB);

                var pointonUnitSphere = pointonUnitCube.normalized* radius;
                vertices[i] = pointonUnitSphere;

                if (x != resolution - 1 && y != resolution - 1)
                {
                    triangles[triIndex] = triangleOffset + i;
                    triangles[triIndex + 1] = triangleOffset + i + resolution + 1;
                    triangles[triIndex + 2] = triangleOffset + i + resolution;

                    triangles[triIndex + 3] = triangleOffset + i;
                    triangles[triIndex + 4] = triangleOffset + i + 1;
                    triangles[triIndex + 5] = triangleOffset + i + resolution + 1;

                    triIndex += 6;
                }
                //Vector3 pointOnUnitCube = localUp + (percent.x - .5f) * 2 * axisA + (percent.y - .5f) * 2 * axisB;
            }

        return (vertices, triangles);
    }
}
