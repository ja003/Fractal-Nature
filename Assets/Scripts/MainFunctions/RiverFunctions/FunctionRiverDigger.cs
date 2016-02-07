using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FunctionRiverDigger {

    public RiverGenerator rg;

    public FunctionMathCalculator fmc;
    public FunctionTerrainManager ftm;
    public FunctionDebugger fd;


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

    public List<float> AssignWidthToPoints(List<Vertex> path, float minWidth)
    {
        List<float> sumOfPointNeighb = new List<float>();
        float totalSum = 0;
        float maxSum = 0;
        foreach (Vertex v in path)
        {
            sumOfPointNeighb.Add(ftm.GetSumFromNeighbourhood(v.x, v.z, 2 * (int)minWidth));
            totalSum += sumOfPointNeighb[path.IndexOf(v)]; 
            if(sumOfPointNeighb[path.IndexOf(v)] > maxSum){
                maxSum = sumOfPointNeighb[path.IndexOf(v)];
            }           
        }
        float averageSum = totalSum / sumOfPointNeighb.Count;

        List<float> finalWidthValues = new List<float>();
        //Debug.Log("-------------");
        foreach (float f in sumOfPointNeighb)
        {
            float value = minWidth + minWidth/2 * (maxSum - f) / maxSum;
            finalWidthValues.Add(value);
            //Debug.Log(value);
            //finalWidthValues.Add(minWidth);
        }

        return finalWidthValues;
    }

    

    public float GetLocalWidth(Vertex point, Vertex v1, Vertex v2,float w1,float w2)
    {
        //project point on line v1-v2
        Vertex projectedP = fmc.ProjectPointOnLine(point, fmc.GetGeneralLineEquation(v1, v2));
        //measure distance to v1 and v2
        float dist1 = fmc.GetDistanceBetweenPoints(point, v1);
        float dist2 = fmc.GetDistanceBetweenPoints(point, v2);
        //Debug.Log(dist1);
        //Debug.Log(dist2);
        //map values to give total sum of 1
        float factor = dist1 + dist2;
        float factor1 = (factor - dist1) / factor;
        float factor2 = (factor - dist2) / factor;
        //Debug.Log(factor);
        //Debug.Log(factor1);
        //Debug.Log(factor2);

        //return sum of given widths multiplied by calculated factors

        return w1 * factor1 + w2*factor2;
    }

    public void DigRiver(List<Vertex> path, int width, float depthFactor)
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

        //should be min 2
        int areaFactor = 2;

        List<float> widthInPoints = AssignWidthToPoints(path, width);
        

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vertex v1 = path[i];
            Vertex v2 = path[i + 1];
            float width1 = widthInPoints[i];
            float width2 = widthInPoints[i+1];
            

            Vertex topLeftCorner = new Vertex(0, 0);
            Vertex botRightCorner = new Vertex(0, 0);
            //evaluate position of affected area
            if (v1.x < v2.x)
            {
                topLeftCorner.x = (int)(v1.x - width1 * areaFactor);
                botRightCorner.x = (int)(v2.x + width2 * areaFactor);
            }
            else
            {
                topLeftCorner.x = (int)(v2.x - width2 * areaFactor);
                botRightCorner.x = (int)(v1.x + width1 * areaFactor);
            }
            if (v1.z < v2.z)
            {
                topLeftCorner.z = (int)(v2.z + width2 * areaFactor);
                botRightCorner.z = (int)(v1.z - width1 * areaFactor);
            }
            else
            {
                topLeftCorner.z = (int)(v1.z + width1 * areaFactor);
                botRightCorner.z = (int)(v2.z - width2 * areaFactor);
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

                    if (ftm.CheckBounds(x, z) && fmc.BelongsToPath(x, z, v1, v2, 2*width * areaFactor))
                    {
                        bool valid = true;
                        Vertex vert = new Vertex(x, z);
                        float distance = fmc.GetDistanceFromLine(vert, v1, v2);

                        float depth = 0;

                        if (distance == 0) //sinc is not defined at 0
                            distance += 0.01f;

                        //determine width (interpolation between points)
                        float localWidth = GetLocalWidth(vert, v1, v2, width1, width2);
                        //float localWidth = 1;
                        if(localWidth < width)
                        {
                            Debug.Log("!");
                        }
                        
                        if(distance > areaFactor * localWidth)
                        {
                            valid = false;
                        }

                        depth = MySinc(distance, localWidth, depthFactor);


                        if (valid && depthField[vert.x, vert.z] == 666) //hasnt been modified yet
                        {
                            depthField[vert.x, vert.z] = depth;
                            distField[vert.x, vert.z] = distance;
                            pathMark[vert.x, vert.z] = 1;//path
                            //widthFactor += 0.0003f;
                        }
                        else if (valid && depthField[vert.x, vert.z] != 666) //has been modified but I can dig it better (valid shouldnt be neccessary)
                        {
                            if (distance < distField[vert.x, vert.z])
                            {
                                //depthField[vert.x, vert.z] = Math.Min(depthField[vert.x, vert.z], depth);
                                depthField[vert.x, vert.z] = depth;
                                distField[vert.x, vert.z] = distance;
                                //widthFactor += 0.0003f;
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
            for (int x = corner.x - 2 * areaFactor * width; x < corner.x + 2 * areaFactor * width; x++)
            {
                for (int z = corner.z - 2 * areaFactor * width; z < corner.z + 2 * areaFactor * width; z++)
                {
                    if (ftm.CheckBounds(x, z))
                    {
                        float distance = fmc.GetDistance(corner, new Vertex(x, z));
                        //if (i == 10)
                        //Debug.Log(i+":"+distance);

                        if (distance < areaFactor * widthInPoints[i])
                        {
                            float depth = 0;

                            if (distance == 0) //sinc is not defined at 0
                                distance += 0.01f;

                            depth = MySinc(distance, widthInPoints[i], depthFactor);


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
                                    pathMark[x, z] = 2;
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


        //filter result
        float[,] filtField = new float[terrainSize, terrainSize];
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {
                if (depthField[x, z] != 666)
                {
                    filtField[x, z] += ftm.GetMedian(x,z,2, depthField);
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
                    //vertices[x, z].y += depthField[x, z] * depthFactor;
                    vertices[x, z].y += filtField[x, z] * depthFactor;
                }
            }
        }

        rg.terrain.build();


        //ColorPixel(20, 20, 0, greenColor);
        //color digging

        //pathmark:
        //  1 = river
        //  2 = corner
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {
                if (pathMark[x, z] == 1)
                {
                    //fd.ColorPixel(x, z, 0, fd.redColor);
                }
                else if (pathMark[x, z] == 2)
                {
                    //fd.ColorPixel(x, z, 0, fd.greenColor);
                }
            }
        }


    }

    public float MySinc(float x, float width, float depth)
    {
        double r = -(depth/ Math.PI) * Math.Sin(x * Math.PI / width) / x * width;
        return (float)r;
        /*
        return (float)(-
            (depth/Math.PI)
            *Math.Sin((x / (width* widthFactor / Math.PI)))
            /(x / Math.PI))
            *(float)(width* widthFactor / Math.PI);
            */
    }

}
