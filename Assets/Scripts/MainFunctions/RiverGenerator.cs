﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class RiverGenerator
{


    public TerrainGenerator terrain;
    public FilterManager filtermanager;

    public Vector3[,] vertices;
    public Texture2D heightMap;
    public int terrainSize;

    public int riverWidth = 16;
    public double depth;


    ///Functions
    public FunctionRiverPlanner frp;
    public FunctionRiverDigger frd;
    public FunctionMathCalculator fmc;
    public FunctionTerrainManager ftm;
    public FunctionDebugger fd;

    //***********<RIVER...>*************
    public RiverGenerator(TerrainGenerator terrain)
    {
        this.terrain = terrain;
        filtermanager = terrain.filterManager;
        terrain.riverGenerator = this;

        vertices = terrain.vertices;
        terrainSize = terrain.terrainSize;
        heightMap = terrain.heightMap;

        frp = new FunctionRiverPlanner(this);
        frd = new FunctionRiverDigger(this);
        fmc = new FunctionMathCalculator(this);
        ftm = new FunctionTerrainManager(this);
        fd = new FunctionDebugger(this);

        AssignFunctions();
    }

    /// <summary>
    /// WARNING!!!!!!!!!!!!!!
    /// ASSING UNUSED FUNCTIONS
    /// </summary>
    void AssignFunctions()
    {
        frp.frd = frd;
        frp.fmc = fmc;
        frp.ftm = ftm;
        frp.fd = fd;

        frd.fmc = fmc;
        frd.ftm = ftm;

        ftm.fmc = fmc;

        fd.ftm = ftm;

    }

    public void GenerateRiver()
    {

        //SimpleSinusRiver();

        //SimpleSincRiver(10, 0.5f);

        //DigRiver(new Vertex(50, 50), new Vertex(100, 100), 10);

        //BestDirectionRiver();

        //ClearTerrain();
        
        List<Vertex> tempList = new List<Vertex>();
        tempList.Add(new Vertex(30, 30));
        tempList.Add(new Vertex(60,30));
        tempList.Add(new Vertex(20,70));
        tempList.Add(new Vertex(30,80));
        tempList.Add(new Vertex(40,90));
        tempList.Add(new Vertex(20,150));
        tempList.Add(new Vertex(35,140));
        tempList.Add(new Vertex(40,135));
        tempList.Add(new Vertex(55,120));

        //DigRiver3(tempList, 10, 0.4f);

        frp.FloodFromLowestPoint();
        //terrain.build();

        //Test();

        foreach (Vertex v in tempList)
        {
            //Debug.Log(v);
            //ColorPixel(v.x, v.z, 1, blueColor);
            //if (finalPath.IndexOf(v) != finalPath.Count-1)
            //DigRiver(v, finalPath[finalPath.IndexOf(v) + 1], 5, 0.2f);
        }
        //Debug.Log("-------");


    }

    
    
    
    

}
