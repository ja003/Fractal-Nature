using UnityEngine;
using System.Collections;

public class RiverGenerator  {
    
    public TerrainGenerator terrain { get; set; }
    public Vector3[,] vertices;
    int terrainSize;
    //***********<RIVER...>*************
    public RiverGenerator(TerrainGenerator terrain)
    {
        this.terrain = terrain;
        vertices = terrain.vertices;
        terrainSize = terrain.terrainSize;
    }

    public void GenerateRiver()
    {
        

        int riverWidth = 15;
        for (int x = 0; x < terrain.terrainSize; x++)
        {
            for (int z = terrainSize / 2 - riverWidth; z < terrainSize / 2 + riverWidth; z++)
            {
                float depth = (float)System.Math.Abs((float)riverWidth - (float)System.Math.Abs(terrainSize / 2 - z)) / riverWidth;
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
