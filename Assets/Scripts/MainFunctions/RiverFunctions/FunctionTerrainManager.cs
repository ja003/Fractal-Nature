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
        return Get8Neighbours(center, step, offset, threshold, 0, terrainSize, 0, terrainSize);
    }

    public List<Vertex> Get8Neighbours(Vertex center, int step, int offset, float threshold, int x_min, int x_max, int z_min, int z_max)
    {
        List<Vertex> neighbours = new List<Vertex>();
        int x = center.x;
        int z = center.z;

        if(fmc.GetDistanceFromCorner(x,z, x_min, x_max, z_min, z_max) < 2 * offset) //dont process points too close to corners
        {
            return neighbours;
        }

        //left
        if (CheckBounds(x - step, z, offset, x_min,x_max,z_min,z_max) && vertices[x - step, z].y < threshold)
            { neighbours.Add(new Vertex(x - step, z, vertices[x - step, z].y)); }
        //up
        if (CheckBounds(x, z + step, offset, x_min, x_max, z_min, z_max) && vertices[x, z + step].y < threshold)
            { neighbours.Add(new Vertex(x, z + step, vertices[x, z + step].y)); }
        //righ
        if (CheckBounds(x + step, z, offset, x_min, x_max, z_min, z_max) && vertices[x + step, z].y < threshold)
            { neighbours.Add(new Vertex(x + step, z, vertices[x + step, z].y)); }
        //down
        if (CheckBounds(x, z - step, offset, x_min, x_max, z_min, z_max) && vertices[x, z - step].y < threshold)
            { neighbours.Add(new Vertex(x, z - step, vertices[x, z - step].y)); }

        //leftUp
        if (CheckBounds(x - step, z + step, offset, x_min, x_max, z_min, z_max) && vertices[x - step, z + step].y < threshold)
            { neighbours.Add(new Vertex(x - step, z + step, vertices[x - step, z + step].y)); }
        //rightUp
        if (CheckBounds(x + step, z + step, offset, x_min, x_max, z_min, z_max) && vertices[x + step, z + step].y < threshold)
            { neighbours.Add(new Vertex(x + step, z + step, vertices[x + step, z + step].y)); }
        //righDown
        if (CheckBounds(x + step, z - step, offset, x_min, x_max, z_min, z_max) && vertices[x + step, z - step].y < threshold)
            { neighbours.Add(new Vertex(x + step, z - step, vertices[x + step, z - step].y)); }
        //leftDown
        if (CheckBounds(x - step, z - step, offset, x_min, x_max, z_min, z_max) && vertices[x - step, z - step].y < threshold)
            { neighbours.Add(new Vertex(x - step, z - step, vertices[x - step, z - step].y)); }
        
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
        double lowestSum = 100;
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
                        else
                            sum += 1;
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
    /// calls PerserveMountains on whole map
    /// </summary>
    /// <param name="count"></param>
    /// <param name="radius"></param>
    public void PerserveMountains(int count, int radius, int scaleFactor)
    {
        PerserveMountains(count, radius, scaleFactor, 0, terrainSize, 0, terrainSize);
    }

    /// <summary>
    /// modifies current terrain
    /// finds 'x' highest peaks which are at least 'radius' away from each other
    /// then all vertices hight is lowered using GetScale function based on logarithm 
    /// 
    /// applied only on restricted area
    /// </summary>
    /// <param name="count"></param>
    /// <param name="radius"></param>
    public void PerserveMountains(int count, int radius, int scaleFactor,int x_min, int x_max, int z_min, int z_max)
    {
        List<Vertex> peaks = new List<Vertex>();
        for (int i = 0; i < count; i++)
        {
            if (FindNextHighestPeak(radius, peaks) != null)
                peaks.Add(FindNextHighestPeak(radius, peaks));
        }

        for (int x = x_min; x < x_max; x++)
        {
            for (int z = z_min; z < z_max; z++)
            {
                Vertex vert = new Vertex(x, z, vertices[x, z].y);
                double scale = 0;
                foreach (Vertex v in peaks)
                {
                    if (fmc.GetScale(vert, v, radius) > scale)
                    {
                        scale = fmc.GetScale(vert, v, radius);
                    }
                }
                /*
                int distance = DistanceFromLine(x, z, x_min, x_max,z_min,z_max);
                if (x < 10 && z < 10)
                    Debug.Log((float)Math.Pow(scale, scaleFactor) *((float)distance / terrainSize));*/

                //vertices[x, z].y *= (float)Math.Pow(scale, scaleFactor) *((float)distance /(terrainSize/4));
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
    

    public int DistanceFromLine(int x, int z, int x_min, int x_max, int z_min, int z_max)
    {
        return Math.Min(
            Math.Min(
                Math.Min(x, z),
                Math.Min(x_max - x, z_max - z)),
            Math.Min(
                Math.Abs(x_min - x), 
                Math.Abs(z_min - z)));
    }

    public void MedianBlur(Vertex downLeft, Vertex upRight)
    {
        float[,] depthField = new float[vertices.Length, vertices.Length];
        for(int x = 0; x < vertices.Length; x++)
        {
            for(int z = 0; z < vertices.Length; z++)
            {
                depthField[x, z] = vertices[x, z].y;
            }
        }

        for(int x = downLeft.x; x < upRight.x; x++)
        {
            for(int z = downLeft.z; z < upRight.z; z++)
            {
                vertices[x, z].y = GetMedian(x, z, 10, depthField);
            }
        }
    }


    /*
    public enum Direction
    {
        up,
        down,
        left,
        right
    }*/

    public void MirrorEdge(int patchSize, int width, Direction direction)
    {
        int line;
        if (direction == Direction.up || direction == Direction.right)
            line = terrainSize - patchSize;
        else
            line = patchSize;

        if (direction == Direction.up || direction == Direction.down)
        {
            for (int x = 0; x < terrainSize; x++)
            {
                for (int w = 0; w < width; w++)
                {
                    if (direction == Direction.up)
                    {
                        int z_orig = line - w - 1;
                        int z_new = line + w;

                        vertices[x, z_new].y =
                            ((float)(width - w) / width) * vertices[x, z_orig].y +
                            ((float)w / width) * vertices[x, z_new].y;
                    }
                    else
                    {
                        int z_orig = line + w;
                        int z_new = line - w - 1;

                        vertices[x, z_new].y =
                            ((float)(width - w) / width) * vertices[x, z_orig].y +
                            ((float)w / width) * vertices[x, z_new].y;

                    }                    
                }
            }
        }
        else
        {
            for (int z = 0; z < terrainSize; z++)
            {
                for (int w = 0; w < width; w++)
                {
                    if (direction == Direction.right)
                    {
                        int x_orig = line - w - 1;
                        int x_new = line + w;

                        vertices[x_new,z].y =
                            ((float)(width - w) / width) * vertices[x_orig,z].y +
                            ((float)w / width) * vertices[x_new,z].y;
                    }
                    else
                    {
                        int x_orig = line + w;
                        int x_new = line - w - 1;

                        vertices[x_new, z].y =
                            ((float)(width - w) / width) * vertices[x_orig,z].y +
                            ((float)w / width) * vertices[x_new,z].y;

                    }
                }
            }

        }

        rg.terrain.build();
    }

    /// <summary>
    /// DOESNT WORK WELL
    /// </summary>
    /// <param name="width"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="direction"></param>
    public void SmoothTerrainTransition(int width, Vertex start, Vertex end, Direction direction)
    {
        //Debug.Log("!");
        //Debug.Log(start);
        //Debug.Log(end);
        //Debug.Log(direction);
        int lineStart;
        int lineEnd;

        int transStart;

        int sgnDirection;

        switch (direction)
        {
            case Direction.right:
                lineStart = start.z;
                lineEnd = end.z;
                transStart = start.x; 
                sgnDirection = 1;
                break;
            case Direction.left:
                lineStart = start.z;
                lineEnd = end.z;
                transStart = start.x;
                sgnDirection = -1;
                break;
            case Direction.up:
                lineStart = start.x;
                lineEnd = end.x;
                transStart = start.z;
                sgnDirection = 1;
                break;
            case Direction.down:
                lineStart = start.x;
                lineEnd = end.x;
                transStart = start.x;
                sgnDirection = -1;
                break;
            default:
                lineStart = start.x;
                lineEnd = end.x;
                transStart = start.z;
                sgnDirection = 1;
                break;
        }
        //Debug.Log(lineStart);
        //Debug.Log(lineEnd);
        //Debug.Log(transStart);
        //Debug.Log(sgnDirection);
        for (int line = lineStart; line < lineEnd-1; line++)
        {
            float step;
            switch (direction)
            {
                case Direction.right:
                    step = (vertices[transStart, line].y + vertices[transStart + sgnDirection * width, line].y) / width;
                    break;
                case Direction.left:
                    step = (vertices[transStart, line].y + vertices[transStart + sgnDirection * width, line].y) / width;
                    break;
                case Direction.up:
                    if(line > 100 && line < 110)
                    {
                        Debug.Log(vertices[line, transStart].y);
                        Debug.Log(vertices[line, transStart + sgnDirection * width].y);
                        Debug.Log((vertices[line, transStart].y + vertices[line, transStart + sgnDirection * width].y) / width);
                    }
                    step = (vertices[line, transStart].y - vertices[line, transStart + sgnDirection * width].y) / width;
                    break;
                case Direction.down:
                    step = (vertices[line, transStart].y + vertices[line, transStart + sgnDirection * width].y) / width;
                    break;
                default:
                    step = (vertices[transStart, line].y + vertices[transStart + sgnDirection * width, line].y) / width;
                    break;
            }
            int stepCount = -1;
            for(int trans = transStart+2; trans != transStart + sgnDirection * width; trans+= sgnDirection)
            {
                stepCount++;
                int x;
                int z;

                switch (direction)
                {
                    case Direction.right:
                        x = trans;
                        z = line;
                        break;
                    case Direction.left:
                        x = trans;
                        z = line;
                        break;
                    case Direction.up:
                        x = line;
                        z = trans;
                        break;
                    case Direction.down:
                        x = line;
                        z = trans;
                        break;
                    default:
                        x = trans;
                        z = line;
                        break;
                }
                if (CheckBounds(x, z))
                {
                    vertices[x, z].y = (width - stepCount) * step;
                    //vertices[x, z].y = vertices[line, transStart + 1].y - stepCount * step;
                }
                else
                {
                    Debug.Log(x + "," + z);
                }
            }
        }
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
        return CheckBounds(x, z, offset, 0, terrainSize, 0, terrainSize);
    }

    public bool CheckBounds(int x, int z, int offset, int x_min, int x_max, int z_min, int z_max)
    {
        return x > x_min+offset && x < x_max - 1 - offset && z > z_min+offset && z < z_max - 1 - offset;
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

    public bool IsOnBorder(Vertex vert)
    {
        bool value = false;
        value =
            vert.x == 0 ||
            vert.x == terrainSize - 1 ||
            vert.z == 0 ||
            vert.z == terrainSize-1;

        return value;
    }

}
