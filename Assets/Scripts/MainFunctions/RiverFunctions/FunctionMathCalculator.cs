using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FunctionMathCalculator  {

    public RiverGenerator rg;

    public FunctionTerrainManager ftm;

    public Vector3[,] vertices;
    public int terrainSize;

    public FunctionMathCalculator(RiverGenerator rg)
    {
        this.rg = rg;
        vertices = rg.vertices;
        terrainSize = rg.terrainSize;
    }

    public bool BelongsToPath(int x, int z, Vertex v1, Vertex v2, float width)
    {


        Vector2 left;
        Vector2 right;
        if (v1.x < v2.x)
        {
            left = new Vector2(v1.x, v1.z);
            right = new Vector2(v2.x, v2.z);
        }
        else
        {
            left = new Vector2(v2.x, v2.z);
            right = new Vector2(v1.x, v1.z);
        }

        Vector2 dir = new Vector2(right.y - left.y, -(right.x - left.x));
        dir = dir.normalized;

        Vector2 point1 = new Vector2(left.x + dir.x * width, left.y + dir.y * width);
        Vector2 point2 = new Vector2(left.x - dir.x * width, left.y - dir.y * width);
        //has to be correct order! 1-2-3-4
        Vector2 point3 = new Vector2(right.x - dir.x * width, right.y - dir.y * width);
        Vector2 point4 = new Vector2(right.x + dir.x * width, right.y + dir.y * width);

        if (v1.x == 30 && v1.z == 60 && x == 10)
        {
            //Debug.Log(point1);
            //Debug.Log(point2);
            //Debug.Log(point3);
            //Debug.Log(point4);
        }

        List<Vector2> rectangle = new List<Vector2>();
        rectangle.Add(point1);
        rectangle.Add(point2);
        rectangle.Add(point3);
        rectangle.Add(point4);

        bool isInSet = IsInSet(x, z, rectangle);
        return isInSet;
    }

    /// <summary>
    /// y = z
    /// </summary>
    /// <returns></returns>
    public bool IsInSet(int x, int y, List<Vector2> set)
    {
        if (set.Count < 3)
            return false;
        Vector2 last = set[set.Count - 1];
        Vector2 first = set[0];
        //determine position of point
        int position = Math.Sign((first.x - last.x) * (y - last.y) - (first.y - last.y) * (x - last.x));
        if (position == 0)//not exactly correct!!!
        {
            Vector2 A = set[0];
            Vector2 B = set[1];
            position = Math.Sign((B.x - A.x) * (y - A.y) - (B.y - A.y) * (x - A.x));
        }
        //point has to have smae position from each part of set
        for (int i = 0; i < set.Count - 1; i++)
        {
            Vector2 A = set[i];
            Vector2 B = set[i + 1];
            int pos = Math.Sign((B.x - A.x) * (y - A.y) - (B.y - A.y) * (x - A.x));

            if (pos != position)
                return false;
        }

        return true;
    }

    public bool IsInBox(int x, int z, Vertex v1, Vertex v2)
    {
        bool result = true;
        if (v1.x < v2.x)
        {
            if (x < v1.x || v2.x < x)
                result = false;
        }
        else
        {
            if (x < v2.x || v1.x < x)
                result = false;
        }

        if (v1.z < v2.z)
        {
            if (z < v1.z || v2.z < z)
                result = false;
        }
        else
        {
            if (z < v2.z || v1.z < z)
                result = false;
        }

        return result;
    }




    public float GetMedian(int _x, int _z, int regionSize, Vector3[,] vertices)
    {
        //List<float> heights = new List<float>();
        float heightSum = 0;
        int count = 0;
        for (int x = _x - regionSize; x < _x + regionSize; x++)
        {
            for (int z = _z - regionSize; z < _z + regionSize; z++)
            {
                if (ftm.CheckBounds(x, z))
                {
                    heightSum += vertices[x, z].y;
                    count++;
                }
            }
        }
        if (count == 0)
            return 0;
        return heightSum / count;
    }


    public bool IsCloseTo(int value, int border, int offset)
    {
        return border - offset < value && value < border + offset;
    }

    /// <summary>
    /// general line equation: ax + by + c = 0
    /// distance = |a*x_0 + b*y_0 + c| / sqrt(a^2 + b^2)
    /// </summary>
    /// <returns></returns>
    public float GetDistanceFromLine(Vertex point, int a, int b, int c)
    {
        return (float)(Math.Abs(a * point.x + b * point.z + c) / (Math.Sqrt(a * a + b * b)));
    }

    public float GetDistanceFromLine(Vertex point, Vertex v1, Vertex v2)
    {
        //general line equation parameters
        int a = v1.z - v2.z;
        int b = -(v1.x - v2.x);
        int c = -(a * v1.x) - (b * v1.z);

        return GetDistanceFromLine(point, a, b, c);
    }


    public bool IsInArea(int x, int z, int xStart, int xEnd, int zStart, int zEnd)
    {
        return (x > xStart && x < xEnd && z > zStart && z < zEnd);
    }

    public double GetScale(Vertex v1, Vertex v2, int radius)
    {
        return Math.Log(terrainSize - GetDistance(v1, v2), terrainSize);
    }

    public float GetDistance(Vertex v1, Vertex v2)
    {
        return (float)Math.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.z - v2.z) * (v1.z - v2.z));
    }

    public int GetManhattanDistance(Vertex v1, Vertex v2)
    {
        return Math.Abs(v1.x - v2.x) + Math.Abs(v1.z - v2.z);
    }

    public bool IsInRange(Vertex vert, Vertex center, int radius)
    {
        if (GetDistance(vert, center) < radius)
            return true;
        return false;
    }

}
