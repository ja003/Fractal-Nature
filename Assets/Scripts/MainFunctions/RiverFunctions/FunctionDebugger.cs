using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FunctionDebugger{

    public RiverGenerator rg;


    public Color redColor = new Color(1, 0, 0);
    public Color greenColor = new Color(0, 1, 0);
    public Color blueColor = new Color(0, 0, 1);
    public Color pinkColor = new Color(1, 0, 1);

    public FunctionTerrainManager ftm;

    public Vector3[,] vertices;
    public int terrainSize;


    public FunctionDebugger(RiverGenerator rg)
    {
        this.rg = rg;
        vertices = rg.vertices;
        terrainSize = rg.terrainSize;
    }

    public void ColorPixel(int x, int z, int offset, Color color)
    {
        for (int _x = x - offset; _x <= x + offset; _x++)
        {
            for (int _z = z - offset; _z <= z + offset; _z++)
            {
                if (ftm.CheckBounds(x, z))
                    rg.heightMap.SetPixel(_x, _z, color);
            }
        }

        rg.heightMap.Apply();
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



}
