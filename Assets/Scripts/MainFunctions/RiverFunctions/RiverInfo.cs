using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RiverInfo  {

    public List<Vertex> riverPath;
    public bool reachTop;
    public bool reachRight;
    public bool reachBot;
    public bool reachLeft;

    public Vertex topVertex;
    public Vertex rightVertex;
    public Vertex botVertex;
    public Vertex leftVertex;

    public TerrainGenerator terrain;
    public FunctionRiverPlanner frp;

    public RiverInfo(TerrainGenerator terrain)
    {
        riverPath = new List<Vertex>();
        reachTop = false;
        reachRight = false;
        reachBot = false;
        reachLeft = false;
        this.terrain = terrain;
    }

    public void UpdatePosition(Direction direction)
    {
        switch (direction)
        {
            case Direction.up:
                UpdateRiverValues(0, -terrain.patchSize);
                CutoffRiverPart(Direction.up);
                
                break;
            case Direction.down :
                UpdateRiverValues(0, terrain.patchSize);
                CutoffRiverPart(Direction.down);
                
                break;
        }
    }

    public void CutoffRiverPart(Direction direction)
    {
        int maxIndex = -1;
        switch (direction)
        {
            case Direction.up:
                for(int i=0;i<riverPath.Count;i++)
                {
                    if(riverPath[i].z < 0)
                    {
                        maxIndex = i;
                    }
                    botVertex = riverPath[maxIndex + 1];
                }
                break;
            case Direction.down:
                for (int i = 0; i < riverPath.Count; i++)
                {
                    if (riverPath[i].z > terrain.terrainSize)
                    {
                        maxIndex = i;
                    }
                    topVertex = riverPath[maxIndex + 1];
                }
                break;
        }

        for(int i = maxIndex; i >= 0; i--)
        {
            Debug.Log("cutting: " + riverPath[i]);
            riverPath.RemoveAt(i);
        }
    }


    private void UpdateRiverValues(int diffX, int diffZ)
    {
        foreach(Vertex v in riverPath)
        {
            string s = "updating " + v;
            v.x += diffX;
            v.z += diffZ;
            s += " to " + v;
            //Debug.Log(s);
        }
    }

    public void UpdateDirection(Direction direction)
    {
        UpdateDirection(direction, riverPath);
    }

    public void UpdateDirection(Direction direction, List<Vertex> riverPath)
    {
        if (riverPath.Count == 0)
        {
            Debug.Log("no river");
            return;
        }

        frp.UpdateDirectionOfPath(direction, riverPath);

        switch (direction)
        {
            case Direction.up:
                topVertex = riverPath[riverPath.Count-1];
                botVertex = riverPath[0];
                break;
            case Direction.down:
                topVertex = riverPath[0];
                botVertex = riverPath[riverPath.Count - 1];
                break;
        }
    }

    public override string ToString()
    {
        string info = "";
        info += "reachTop: " + reachTop + "\n";
        info += "reachRight: " + reachRight + "\n";
        info += "reachBot: " + reachBot + "\n";
        info += "reachLeft: " + reachLeft + "\n";
        foreach(Vertex v in riverPath)
        {
            info += riverPath.IndexOf(v)+": " + v + "\n";
        }
        info += "topVertex:" + topVertex+"\n";
        info += "rightVertex:" + rightVertex + "\n";
        info += "botVertex:" + botVertex+"\n";
        info += "leftVertex:" + leftVertex+"\n";

        return info;
    }
}
