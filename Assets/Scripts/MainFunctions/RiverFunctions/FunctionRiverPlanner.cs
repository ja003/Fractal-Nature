using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class FunctionRiverPlanner  {

    public RiverGenerator rg;
    public FunctionRiverDigger frd;
    public FunctionMathCalculator fmc;
    public FunctionTerrainManager ftm;
    public FunctionDebugger fd;
    public RiverInfo currentRiver;


    public Vector3[,] vertices;
    public int terrainSize;

    public Color redColor = new Color(1, 0, 0);
    public Color greenColor = new Color(0, 1, 0);
    public Color blueColor = new Color(0, 0, 1);
    public Color pinkColor = new Color(1, 0, 1);

    public FunctionRiverPlanner(RiverGenerator rg)
    {
        this.rg = rg;
        vertices = rg.vertices;
        terrainSize = rg.terrainSize;
        
    }


    public void FloodFromLowestPoint()
    {
        Vertex start = ftm.GetLowestRegionCenter(10, 20);
        FloodFromPoint(start,0,terrainSize-1,0,terrainSize-1, false,false,false,false,Direction.none);
    }

    public void FloodFromPoint(Vertex start, 
        int x_min, int x_max, int z_min, int z_max, 
        bool reachTop,bool reachRight,bool reachBot,bool reachLeft,
        Direction direction)
    {
        //find vertex with lowest neigbourhood
        //add some offset from borders
        
        //Debug.Log(start);
        fd.ColorPixel(start.x, start.z, 5, redColor);


        //start = new Vertex(10, 10);

        //flood algorithm
        //float step = Math.Abs(vertices[start.x, start.z].y); //height can be negative
        float step = Math.Abs(start.height);

        if (step < 0.01f)
            step = 0.01f;
        else if (step > 0.1f)
            step = 0.1f;

        Vertex highestPoint = ftm.GetHighestpoint();
        float maxThreshold = highestPoint.height;
        fd.ColorPixel(highestPoint.x, highestPoint.z, 5, blueColor);
        //Debug.Log(highestPoint);

        //bool reachLeft = false;
        //bool reachTop = false;
        //bool reachRight = false;
        //bool reachBot = false;

        List<FloodNode> reachableNodes = new List<FloodNode>();
        reachableNodes.Add(new FloodNode(start, 0));
        float threshold = start.height + step;
        //Debug.Log("threshold: " + threshold);

        int gridStep = 20;
        int borderOffset = gridStep + 2;

        int leftEndIndex = 0;
        int rightEndIndex = 0;
        int topEndIndex = 0;
        int botEndIndex = 0;

        bool reachOppositeSide = false;

        //int offset = 20;
        //search until process reaches 2 sides
        while (!((reachBot && reachTop) || (reachLeft && reachRight)) || !reachOppositeSide)
        {
            int proc = 0;
            for (int i = 0; i < reachableNodes.Count; i++)
            {
                FloodNode currentNode = reachableNodes[i];
                //dont process already processed nodes
                if (!currentNode.processed)
                {
                    proc++;
                    //Debug.Log("process "+i);
                    int x = currentNode.vertex.x;
                    int z = currentNode.vertex.z;

                    //record index of node which is close to the border
                    //ignore the corners
                    if (!reachLeft &&fmc.IsCloseTo(x, 0, borderOffset) && !fmc.IsCloseTo(z, 0, borderOffset) && !fmc.IsCloseTo(z, terrainSize, borderOffset))
                    { reachLeft = true; leftEndIndex = i; }//Debug.Log("reachLeft"); }
                    if (!reachRight &&fmc.IsCloseTo(x, terrainSize, borderOffset) && !fmc.IsCloseTo(z, 0, borderOffset) && !fmc.IsCloseTo(z, terrainSize, borderOffset))
                    { reachRight = true; rightEndIndex = i; }//Debug.Log("reachRight"); }
                    if (!reachBot &&fmc.IsCloseTo(z, 0, borderOffset) && !fmc.IsCloseTo(x, 0, borderOffset) && !fmc.IsCloseTo(x, terrainSize, borderOffset))
                    { reachBot = true; botEndIndex = i; }//Debug.Log("reachBot"); }
                    if (!reachTop &&fmc.IsCloseTo(z, terrainSize, borderOffset) && !fmc.IsCloseTo(x, 0, borderOffset) && !fmc.IsCloseTo(x, terrainSize, borderOffset))
                    { reachTop = true; topEndIndex = i; }//Debug.Log("reachTop"); }

                    if (direction != Direction.none)
                    {
                        switch (direction)
                        {
                            case Direction.up:
                                if (reachTop)
                                    reachOppositeSide = true;
                                break;
                            case Direction.down:
                                if (reachBot)
                                    reachOppositeSide = true;
                                break;
                        }
                        reachTop = false;
                        reachRight = false;
                        reachBot = false;
                        reachLeft = false;
                    }

                    //if (endIndex.Count == 2)
                    if ((reachBot && reachTop) || (reachLeft && reachRight))
                        break;

                    if (i > terrainSize * terrainSize)
                    {
                        Debug.Log("FAIL");
                        reachLeft = true;
                        reachRight = true;
                        reachBot = true;
                        reachTop = true;
                        break;
                    }

                    List<Vertex> neighbours = ftm.Get8Neighbours(currentNode.vertex, gridStep, 0, threshold, x_min, x_max, z_min, z_max);
                    if (neighbours.Count == 8)
                    {
                        currentNode.processed = true;
                    }
                    foreach (Vertex v in neighbours)
                    {
                        if (v.height < threshold && !reachableNodes.Contains(new FloodNode(v, i)))
                        {
                            reachableNodes.Add(new FloodNode(v, i));
                        }
                    }
                }
                else
                {
                    //Debug.Log("skip " + i);
                }
            }
            threshold += step;
            //Debug.Log("proc:"+proc+"/"+reachableNodes.Count);
            //Debug.Log("step++");
            if (threshold > maxThreshold)
            {
                Debug.Log("step=" + step);
                Debug.Log("max=" + maxThreshold);
                Debug.Log("FAILz");
                break;
            }
        }

        List<Vertex> path1 = new List<Vertex>();
        int index1 = 0; ;
        int index2 = 0;
        if (reachLeft && reachRight)
        {
            index1 = leftEndIndex;
            index2 = rightEndIndex;
        }
        else
        {
            index1 = topEndIndex;
            index2 = botEndIndex;
        }
        while (index1 != 0)
        {
            path1.Add(reachableNodes[index1].vertex);
            index1 = reachableNodes[index1].parentIndex;
        }
        List<Vertex> path2 = new List<Vertex>();

        while (index2 > 0) //dont add start node to this path (it is already in path1)
        {
            path2.Add(reachableNodes[index2].vertex);
            index2 = reachableNodes[index2].parentIndex;
        }

        //now we reverse path2 and connect it with path1
        path2.Reverse();


        //check if nodes aren't duplicate
        bool duplicate = true;
        if (path1.Count == 0 || path2.Count == 0)
            duplicate = false;
        Vertex lastDuplicate = null;
        //int origCount = path1.Count;
        for (int i = 0; i < path1.Count - 1; i++)
        {
            if (!duplicate)
            {
                Debug.Log("break");
                break;
            }
            //Debug.Log(i);
            if (!path1[path1.Count - 1 - i].Equals(path2[i]))
            {
                //Debug.Log("no dupl");
                duplicate = false;
                break;
            }
            if (duplicate)
            {
                Debug.Log("removing " + path1[path1.Count - 1 - i]);
                lastDuplicate = path1[path1.Count - 1 - i].Clone();
                path1.RemoveAt(path1.Count - 1 - i);
                path2.RemoveAt(i);
                i--;

            }
        }

        if (lastDuplicate != null)
        {
            path1.Add(lastDuplicate);
        }
        else
        {
            //Debug.Log("no dupl");
        }


        //connect path1 and path 2
        foreach (Vertex v in path2)
        {
            path1.Add(v);
        }

        //connect with border
        List<Vertex> finalPath = new List<Vertex>();

        Vertex connectStart = new Vertex(0, 0);
        Vertex connectEnd = new Vertex(0, 0);

        UpdateDirectionOfPath(direction,path1);

        if (path1.Count != 0)
        {
            //Debug.Log(path1[0]);
            //Debug.Log(path1[path1.Count - 1]);

            if (direction == Direction.none)
            {
                UpdateDirectionOfPath(Direction.up, path1);
                connectStart = new Vertex(path1[0].x, z_min);
                connectEnd = new Vertex(path1[path1.Count - 1].x, z_max);
            }

            if (direction == Direction.up)
            {
                connectStart = new Vertex(path1[0].x, z_min);
                connectEnd = new Vertex(path1[path1.Count - 1].x, z_max);
            }
            else if (direction == Direction.down)
            {
                Debug.Log("path1[0] :" + path1[0]);
                connectStart = new Vertex(path1[0].x, z_max);
                connectEnd = new Vertex(path1[path1.Count - 1].x, z_min);
            }
            else if (direction == Direction.left) //TODO
            {
            }


            if (reachBot && reachTop)
            {
                
                /*
                if (path1[0].z > path1[path1.Count - 1].z)
                {
                    connectStart = new Vertex(path1[0].x, z_min);
                    connectEnd = new Vertex(path1[path1.Count - 1].x, z_max);
                }
                else
                {
                    connectStart = new Vertex(path1[0].x, 0);
                    connectEnd = new Vertex(path1[path1.Count - 1].x, terrainSize);

                }*/
            }
            else
            {
                //IMPLEMENT!!!
                /*
                UpdateDirectionOfPath(Direction.right, finalPath);
                connectStart = new Vertex(terrainSize, path1[0].z);
                connectEnd = new Vertex(0, path1[path1.Count - 1].z);*/
                /*
                if (path1[0].x > path1[path1.Count - 1].x)
                {
                    connectStart = new Vertex(terrainSize-1, path1[0].z);
                    connectEnd = new Vertex(0, path1[path1.Count - 1].z);
                }
                else
                {
                    connectStart = new Vertex(0, path1[0].z);
                    connectEnd = new Vertex(terrainSize-1, path1[path1.Count - 1].z);

                }*/
            }
        }

        UpdateDirectionOfPath(direction, finalPath);

        //update height to connection points
        connectStart.height = vertices[connectStart.x, connectStart.z].y;
        Debug.Log("connectEnd:" + connectEnd);
        connectEnd.height = vertices[connectEnd.x, connectEnd.z].y;

        //dont add corner
        if (!(connectStart.x == 0 && connectStart.z == 0) && !finalPath.Contains(connectStart))
        {
            Debug.Log("start: " + connectStart);
            if(ftm.IsOnBorder(connectStart))//connect only of point is on border
                finalPath.Add(connectStart);
        }

        foreach (Vertex v in path1)
        {
            finalPath.Add(v);
        }

        if (!(connectEnd.x == 0 && connectEnd.z == 0) && !finalPath.Contains(connectEnd))
        {
            Debug.Log("end: " + connectEnd);
            if (ftm.IsOnBorder(connectEnd))//connect only of point is on border
                finalPath.Add(connectEnd);
        }





        

        bool connecting = false;
        if (!finalPath.Contains(start))//add starting point to digging (if we are connecting)
        {
            connecting = true;
            finalPath.Reverse();
            finalPath.Add(start);
            finalPath.Reverse();
        }

        frd.DigRiver(finalPath, 10, 0.45f);

        if(connecting)
            finalPath.RemoveAt(0);

        //Debug.Log("---------------");
        foreach (Vertex v in finalPath)
        {
            //Debug.Log(v);
            fd.ColorPixel(v.x, v.z, 1, blueColor);

            currentRiver.riverPath.Add(v);
        }
        //record info about river
        if (reachTop && reachBot)
        {
            currentRiver.reachTop = true;
            currentRiver.reachBot = true;
            currentRiver.reachRight = false;
            currentRiver.reachLeft = false;
            
            //Debug.Log(rg.currentRiver);
        }
        else if (reachLeft && reachRight)
        {
            currentRiver.reachTop = false;
            currentRiver.reachBot = false;
            currentRiver.reachRight = true;
            currentRiver.reachLeft = true;
            
        }
        currentRiver.UpdateDirection(direction);

        Debug.Log("current: " + currentRiver);
        


    }

    //MAYBE LATER
    public void FindRiverPath(List<FloodNode> reachableNodes,
        int x_min, int x_max, int z_min, int z_max,
        bool reachTop, bool reachRight, bool reachBot, bool reachLeft,
        int leftEndIndex,int rightEndIndex,int topEndIndex,int botEndIndex,
        int borderOffset, int gridStep, float threshold,float maxThreshold, float step)
    {

        int proc = 0;
        Debug.Log("threshold: " + threshold);
        for (int i = 0; i < reachableNodes.Count; i++)
        {
            FloodNode currentNode = reachableNodes[i];
            //dont process already processed nodes
            if (!currentNode.processed)
            {
                proc++;
                //Debug.Log("process "+i);
                int x = currentNode.vertex.x;
                int z = currentNode.vertex.z;

                //record index of node which is close to the border
                //ignore the corners
                if (!reachLeft && fmc.IsCloseTo(x, 0, borderOffset) && !fmc.IsCloseTo(z, 0, borderOffset) && !fmc.IsCloseTo(z, terrainSize, borderOffset))
                { reachLeft = true; leftEndIndex = i; }//Debug.Log("reachLeft"); }
                if (!reachRight && fmc.IsCloseTo(x, terrainSize, borderOffset) && !fmc.IsCloseTo(z, 0, borderOffset) && !fmc.IsCloseTo(z, terrainSize, borderOffset))
                { reachRight = true; rightEndIndex = i; }//Debug.Log("reachRight"); }
                if (!reachBot && fmc.IsCloseTo(z, 0, borderOffset) && !fmc.IsCloseTo(x, 0, borderOffset) && !fmc.IsCloseTo(x, terrainSize, borderOffset))
                { reachBot = true; botEndIndex = i; }//Debug.Log("reachBot"); }
                if (!reachTop && fmc.IsCloseTo(z, terrainSize, borderOffset) && !fmc.IsCloseTo(x, 0, borderOffset) && !fmc.IsCloseTo(x, terrainSize, borderOffset))
                { reachTop = true; topEndIndex = i; }//Debug.Log("reachTop"); }

                //if (endIndex.Count == 2)
                if ((reachBot && reachTop) || (reachLeft && reachRight))
                    break;

                if (i > terrainSize * terrainSize)
                {
                    Debug.Log("FAIL");
                    reachLeft = true;
                    reachRight = true;
                    reachBot = true;
                    reachTop = true;
                    break;
                }

                List<Vertex> neighbours = ftm.Get8Neighbours(currentNode.vertex, gridStep, 0, threshold, x_min, x_max, z_min, z_max);
                if (neighbours.Count == 8)
                {
                    currentNode.processed = true;
                }
                foreach (Vertex v in neighbours)
                {
                    if (v.height < threshold && !reachableNodes.Contains(new FloodNode(v, i)))
                    {
                        reachableNodes.Add(new FloodNode(v, i));
                    }
                }
            }
            else
            {
                //Debug.Log("skip " + i);
            }
        }
        threshold += step;
        //Debug.Log("proc:"+proc+"/"+reachableNodes.Count);
        //Debug.Log("step++");
        if (threshold > maxThreshold)
        {
            Debug.Log("step=" + step);
            Debug.Log("max=" + maxThreshold);
            Debug.Log("FAILz");
            //break;!!!!!!!!!!!!!!!!!!!!!!!!!!!
        }
    }


    /// <summary>
    /// find river path from given starting point on restricted area
    /// </summary>
    /// <param name="start">starting point</param>
    /// <param name="reachedSides">sideas already reached - result path can't end on any of these these sides</param>
    /// <param name="x_min">area restriction</param>
    /// <param name="x_max">area restriction</param>
    /// <param name="z_min">area restriction</param>
    /// <param name="z_max">area restriction</param>
    /// <returns></returns>
    public RiverInfo GetRiverPathFrom(Vertex start, List<Direction> reachedSides,
        int x_min, int x_max, int z_min, int z_max)
    {
        fd.ColorPixel(start.x, start.z, 5, redColor);
        float step = Math.Abs(start.height); //height can be negative

        if (step <= 0.1f) //step can't be too small
            step = 0.1f;
        else if (step > 0.1f) //step can't be too big
            step = 0.1f;

        Vertex highestPoint = ftm.GetHighestpoint();
        float maxThreshold = highestPoint.height; //highest value on map - river can't flow that heigh
        fd.ColorPixel(highestPoint.x, highestPoint.z, 5, blueColor);
        //Debug.Log(highestPoint);
        
        List<FloodNode> reachableNodes = new List<FloodNode>();
        reachableNodes.Add(new FloodNode(start, 0));
        float threshold = start.height + step;
        Debug.Log("threshold: " + threshold);
        Debug.Log("terrainSize: " + terrainSize);

        int gridStep = 20; //step between river path points
        int borderOffset = gridStep + 5;
        
        //bool reachedSide = false;
        Direction reachedSide = Direction.none;
        int finalIndex = 0;//index of final node (reached one of the sides)

        while (reachedSide == Direction.none)
        {
            for (int i = 0; i < reachableNodes.Count; i++)
            {
                //Debug.Log(i + " - threshold: " + threshold);

                FloodNode currentNode = reachableNodes[i];
                if (!currentNode.processed)
                {
                    int x = currentNode.vertex.x;
                    int z = currentNode.vertex.z;
                    //if (i < 100)
                    //    Debug.Log(i + ":" + currentNode);

                    if (ftm.CheckBounds(x, z, 0, x_min, x_max, z_min, z_max))
                    {
                        //if(i < 100)
                        //    Debug.Log(currentNode);


                        //check if node is not on side that has already been reached
                        bool reachedAvailableSide = fmc.ReachedAvailableSide(x, z, reachedSides, borderOffset, x_min, x_max, z_min, z_max);

                        if (reachedAvailableSide)
                        {
                            
                            
                            

                            reachedSide = fmc.GetReachedSide(x, z, borderOffset, x_min, x_max, z_min, z_max);
                            if (reachedSide != Direction.none)
                            {
                                Debug.Log("!!!!!!!!!!");
                                finalIndex = i;
                                break;
                            }
                            if (x > 150)
                            {
                                Debug.Log(x_min);
                                Debug.Log(x_max);
                                Debug.Log(z_min);
                                Debug.Log(z_max);
                                Debug.Log(reachedSide);
                                Debug.Log(currentNode);
                            }

                        }
                        if (reachedSide != Direction.none)
                        {
                            Debug.Log("????");
                            break;
                        }
                    }
                    if (reachedSide != Direction.none)
                    {
                        Debug.Log("KKKKK");
                        break;
                    }

                    //dont process already processed nodes again
                    if (!currentNode.processed)
                    {
                        if (i > terrainSize * terrainSize)
                        {
                            Debug.Log("FAIL");
                            finalIndex = i;
                            reachedSide = Direction.up;
                            break;
                        }

                        List<Vertex> neighbours =
                            ftm.Get8Neighbours(currentNode.vertex, gridStep, 0, threshold, x_min, x_max, z_min, z_max);
                        if (neighbours.Count == 8)
                        {
                            currentNode.processed = true;
                        }
                        foreach (Vertex v in neighbours)
                        {
                            if (v.height < threshold && !reachableNodes.Contains(new FloodNode(v, i)))
                            {
                                reachableNodes.Add(new FloodNode(v, i));
                            }
                        }
                    }
                    else
                    {
                        //Debug.Log("skip " + i);
                    }
                }
                if (reachedSide != Direction.none)
                {
                    Debug.Log("MMM");
                    break;
                }
            }
            if (reachedSide != Direction.none)
            {
                Debug.Log("JJJJ");
                break;
            }
            threshold += step; 

            if (threshold > maxThreshold)
            {
                Debug.Log("step=" + step);
                Debug.Log("max=" + maxThreshold);
                Debug.Log("FAILz");
                break;
            }
        }

        

        int pathIndex = finalIndex;
        List<Vertex> finalPath = new List<Vertex>();
        finalPath.Add(fmc.GetVertexOnBorder(reachableNodes[finalIndex].vertex, 
            borderOffset, reachedSide,
            x_min, x_max, z_min, z_max)); //add new node which lies exactly on border

        while (pathIndex != 0)//recursively add all vertices of found path
        {
            finalPath.Add(reachableNodes[pathIndex].vertex);
            pathIndex = reachableNodes[pathIndex].parentIndex;
        }
        finalPath.Reverse();

        for (int i = 0; i < finalPath.Count; i++)
        {
            Debug.Log(finalPath[i]);
        }

        RiverInfo river = new RiverInfo(rg.terrain);
        river.riverPath = finalPath;

        reachedSides.Add(reachedSide);
        foreach(Direction side in reachedSides)
        {
            switch (reachedSide)
            {
                case Direction.up:
                    river.reachTop = true;
                    break;
                case Direction.right:
                    river.reachRight = true;
                    break;
                case Direction.down:
                    river.reachBot = true;
                    break;
                case Direction.left:
                    river.reachLeft = true;
                    break;
            }
        }
        return river;
    }

    public void UpdateDirectionOfPath(Direction direction, List<Vertex> path)
    {
        

        if (path.Count == 0)
            return;
        else
        {
            Debug.Log("dir:" + direction);
            foreach(Vertex v in path)
            {
                //Debug.Log(v);
            }
        }

        switch (direction)
        {
            case Direction.up:
                if (path[0].z > path[path.Count - 1].z)
                {
                    path.Reverse();
                }
                break;
            case Direction.down:
                if (path[0].z < path[path.Count - 1].z)
                {
                    path.Reverse();
                }
                break;
        }
    }

    ///FIX!!!!!!!!!!!!!
    /*
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


    public void SimpleSincRiver(int width, float depthFactor)
    {

        //using sinus function
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = terrainSize / 2 - 2 * width; z < terrainSize / 2 + 2 * width; z++)
            {
                //position = terrainSize / 2 - riverWidth - shift - z;

                //use sinus function with period = 2*riverWidth, shifted to the required position
                //depth = -Math.Sin((z - (terrainSize / 2 + riverWidth - shift)) * (Math.PI / (2 * riverWidth)));
                float distance = GetDistanceFromLine(new Vertex(x, z), new Vertex(0, terrainSize / 2), new Vertex(terrainSize, terrainSize / 2));
                //Debug.Log("[" + x + "," + z + "]:" + distance);

                if (distance != 0) //sinc is not defined at 0
                    depth = -depthFactor * Math.Sin((distance / (width / Math.PI))) / (distance / Math.PI);
                else
                    depth = -depthFactor;
                //if (x == 0)
                //Debug.Log("depth=" + depth);
                try
                {
                    vertices[x, z].y += (float)depth / 2;
                }
                catch (IndexOutOfRangeException ex)
                {
                    //Debug.Log("x=" + x + ",z=" + z);
                }
            }

        }
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
    */

}
