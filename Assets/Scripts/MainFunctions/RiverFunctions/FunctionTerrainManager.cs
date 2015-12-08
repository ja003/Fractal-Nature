using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FunctionTerrainManager {

    public RiverGenerator rg;

    public FunctionMathCalculator fmc;


    public Vector3[,] vertices;
    public int terrainSize;

    public FunctionTerrainManager(RiverGenerator rg)
    {
        this.rg = rg;
        vertices = rg.vertices;
        terrainSize = rg.terrainSize;

    }

    public void ClearTerrain()
    {
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {
                vertices[x, z].y = 0;
            }
        }
    }

    public float GetMedian(int _x, int _z, int regionSize)
    {
        //List<float> heights = new List<float>();
        float heightSum = 0;
        int count = 0;
        for (int x = _x - regionSize; x < _x + regionSize; x++)
        {
            for (int z = _z - regionSize; z < _z + regionSize; z++)
            {
                if (CheckBounds(x, z))
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

    public float GetMedian(int _x, int _z, int regionSize, float[,] depthMap)
    {
        //List<float> heights = new List<float>();
        float heightSum = 0;
        int count = 0;
        for (int x = _x - regionSize; x < _x + regionSize; x++)
        {
            for (int z = _z - regionSize; z < _z + regionSize; z++)
            {
                if (CheckBounds(x, z) && depthMap[x, z] != 666)
                {
                    heightSum += depthMap[x, z];
                    count++;
                }
            }
        }
        if (count == 0)
            return 0;
        return heightSum / count;
    }

    public List<Vertex> Get8Neighbours(Vertex center, int step, int offset, float threshold)
    {
        List<Vertex> neighbours = new List<Vertex>();
        int x = center.x;
        int z = center.z;

        //left
        if (CheckBounds(x - step, z, offset) && vertices[x - step, z].y < threshold) { neighbours.Add(new Vertex(x - step, z, vertices[x - step, z].y)); }
        //up
        if (CheckBounds(x, z + step, offset) && vertices[x, z + step].y < threshold) { neighbours.Add(new Vertex(x, z + step, vertices[x, z + step].y)); }
        //righ
        if (CheckBounds(x + step, z, offset) && vertices[x + step, z].y < threshold) { neighbours.Add(new Vertex(x + step, z, vertices[x + step, z].y)); }
        //down
        if (CheckBounds(x, z - step, offset) && vertices[x, z - step].y < threshold) { neighbours.Add(new Vertex(x, z - step, vertices[x, z - step].y)); }

        //leftUp
        if (CheckBounds(x - step, z + step, offset) && vertices[x - step, z + step].y < threshold) { neighbours.Add(new Vertex(x - step, z + step, vertices[x - step, z + step].y)); }
        //rightUp
        if (CheckBounds(x + step, z + step, offset) && vertices[x + step, z + step].y < threshold) { neighbours.Add(new Vertex(x + step, z + step, vertices[x + step, z + step].y)); }
        //righDown
        if (CheckBounds(x + step, z - step, offset) && vertices[x + step, z - step].y < threshold) { neighbours.Add(new Vertex(x + step, z - step, vertices[x + step, z - step].y)); }
        //leftDown
        if (CheckBounds(x - step, z - step, offset) && vertices[x - step, z - step].y < threshold) { neighbours.Add(new Vertex(x - step, z - step, vertices[x - step, z - step].y)); }


        return neighbours;
    }

    //obsolete!!!
    public List<Vertex> Get4Neighbours(Vertex center, int step, int offset)
    {
        List<Vertex> neighbours = new List<Vertex>();
        int x = center.x;
        int z = center.z;
        //left
        if (CheckBounds(x - step, z, offset)) { neighbours.Add(new Vertex(x - step, z, vertices[x - step, z].y)); }
        //up
        if (CheckBounds(x, z + step, offset)) { neighbours.Add(new Vertex(x, z + step, vertices[x, z + step].y)); }
        //righ
        if (CheckBounds(x + step, z, offset)) { neighbours.Add(new Vertex(x + step, z, vertices[x + step, z].y)); }
        //down
        if (CheckBounds(x, z - step, offset)) { neighbours.Add(new Vertex(x, z - step, vertices[x, z - step].y)); }

        return neighbours;
    }

    public Vertex GetLowestRegionCenter(int radius, int offset)
    {
        double lowestSum = 10;
        Vertex lowestRegionCenter = new Vertex(offset, offset, 10);
        for (int x = offset; x < terrainSize - offset; x += radius)
        {
            for (int z = offset; z < terrainSize - offset; z += radius)
            {
                double sum = 0;
                for (int i = x - radius; i < x + radius; i++)
                {
                    for (int j = z - radius; j < z + radius; j++)
                    {
                        if (CheckBounds(i, j))
                            sum += vertices[i, j].y;
                    }
                }
                if (sum < lowestSum)
                {
                    lowestSum = sum;
                    lowestRegionCenter.Rewrite(x, z, vertices[x, z].y);
                }

            }
        }
        return lowestRegionCenter;
    }

    /// <summary>
    /// modifies current terrain
    /// finds 'x' highest peaks which are at least 'radius' away from each other
    /// then all vertices hight is lowered using GetScale function based on logarithm 
    /// </summary>
    /// <param name="count"></param>
    /// <param name="radius"></param>
    public void PerserveMountains(int count, int radius)
    {
        int scaleFactor = 20;

        List<Vertex> peaks = new List<Vertex>();
        for (int i = 0; i < count; i++)
        {
            if (FindNextHighestPeak(radius, peaks) != null)
                peaks.Add(FindNextHighestPeak(radius, peaks));
            //Debug.Log(i+"-:"+peaks[i]);
        }

        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {
                Vertex vert = new Vertex(x, z, vertices[x, z].y);
                bool isInRange = false;
                double scale = 0;
                foreach (Vertex v in peaks)
                {
                    if (fmc.IsInRange(vert, v, radius))
                    {
                        isInRange = true;
                    }
                    if (fmc.GetScale(vert, v, radius) > scale)
                    {
                        scale = fmc.GetScale(vert, v, radius);
                    }
                }

                vertices[x, z].y *= (float)Math.Pow(scale, scaleFactor);

            }
        }



        //blur the peaks
        float blurringFactor = radius / 10;
        int kernelSize = radius / 10;

        for (int i = 0; i < peaks.Count; i++)
        {
            rg.filtermanager.applyGaussianBlur(blurringFactor, kernelSize,
                new Vector3(peaks[i].x - kernelSize, 0, peaks[i].z - kernelSize),
                new Vector3(peaks[i].x + kernelSize, 0, peaks[i].z + kernelSize));

        }

        rg.terrain.build();
    }

    public Vertex GetHighestpoint()
    {
        Vertex highestPoint = new Vertex(10, 10, vertices[10, 10].y);
        for (int x = 1; x < terrainSize - 1; x++)
        {
            for (int z = 1; z < terrainSize - 1; z++)
            {
                if (vertices[x, z].y > highestPoint.height)
                    highestPoint = new Vertex(x, z, vertices[x, z].y);
            }
        }
        return highestPoint;
    }

    public Vertex FindNextHighestPeak(int radius, List<Vertex> foundPeaks)
    {
        int border = 20;
        Vertex highestPeak = new Vertex(0, 0, 0);
        for (int x = border; x < terrainSize - border; x++)
        {
            for (int z = border; z < terrainSize - border; z++)
            {
                if (vertices[x, z].y > highestPeak.height)
                {
                    bool isInRange = false;
                    foreach (Vertex v in foundPeaks)
                    {
                        if (fmc.IsInRange(new Vertex(x, z, vertices[x, z].y), v, radius * 2))
                        {
                            isInRange = true;
                        }
                    }
                    if (!isInRange)
                        highestPeak = new Vertex(x, z, vertices[x, z].y);
                }
            }
        }
        if (highestPeak.x == 0 && highestPeak.z == 0)
        {
            //Debug.Log("no place for more mountains");
            return null;
        }

        return highestPeak;
    }

    public Vertex GetLowestPointInArea(int _x, int _z, int area)
    {
        Vertex lowestVert = new Vertex(_x, _z, vertices[_x, _z].y);
        for (int x = _x - area; x <= _x + area; x++)
        {
            for (int z = _z - area; z <= _z + area; z++)
            {
                if (CheckBounds(x, z) && vertices[x, z].y < lowestVert.height)
                {
                    lowestVert.Rewrite(x, z, vertices[x, z].y);
                }
            }
        }
        return lowestVert;
    }

    public bool CheckBounds(int x, int z, int offset)
    {
        return x > offset && x < terrainSize - 1 - offset && z > offset && z < terrainSize - 1 - offset;
    }

    public bool CheckBounds(int x, int z)
    {
        return CheckBounds(x, z, 0);
    }

    public bool CheckBounds(Vertex vertex)
    {
        return CheckBounds(vertex.x, vertex.z);
    }

    public float GetSumFromNeighbourhood(int x, int z, int offset)
    {
        float sum = 0;
        for (int _x = x - offset; _x <= x + offset; _x++)
        {
            for (int _z = z - offset; _z <= z + offset; _z++)
            {
                if (CheckBounds(_x,_z))
                {
                    sum += vertices[_x, _z].y;
                }
            }
        }
        return sum;
    }


}
