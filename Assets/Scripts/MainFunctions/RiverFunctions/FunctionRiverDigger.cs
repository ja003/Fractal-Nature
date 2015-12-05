using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FunctionRiverDigger {

    public RiverGenerator rg;

    public FunctionMathCalculator fmc;
    public FunctionTerrainManager ftm;


    public Vector3[,] vertices;
    public int terrainSize;

    public FunctionRiverDigger(RiverGenerator rg)
    {
        this.rg = rg;
        vertices = rg.vertices;
        terrainSize = rg.terrainSize;
    }

    public void DistortPath(List<Vertex> path, int maxDistort)
    {
        System.Random rnd = new System.Random();
        foreach (Vertex v in path)
        {
            int distortX = rnd.Next(-maxDistort, maxDistort);
            int distortZ = rnd.Next(-maxDistort, maxDistort);
            v.Rewrite(v.x + distortX, v.z + distortZ, v.height);
        }
    }

    public void DigRiver3(List<Vertex> path, int width, float depthFactor)
    {
        float[,] depthField = new float[terrainSize, terrainSize]; //depth to dig
        float[,] distField = new float[terrainSize, terrainSize]; //distance from line
        float[,] pathMark = new float[terrainSize, terrainSize]; //path number which will effect the vertex

        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {
                depthField[x, z] = 666;//mark
                distField[x, z] = 666;//mark
            }
        }
        int widthFactor = 2;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vertex v1 = path[i];
            Vertex v2 = path[i + 1];

            Vertex topLeftCorner = new Vertex(0, 0);
            Vertex botRightCorner = new Vertex(0, 0);
            //evaluate position of starting vertex
            if (v1.x < v2.x)
            {
                topLeftCorner.x = v1.x - width * widthFactor;
                botRightCorner.x = v2.x + width * widthFactor;
            }
            else
            {
                topLeftCorner.x = v2.x - width * widthFactor;
                botRightCorner.x = v1.x + width * widthFactor;
            }
            if (v1.z < v2.z)
            {
                topLeftCorner.z = v2.z + width * widthFactor;
                botRightCorner.z = v1.z - width * widthFactor;
            }
            else
            {
                topLeftCorner.z = v1.z + width * widthFactor;
                botRightCorner.z = v2.z - width * widthFactor;
            }

            if (topLeftCorner.Equals(botRightCorner))
            {
                Debug.Log("same vertices");
                break;
            }

            for (int x = topLeftCorner.x; x < botRightCorner.x; x++)
            {
                for (int z = topLeftCorner.z; z > botRightCorner.z; z--)
                {

                    if (ftm.CheckBounds(x, z) && fmc.BelongsToPath(x, z, v1, v2, width * widthFactor))
                    {
                        Vertex vert = new Vertex(x, z);
                        float distance = fmc.GetDistanceFromLine(vert, v1, v2);
                        if (v2.x == 30 && v2.z == 80)
                        {
                            // Debug.Log(x+","+z+":"+distance);
                        }

                        float depth = 0;

                        if (distance == 0) //sinc is not defined at 0
                            distance += 0.01f;
                        depth = MySinc(distance, width, depthFactor);


                        if (depthField[vert.x, vert.z] == 666) //hasnt been modified yet
                        {
                            depthField[vert.x, vert.z] = depth;
                            distField[vert.x, vert.z] = distance;
                            pathMark[vert.x, vert.z] = 1;//path
                        }
                        else if (depthField[vert.x, vert.z] != 666) //has been modified but I can dig it
                        {
                            if (distance < distField[vert.x, vert.z])
                            {
                                //depthField[vert.x, vert.z] = Math.Min(depthField[vert.x, vert.z], depth);
                                depthField[vert.x, vert.z] = depth;
                                distField[vert.x, vert.z] = distance;
                            }
                            //depthField[vert.x, vert.z] = (depthField[vert.x, vert.z] + depth)/2;
                        }

                    }
                }
            }

        }


        //fix corners

        for (int i = 0; i < path.Count; i++)
        {
            Vertex corner = path[i];
            //Debug.Log(corner);
            for (int x = corner.x - widthFactor * 2 * width; x < corner.x + widthFactor * width; x++)
            {
                for (int z = corner.z - widthFactor * 2 * width; z < corner.z + widthFactor * width; z++)
                {
                    if (ftm.CheckBounds(x, z))
                    {
                        float distance = fmc.GetDistance(corner, new Vertex(x, z));
                        //if (i == 10)
                        //Debug.Log(i+":"+distance);

                        if (distance < widthFactor * 2 * width)
                        {
                            float depth = 0;

                            if (distance == 0) //sinc is not defined at 0
                                distance += 0.01f;

                            depth = MySinc(distance, width, depthFactor);


                            if (depthField[x, z] == 666) //hasnt been modified yet
                            {
                                depthField[x, z] = depth;
                                //rg.ColorPixel(x, z, 1, greenColor);
                                pathMark[x, z] = 2;//corner
                                distField[x, z] = distance;
                                //Debug.Log(depth);
                            }
                            else if (depthField[x, z] != 666) //has been modified but maybe badly
                            {
                                if (distance < distField[x, z])
                                {
                                    depthField[x, z] = depth;
                                    distField[x, z] = distance;
                                }
                                //depthField[x, z] = Math.Min(depthField[x, z], depth);
                                //depthField[x, z] = (depthField[x, z] + depth)/2; //blbost
                                //ColorPixel(x, z, 1,redColor);
                                //Debug.Log(x + "," + z);

                            }

                        }
                        else
                        {
                            //ColorPixel(x, z, 0, greenColor);
                        }
                    }
                }

            }
        }


        //apply digging
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {
                if (depthField[x, z] != 666)
                {
                    vertices[x, z].y += depthField[x, z] * depthFactor;

                }
            }
        }

        rg.terrain.build();


        //ColorPixel(20, 20, 0, greenColor);
        //color digging

        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {
                if (pathMark[x, z] == 1)
                {
                    //ColorPixel(x, z, 0, redColor);
                }
                else if (pathMark[x, z] == 2)
                {
                    //ColorPixel(x, z, 0, greenColor);
                }
            }
        }


    }

    public float MySinc(float x, float width, float depthFactor)
    {
        return (float)(-depthFactor * Math.Sin((x / (width / Math.PI))) / (x / Math.PI));
    }

}
