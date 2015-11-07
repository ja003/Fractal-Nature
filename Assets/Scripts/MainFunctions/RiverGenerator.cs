﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class RiverGenerator {


    public TerrainGenerator terrain;
    public FilterManager filtermanager;

    public Vector3[,] vertices;
    public Texture2D heightMap;
    int terrainSize;

    public int riverWidth = 16;
    public double depth;

    public Color redColor = new Color(1, 0, 0);
    public Color greenColor = new Color(0, 1, 0);
    public Color blueColor = new Color(0, 0, 1);
    public Color pinkColor = new Color(1, 0, 1);

    //***********<RIVER...>*************
    public RiverGenerator(TerrainGenerator terrain)
    {
        this.terrain = terrain;
        filtermanager = terrain.filterManager;
        terrain.riverGenerator = this;

        vertices = terrain.vertices;
        terrainSize = terrain.terrainSize;
        heightMap = terrain.heightMap;
    }

    public void GenerateRiver()
    {

        //SimpleSinusRiver();


        //DigRiver(new Vertex(50, 50), new Vertex(100, 100), 10);

        //BestDirectionRiver();
        terrain.build();
        FloodFromLowestPoint();

        //Test();

        
    }

    public class FloodNode
    {
        public Vertex vertex;
        public int parentIndex;

        public FloodNode(Vertex vertex, int parentIndex)
        {
            this.vertex = vertex;
            this.parentIndex = parentIndex;
        }

        public override string ToString()
        {
            return vertex + "[" + parentIndex + "]";
        }

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            FloodNode fn = obj as FloodNode;
            if ((System.Object)fn == null)
            {
                return false;
            }

            // Return true if the fields match:
            return vertex.Equals(fn.vertex);
        }
    }


    public void Test()
    {

        //FloodNode LIST//
        List<FloodNode> fnl = new List<FloodNode>();

        fnl.Add(new FloodNode(new Vertex(0, 0), 0));
        Debug.Log(fnl.Contains(new FloodNode(new Vertex(0, 0), 0))); //false

        FloodNode fn = new FloodNode(new Vertex(0, 0), 0);
        fnl.Add(fn);        
        Debug.Log(fnl.Contains(fn));    //true

        //Vertex LIST//
        List<Vertex> vl = new List<Vertex>();

        vl.Add(new Vertex(0, 0));
        Debug.Log(vl.Contains(new Vertex(0, 0))); //true

    }

    public void FloodFromLowestPoint()
    {
        //find vertex with lowest neigbourhood
        //add some offset from borders
        Vertex start = GetLowestRegionCenter(10, 20);
        Debug.Log(start);
        ColorPixel(start.x, start.z, 5, redColor);


        //start = new Vertex(10, 10);

        //flood algorithm
        float step = Math.Abs(vertices[start.x, start.z].y); //height can be negative
        if (step == 0)
            step = 0.01f;

        bool reachLeft = false;
        bool reachTop = false;
        bool reachRight = false;
        bool reachBot = false;

        List<FloodNode> reachableNodes = new List<FloodNode>();
        reachableNodes.Add(new FloodNode(start, 0));
        float threshold = step;
        List<int> endIndex = new List<int>();

        int gridStep = 5;
        int borderOffset = gridStep * 2;

        //search until process reaches 2 sides
        while (Convert.ToInt32(reachLeft) + Convert.ToInt32(reachTop) + Convert.ToInt32(reachRight) + Convert.ToInt32(reachBot) < 2)
        {
            for(int i = 0; i < reachableNodes.Count; i++)
            {
                FloodNode currentNode = reachableNodes[i];
                int x = currentNode.vertex.x;
                int z = currentNode.vertex.z;

                //record index of node which is close to the border
                //ignore the corners
                if (!reachLeft && IsCloseTo(x, 0, borderOffset) && !IsCloseTo(z, 0, borderOffset) && !IsCloseTo(z, terrainSize, borderOffset))
                    { reachLeft = true; endIndex.Add(i); }
                if (!reachRight && IsCloseTo(x, terrainSize, borderOffset) && !IsCloseTo(z, 0, borderOffset) && !IsCloseTo(z, terrainSize, borderOffset))
                    { reachRight = true; endIndex.Add(i); }
                if (!reachBot && IsCloseTo(z, 0, borderOffset) && !IsCloseTo(x, 0, borderOffset) && !IsCloseTo(x, terrainSize, borderOffset))
                    { reachBot = true; endIndex.Add(i); }
                if (!reachTop && IsCloseTo(z, terrainSize, borderOffset) && !IsCloseTo(x, 0, borderOffset) && !IsCloseTo(x, terrainSize, borderOffset))
                    { reachTop = true; endIndex.Add(i); }

                if (endIndex.Count == 2)
                    break;

                if(i > terrainSize * terrainSize)
                {
                    Debug.Log("!");
                    endIndex.Add(0);
                    endIndex.Add(0);
                    reachLeft = true;
                    reachRight = true;
                    reachBot = true;
                    reachTop = true;
                    break;
                }

                List<Vertex> neighbours = GetNeighbours(currentNode.vertex, gridStep);
                foreach(Vertex v in neighbours)
                {
                    if(v.height < threshold && !reachableNodes.Contains(new FloodNode(v, i)))
                    {
                        reachableNodes.Add(new FloodNode(v, i));
                    }
                }
            }                    
            threshold += step;
        }

        List<Vertex> path1 = new List<Vertex>();
        int index1 = endIndex[0];
        while(index1 != 0)
        {
            path1.Add(reachableNodes[index1].vertex);
            index1 = reachableNodes[index1].parentIndex;
        }
        List<Vertex> path2 = new List<Vertex>();
        int index2 = endIndex[1];
        while (index2 > 0) //dont add start node to this path (it is already in path1)
        {
            path2.Add(reachableNodes[index2].vertex);
            index2 = reachableNodes[index2].parentIndex;
        }

        //now we reverse path2 and connect it with path1
        path2.Reverse();

        List<Vertex> finalPath = new List<Vertex>();

        foreach(Vertex v in path1)
        {
            finalPath.Add(v);
        }
        foreach (Vertex v in path2)
        {
            finalPath.Add(v);
        }

        foreach(Vertex v in finalPath)
        {
            //Debug.Log(v);
            ColorPixel(v.x, v.z, 5, blueColor);
        }
    }

    public bool IsCloseTo(int value, int border, int offset)
    {
        return border - offset < value && value < border + offset;
    }

    public List<Vertex> GetNeighbours(Vertex center, int step)
    {
        List<Vertex> neighbours = new List<Vertex>();
        int x = center.x;
        int z = center.z;
        //left
        if (CheckBounds(x- step, z)) { neighbours.Add(new Vertex(x - step, z, vertices[x- step, z].y)); }
        //up
        if (CheckBounds(x, z+ step)) { neighbours.Add(new Vertex(x, z+ step, vertices[x, z+ step].y)); }
        //righ
        if (CheckBounds(x + step, z)) { neighbours.Add(new Vertex(x + step, z, vertices[x+ step, z].y)); }
        //down
        if (CheckBounds(x, z- step)) { neighbours.Add(new Vertex(x, z- step, vertices[x, z- step].y)); }

        return neighbours;
    }

    public Vertex GetLowestRegionCenter(int radius, int offset)
    {
        double lowestSum = 10;
        Vertex lowestRegionCenter = new Vertex(offset, offset);
        for(int x = offset; x < terrainSize- offset; x++)
        {
            for (int z = offset; z < terrainSize- offset; z++)
            {
                double sum = 0;
                for(int i = x-radius;i < x + radius; i++)
                {
                    for (int j = z - radius; j < z + radius; j++)
                    {
                        if(CheckBounds(i, j))
                            sum += vertices[i, j].y;
                    }
                }
                if (sum < lowestSum)
                {
                    lowestSum = sum;
                    lowestRegionCenter.Rewrite(x,z,vertices[x,z].y);
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
        for(int i = 0; i < count; i++)
        {
            if(FindNextHighestPeak(radius, peaks) != null)
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
                    if (IsInRange(vert, v, radius))
                    {
                        isInRange = true;
                    }
                    if(GetScale(vert,v,radius) > scale)
                    {
                        scale = GetScale(vert, v, radius);
                    }
                }
 
                vertices[x, z].y *= (float)Math.Pow(scale,scaleFactor);
                
            }
        }



        //blur the peaks
        float blurringFactor = radius / 10;
        int kernelSize = radius / 10;
        
        for (int i = 0; i < peaks.Count; i++)
        {
            filtermanager.applyGaussianBlur(blurringFactor, kernelSize, 
                new Vector3(peaks[i].x - kernelSize, 0, peaks[i].z- kernelSize), 
                new Vector3(peaks[i].x + kernelSize, 0, peaks[i].z+ kernelSize));
            
        }
        
        terrain.build();
    }
    
    public void DigRiver(Vertex v1, Vertex v2, float width)
    {
        //general line equation parameters
        int a = v1.z - v2.z;
        int b = -(v1.x - v2.x);
        int c = -(a * v1.x) - (b * v1.z);

        //set bounds of region to dig
        int min_x;
        int max_x;
        if (v1.x < v2.x)
        {
            min_x = v1.x - (int)width;
            max_x = v2.x + (int)width;
        }
        else
        {
            min_x = v2.x - (int)width;
            max_x = v1.x + (int)width;
        }

        int min_z;
        int max_z;
        if (v1.z < v2.z)
        {
            min_z = v1.z - (int)width;
            max_z = v2.z + (int)width;
        }
        else
        {
            min_z = v2.z - (int)width;
            max_z = v1.z + (int)width;
        }
        
        for(int x = min_x; x < max_x; x++)
        {
            for(int z = min_z; z < max_z; z++)
            {
                if (CheckBounds(x, z))
                {
                    double depth = GetDistanceFromLine(new Vertex(x, z), a, b, c);

                    if (-width < depth && depth < width)
                    {
                        depth = Math.Sin((depth - width) * (Math.PI / (2 * width)));
                        vertices[x, z].y += (float)depth / width;
                    }
                }
            }
        }



    }

    /// <summary>
    /// general line equation: ax + by + c = 0
    /// distance = |a*x_0 + b*y_0 + c| / sqrt(a^2 + b^2)
    /// </summary>
    /// <param name="point"></param>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <returns></returns>
    public double GetDistanceFromLine(Vertex point, int a, int b, int c)
    {
        return Math.Abs(a*point.x+b*point.z+ c)/(Math.Sqrt(a*a+b* b));
    }



    public void BestDirectionRiver()
    {
        Vertex candidateHighest = new Vertex(0, 0, 0);
        Vertex candidateLowest = new Vertex(0, 0, 1);
        Vertex candidateScndLowest = new Vertex(0, 0, 1);

        SetHighestCandidate(candidateLowest, candidateHighest);
        SetLowestCandidate(candidateLowest, candidateHighest);
        SetSecondLowestCandidate(candidateLowest, candidateScndLowest);

        //Debug.Log(candidateHighest.x + "," + candidateHighest.z + "-highest=" + candidateHighest.value);
        //Debug.Log(candidateLowest.x + "," + candidateLowest.z + "-lowest=" + candidateLowest.value);
        //Debug.Log(candidateScndLowest.x + "," + candidateScndLowest.z + "-2-lowest=" + candidateLowest.value);

        
        terrain.vertices[0, 0].y = 0.5f;
        
        
        //mark z-axis
        terrain.ColorPixels();
        


        //List<Vertex> straightLine = ColorLine(candidateHighest, candidateLowest, 3, blueColor);
        //MarkLowSpotsOnLine(candidateHighest, candidateLowest, 10, greenColor);
        


        ColorLine(candidateScndLowest, candidateLowest, 3, pinkColor);
        MarkLowSpotsOnLine(candidateScndLowest, candidateLowest, 10, redColor);

    }
    
    public void SimpleSinusRiver()
    {
        riverWidth = 16;
        //int position;
        int shift = 50;//cant be too high on smaller maps - its caught later in catch

        //using sinus function
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = terrainSize / 2 - riverWidth - shift; z < terrainSize / 2 + riverWidth - shift; z++)
            {
                //position = terrainSize / 2 - riverWidth - shift - z;

                //use sinus function with period = 2*riverWidth, shifted to the required position
                depth = -Math.Sin((z - (terrainSize / 2 + riverWidth - shift)) * (Math.PI / (2 * riverWidth)));
                //if (x == 0)
                //Debug.Log("depth=" + depth);
                try
                {
                    vertices[x, z].y -= (float)depth / 2;
                }
                catch (IndexOutOfRangeException ex)
                {
                    //Debug.Log("x=" + x + ",z=" + z);
                }
            }

        }
    }

    public void SimpleRiver()
    {
        riverWidth = 16;

        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = terrainSize / 2 - riverWidth; z < terrainSize / 2 + riverWidth; z++)
            {
                //float depth = (float)System.Math.Abs((float)riverWidth - (float)System.Math.Abs(terrainSize / 2 - z)) / riverWidth;
                float depth = (terrainSize / 2) - (terrainSize / 2 - riverWidth) % (terrainSize / 2);
                depth *= vertices[x, z].y;
                if (depth > vertices[x, z].y)
                    depth = vertices[x, z].y;
                //Debug.Log("["+x+","+z+"]" + "=" + vertices[x,z].y + "  => "+depth);
                if (depth < 0)
                    depth *= -1;
                vertices[x, z].y -= depth;
            }
        }
    }





    //***********HELPER FUNCTIONS****************
    public bool IsInArea(int x, int z, int xStart, int xEnd, int zStart, int zEnd)
    {
        return (x > xStart && x < xEnd && z > zStart && z < zEnd);
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
                        if (IsInRange(new Vertex(x, z, vertices[x, z].y), v, radius * 2))
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

    public double GetScale(Vertex v1, Vertex v2, int radius)
    {
        return Math.Log(terrainSize - GetDistance(v1, v2), terrainSize);
    }

    public double GetDistance(Vertex v1, Vertex v2)
    {
        return Math.Sqrt((v1.x - v2.x) * (v1.x - v2.x) + (v1.z - v2.z) * (v1.z - v2.z));
    }

    public bool IsInRange(Vertex vert, Vertex center, int radius)
    {
        if (GetDistance(vert, center) < radius)
            return true;
        return false;
    }

    public void MarkLowSpotsOnLine(Vertex vert1, Vertex vert2, int density, Color color)
    {
        List<Vertex> lowVertices = new List<Vertex>();
        if (vert1.x > vert2.x)
        {
            Vertex tmp = new Vertex(vert1.x, vert1.z, vert1.height);
            vert1 = vert2;
            vert2 = tmp;
        }

        int z;
        if (((vert2.x - vert1.x) / density) > 0)
        {
            for (int x = vert1.x; x < vert2.x; x += ((vert2.x - vert1.x) / density))
            {
                z = GetZCoord(vert1, vert2, x);
                ColorPixel(x, z, 1, color);
                lowVertices.Add(GetLowestPointInArea(x, z, 10));
            }
        }
        for (int i = 0; i < lowVertices.Count; i++)
        {
            //Debug.Log("vert" + i + ":" + lowVertices[i]);
            ColorPixel(lowVertices[i].x, lowVertices[i].z, 3, color);
            if (i + 1 < lowVertices.Count)
            {
                ColorLine(lowVertices[i], lowVertices[i + 1], 2, color);
                DigRiver(lowVertices[i], lowVertices[i + 1], 10);
            }
        }
    }


    public class Vertex
    {
        public int x { get; set; }
        public int z { get; set; }
        public double height { get; set; }

        public Vertex(int x, int z, double value)
        {
            this.x = x;
            this.z = z;
            this.height = value;
        }
        public Vertex(int x, int z)
        {
            this.x = x;
            this.z = z;
            this.height = 0;
        }
        public void Rewrite(int x, int z, double value)
        {
            this.x = x;
            this.z = z;
            this.height = value;
        }
        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            // If parameter cannot be cast to Point return false.
            Vertex v = obj as Vertex;
            if ((System.Object)v == null)
            {
                return false;
            }

            // Return true if the fields match:
            return x == v.x && z == v.z && height == v.height;
        }

        /* not sure if good/necessary
        public override int GetHashCode()
        {
            return x * 98411 + z * 98507;
        }*/

        public override string ToString()
        {
            return "[" + x + "," + z + "]=" + height;
        }
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

    public bool CheckBounds(int x, int z)
    {
        return x > 0 && x < terrainSize - 1 && z > 0 && z < terrainSize - 1;
    }

    public bool CheckBounds(Vertex vertex)
    {
        return CheckBounds(vertex.x, vertex.z);
    }

    public int GetZCoord(Vertex vert1, Vertex vert2, int x)
    {
        Color redColor = new Color(1, 0, 0);
        int x1 = vert1.x;
        int y1 = vert1.z;
        int x2 = vert2.x;
        int y2 = vert2.z;

        int x_i = (y1 - y2);
        int y_i = -(x1 - x2);
        int c = -(x_i * x1) - (y_i * y1);
        int y;
        y = -(x_i * x + c) / y_i;
        if (x > 0 && x < terrainSize - 1 && y > 0 && y < terrainSize - 1)
        {
            ColorPixel(x, y, 1, redColor);
        }
        return y;
    }

    public List<Vertex> ColorLine(Vertex vert1, Vertex vert2, int width, Color color)
    {
        List<Vertex> nodes = new List<Vertex>();

        int x1 = vert1.x;
        int z1 = vert1.z;
        int x2 = vert2.x;
        int z2 = vert2.z;

        int x_i = (z1 - z2);
        int z_i = -(x1 - x2);
        int c = -(x_i * x1) - (z_i * z1);
        int z;

        int x_min;
        int x_max;
        if (vert1.x < vert2.x)
        {
            x_min = vert1.x;
            x_max = vert2.x;
        }
        else
        {
            x_min = vert2.x;
            x_max = vert1.x;
        }

        for (int x = x_min; x < x_max - 1; x++)
        {
            if (z_i != 0)
                z = -(x_i * x + c) / z_i;
            else
                z = 0;
            if (x > 0 && x < terrainSize - 1 && z > 0 && z < terrainSize - 1)
            {
                ColorPixel(x, z, width, color);
                nodes.Add(new Vertex(x, z, vertices[x, z].y));
            }
            //Debug.Log("coloring:" + x + "," + y);
        }
        return nodes;

    }

    public void SetHighestCandidate(Vertex candidateLowest, Vertex candidateHighest)
    {
        //[x,0]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, 0) > candidateHighest.height && candidateLowest.z != 0)
            {
                candidateHighest.Rewrite(edge, 0, SumEdgeNeighbourhood(edge, 0));
            }
        }
        //[x,end]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, terrainSize - 1) > candidateHighest.height && candidateLowest.z != terrainSize - 1)
            {
                candidateHighest.Rewrite(edge, terrainSize - 1, SumEdgeNeighbourhood(edge, terrainSize - 1));
            }
        }
        //[0,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(0, edge) > candidateHighest.height && candidateLowest.x != 0)
            {
                candidateHighest.Rewrite(0, edge, SumEdgeNeighbourhood(0, edge));
            }
        }
        //[end,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(terrainSize - 1, edge) > candidateHighest.height && candidateLowest.x != terrainSize - 1)
            {
                candidateHighest.Rewrite(terrainSize - 1, edge, SumEdgeNeighbourhood(terrainSize - 1, edge));
            }
        }
    }

    public void SetLowestCandidate(Vertex candidateLowest, Vertex candidateHighest)
    {
        //[x,0]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, 0) < candidateLowest.height && candidateHighest.z != 0)
            {
                candidateLowest.Rewrite(edge, 0, SumEdgeNeighbourhood(edge, 0));
            }
        }
        //[x,end]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, terrainSize - 1) < candidateLowest.height && candidateHighest.z != terrainSize - 1)
            {
                candidateLowest.Rewrite(edge, terrainSize - 1, SumEdgeNeighbourhood(edge, terrainSize - 1));
            }
        }
        //[0,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(0, edge) < candidateLowest.height && candidateHighest.x != 0)
            {
                candidateLowest.Rewrite(0, edge, SumEdgeNeighbourhood(0, edge));
            }
        }
        //[end,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(terrainSize - 1, edge) < candidateLowest.height && candidateHighest.x != terrainSize - 1)
            {
                candidateLowest.Rewrite(terrainSize - 1, edge, SumEdgeNeighbourhood(terrainSize - 1, edge));
            }
        }
    }

    public void SetSecondLowestCandidate(Vertex candidateLowest, Vertex candidateScndLowest)
    {
        //[x,0]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, 0) < candidateScndLowest.height && candidateLowest.z != 0)
            {
                candidateScndLowest.Rewrite(edge, 0, SumEdgeNeighbourhood(edge, 0));
            }
        }
        //[x,end]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, terrainSize - 1) < candidateScndLowest.height && candidateLowest.z != terrainSize - 1)
            {
                candidateScndLowest.Rewrite(edge, terrainSize - 1, SumEdgeNeighbourhood(edge, terrainSize - 1));
            }
        }
        //[0,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(0, edge) < candidateScndLowest.height && candidateLowest.x != 0)
            {
                candidateScndLowest.Rewrite(0, edge, SumEdgeNeighbourhood(0, edge));
            }
        }
        //[end,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(terrainSize - 1, edge) < candidateScndLowest.height && candidateLowest.x != terrainSize - 1)
            {
                candidateScndLowest.Rewrite(terrainSize - 1, edge, SumEdgeNeighbourhood(terrainSize - 1, edge));
            }
        }
    }


    public void ColorPixel(int x, int z, int offset, Color color)
    {
        for (int _x = x - offset; _x <= x + offset; _x++)
        {
            for (int _z = z - offset; _z <= z + offset; _z++)
            {
                if (CheckBounds(x, z))
                    heightMap.SetPixel(_x, _z, color);
            }
        }

        heightMap.Apply();
    }

    public double SumEdgeNeighbourhood(int x, int z)
    {
        double sum = 0;
        int offset = 3;
        for (int _x = x - offset; _x <= x + offset; _x++)
        {
            for (int _z = z - offset; _z <= z + offset; _z++)
            {
                if (!(_x < 0 || _x > terrainSize - 1 || _z < 0 || _z > terrainSize - 1))
                {
                    sum += vertices[_x, _z].y;
                }
                else //avoid corners
                {
                    //sum += 0.3;
                }
            }
        }
        return sum;
    }


}