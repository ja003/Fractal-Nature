using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class RiverGenerator  {

    
    public TerrainGenerator terrain { get; set; }
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
        vertices = terrain.vertices;
        terrainSize = terrain.terrainSize;
        heightMap = terrain.heightMap;
    }

    public void GenerateRiver()
    {
        //SimpleSinusRiver();

        BestDirectionRiver();

        
        
        
    }

    public void BestDirectionRiver()
    {
        Vertex candidateHighest = new Vertex(0, 0, 0);
        Vertex candidateLowest = new Vertex(0, 0, 1);
        Vertex candidateScndLowest = new Vertex(0, 0, 1);

        SetHighestCandidate(candidateLowest, candidateHighest);
        SetLowestCandidate(candidateLowest, candidateHighest);
        SetSecondLowestCandidate(candidateLowest, candidateScndLowest);

        Debug.Log(candidateHighest.x + "," + candidateHighest.z + "-highest=" + candidateHighest.value);
        Debug.Log(candidateLowest.x + "," + candidateLowest.z + "-lowest=" + candidateLowest.value);
        Debug.Log(candidateScndLowest.x + "," + candidateScndLowest.z + "-2-lowest=" + candidateLowest.value);


        Vector3 markColor = new Vector3(0, 0, 1);
        vertices[0, 0].y = 0.5f;
        
        
        //mark z-axis
        terrain.ColorPixels();
        
        //
        ColorPixel(candidateHighest.x, candidateHighest.z,3, blueColor);
        ColorPixel(candidateLowest.x, candidateLowest.z,3, blueColor);
        ColorPixel(candidateScndLowest.x, candidateScndLowest.z,5, pinkColor);


        ColorLine(candidateHighest, candidateLowest,3,blueColor);
        ColorLine(candidateScndLowest, candidateLowest, 3, pinkColor);

        MarkLowSpotsOnLine(candidateHighest, candidateLowest,10);
        MarkLowSpotsOnLine(candidateScndLowest, candidateLowest,10);
    }

    public void MarkLowSpotsOnLine(Vertex vert1, Vertex vert2, int density)
    {
        List<Vertex> lowVertices = new List<Vertex>();
        if(vert1.x > vert2.x)
        {
            Vertex tmp = new Vertex(vert1.x, vert1.z, vert1.value);
            vert1 = vert2;
            vert2 = tmp;
        }
        Color greenColor = new Color(0, 1, 0);
        int z;
        for(int x = vert1.x; x < vert2.x; x += ((vert2.x - vert1.x) / density)){
            z = GetZCoord(vert1, vert2, x);
            ColorPixel(x, z, 1, greenColor);
            lowVertices.Add(GetLowestPointInArea(x,z,10));
        }
        for(int i = 0; i < lowVertices.Count; i++)
        {
            //Debug.Log("vert" + i + ":" + lowVertices[i]);
            ColorPixel(lowVertices[i].x, lowVertices[i].z,3, redColor);
            if(i+1 < lowVertices.Count)
                ColorLine(lowVertices[i], lowVertices[i + 1], 2, greenColor);
        }
    }
    public Vertex GetLowestPointInArea(int _x,int _z, int area)
    {
        Vertex lowestVert = new Vertex(_x,_z,vertices[_x,_z].y);
        for (int x = _x - area; x <= _x + area; x++)
        {
            for (int z = _z - area; z <= _z + area; z++)
            {
                if(CheckBounds(x, z) && vertices[x,z].y < lowestVert.value)
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

    public int GetZCoord(Vertex vert1, Vertex vert2,int x)
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
                ColorPixel(x, y,1, redColor);
            }
        return y;
    }

    public void ColorLine(Vertex vert1, Vertex vert2,int width, Color color)
    {
        int x1 = vert1.x;
        int y1 = vert1.z;
        int x2 = vert2.x;
        int y2 = vert2.z;

        int x_i = (y1 - y2);
        int y_i = -(x1 - x2);
        int c = -(x_i*x1)-(y_i*y1);
        int y;

        int x_min;
        int x_max;
        if(vert1.x < vert2.x)
        {
            x_min = vert1.x;
            x_max = vert2.x;
        }
        else
        {
            x_min = vert2.x;
            x_max = vert1.x;            
        }

        for(int x = x_min; x < x_max- 1; x++)
        {
            if (y_i != 0)
                y = -(x_i * x + c) / y_i;
            else
                y = 0;
            if (x > 0 && x < terrainSize - 1 && y > 0 && y < terrainSize - 1)
            {
                ColorPixel(x, y,width,color);
            }
            //Debug.Log("coloring:" + x + "," + y);
        }

    }

    public void SetHighestCandidate(Vertex candidateLowest, Vertex candidateHighest)
    {
        //[x,0]
        for (int edge = 0; edge < terrainSize-1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, 0) > candidateHighest.value && candidateLowest.z != 0)
            {
                candidateHighest.Rewrite(edge, 0, SumEdgeNeighbourhood(edge, 0));
            }
        }
        //[x,end]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, terrainSize - 1) > candidateHighest.value && candidateLowest.z != terrainSize - 1)
            {
                candidateHighest.Rewrite(edge, terrainSize - 1, SumEdgeNeighbourhood(edge, terrainSize - 1));
            }
        }
        //[0,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(0, edge) > candidateHighest.value && candidateLowest.x != 0)
            {
                candidateHighest.Rewrite(0, edge, SumEdgeNeighbourhood(0, edge));
            }
        }
        //[end,z]
        for (int edge = 0; edge < terrainSize-1; edge++)
        {
            if (SumEdgeNeighbourhood(terrainSize - 1, edge) > candidateHighest.value && candidateLowest.x != terrainSize - 1)
            {
                candidateHighest.Rewrite(terrainSize - 1, edge, SumEdgeNeighbourhood(terrainSize - 1, edge));
            }
        }
    }

    public void SetLowestCandidate(Vertex candidateLowest, Vertex candidateHighest)
    {
        //[x,0]
        for (int edge = 0; edge < terrainSize-1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, 0) < candidateLowest.value && candidateHighest.z != 0)
            {
                candidateLowest.Rewrite(edge, 0, SumEdgeNeighbourhood(edge, 0));
            }
        }
        //[x,end]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, terrainSize - 1) < candidateLowest.value && candidateHighest.z != terrainSize - 1)
            {
                candidateLowest.Rewrite(edge, terrainSize - 1, SumEdgeNeighbourhood(edge, terrainSize - 1));
            }
        }
        //[0,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(0, edge) < candidateLowest.value && candidateHighest.x != 0)
            {
                candidateLowest.Rewrite(0, edge, SumEdgeNeighbourhood(0, edge));
            }
        }
        //[end,z]
        for (int edge = 0; edge < terrainSize-1; edge++)
        {
            if (SumEdgeNeighbourhood(terrainSize - 1, edge) < candidateLowest.value && candidateHighest.x != terrainSize - 1)
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
            if (SumEdgeNeighbourhood(edge, 0) < candidateScndLowest.value && candidateLowest.z != 0)
            {
                candidateScndLowest.Rewrite(edge, 0, SumEdgeNeighbourhood(edge, 0));
            }
        }
        //[x,end]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(edge, terrainSize - 1) < candidateScndLowest.value && candidateLowest.z != terrainSize - 1)
            {
                candidateScndLowest.Rewrite(edge, terrainSize - 1, SumEdgeNeighbourhood(edge, terrainSize - 1));
            }
        }
        //[0,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(0, edge) < candidateScndLowest.value && candidateLowest.x != 0)
            {
                candidateScndLowest.Rewrite(0, edge, SumEdgeNeighbourhood(0, edge));
            }
        }
        //[end,z]
        for (int edge = 0; edge < terrainSize - 1; edge++)
        {
            if (SumEdgeNeighbourhood(terrainSize - 1, edge) < candidateScndLowest.value && candidateLowest.x != terrainSize - 1)
            {
                candidateScndLowest.Rewrite(terrainSize - 1, edge, SumEdgeNeighbourhood(terrainSize - 1, edge));
            }
        }
    }


    public void ColorPixel(int x,int z, int offset, Color color)
    {
        for (int _x = x- offset; _x <= x+ offset; _x++)
        {
            for (int _z = z- offset; _z <= z+ offset; _z++)
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
        for(int _x = x- offset; _x <= x + offset; _x++)
        {
            for( int _z = z - offset; _z <= z + offset; _z++)
            {
                if (!(_x < 0 || _x > terrainSize-1 || _z < 0 || _z > terrainSize-1))
                {
                    sum += terrain.vertices[_x, _z].y;
                }
                else //avoid corners
                {
                    //sum += 0.3;
                }
            }
        }
        return sum;
    }

    public class Vertex
    {
        public int x { get; set; }
        public int z { get; set; }
        public double value { get; set; }

        public Vertex(int x, int z, double value)
        {
            this.x = x;
            this.z = z;
            this.value = value;
        }
        public void Rewrite(int x, int z, double value)
        {
            this.x = x;
            this.z = z;
            this.value = value;
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
            return x == v.x && z == v.z && value == v.value;
        }

        public override string ToString()
        {
            return "[" + x + "," + z + "]=" + value;
        }
    }



    //ALGORITHMS
    public void SimpleSinusRiver()
    {
        riverWidth = 16;
        //int position;
        int shift = 50;//cant be too high on smaller maps - its caught later in catch

        //using sinus function
        for (int x = 0; x < terrain.terrainSize; x++)
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

        for (int x = 0; x < terrain.terrainSize; x++)
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


    //***********<...RIVER>*************
}
