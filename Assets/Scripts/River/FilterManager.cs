using UnityEngine;
using System.Collections;

public class FilterManager
{
    //LOW-PASS / SPIKES REMOVAL FILTER MODEL
    int terrainSize;
    Vector3[,] vertices;
    float[,] gaussianKernel;




    public FilterManager(TerrainGenerator terrain)
    {
        terrainSize = terrain.terrainSize;
        vertices = terrain.vertices;
        terrain.filterManager = this;


    }

    public void applySpikesFilter(float epsilon)
    {

        //Personal filter for smoothing out unwanted spikes

        //Iterate through the mesh
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = 0; z < terrainSize; z++)
            {

                float nSum = 0;
                float index = 0;

                //Find neighbours and add their values
                if (x > 0) { nSum += vertices[x - 1, z].y; index += 1; }
                if (x < terrainSize - 1) { nSum += vertices[x + 1, z].y; index += 1; }
                if (z > 0) { nSum += vertices[x, z - 1].y; index += 1; }
                if (z < terrainSize - 1) { nSum += vertices[x, z + 1].y; index += 1; }
                if (x > 0 && z > 0) { nSum += vertices[x - 1, z - 1].y; index += 1; }
                if (x > 0 && z < terrainSize - 1) { nSum += vertices[x - 1, z + 1].y; index += 1; }
                if (x < terrainSize - 1 && z < terrainSize - 1) { nSum += vertices[x + 1, z + 1].y; index += 1; }
                if (x < terrainSize - 1 && z > 0) { nSum += vertices[x + 1, z - 1].y; index += 1; }

                //Find neighbours height average
                float averageN = nSum / index;

                //Check offset parameters and assign new values
                if (vertices[x, z].y < averageN - epsilon) vertices[x, z].y = averageN - epsilon;
                if (vertices[x, z].y > averageN + epsilon) vertices[x, z].y = averageN + epsilon;
            }
        }
    }
    //^^^^^^^^^^^^^^^^^^^^^^^^



    //GAUSSIAN FILTER MODEL

    public void applyGaussianBlur(float blurring_factor, int kernel_size, Vector3 start, Vector3 end)
    {

        //Gaussian filter main loop


        //Build the kernel
        initGaussKernel(blurring_factor, kernel_size);
        int half_step = (int)(kernel_size / 2);


        //Copy the vertices onto temporary map
        Vector3[,] temp;
        temp = vertices;

        //Iterate through the mesh
        for (int x = (int)start.x; x < (int)end.x + 1; x++)
            for (int y = (int)start.z; y < (int)end.z + 1; y++)
            {

                float sum = 0.0f;

                //Iterate through kernel
                for (int m = -1 * half_step; m <= half_step; m++)
                    for (int n = -1 * half_step; n <= half_step; n++)
                    {

                        //Average the values according to the kernel weights
                        if (x + m < 0) sum += vertices[x, y].y * gaussianKernel[m + half_step, n + half_step];
                        else if (y + n < 0) sum += vertices[x, y].y * gaussianKernel[m + half_step, n + half_step];
                        else if (x + m > terrainSize - 1) sum += vertices[x, y].y * gaussianKernel[m + half_step, n + half_step];
                        else if (y + n > terrainSize - 1) sum += vertices[x, y].y * gaussianKernel[m + half_step, n + half_step];
                        else sum += vertices[x + m, y + n].y * gaussianKernel[m + half_step, n + half_step];
                    }

                //Assign new value to temporary map
                temp[x, y].y = sum;
            }

        //Swap maps
        vertices = temp;
    }

    private void initGaussKernel(float blurring_factor, int kernel_size)
    {

        //Gaussian kernel build algorithm

        //Initialise kernel map
        gaussianKernel = new float[kernel_size, kernel_size];
        int half_step = (int)(kernel_size / 2);

        float PI = 3.14159265359f;
        float sum = 0.0f;

        //Iterate through kernel map
        for (int x = -1 * half_step; x <= half_step; x++)
        {
            for (int y = -1 * half_step; y <= half_step; y++)
            {

                //Assign raw values to the kernel
                gaussianKernel[x + half_step, y + half_step] = (1.0f / 2 * PI * (blurring_factor * blurring_factor)) * Mathf.Exp(-1.0f * ((x * x + y * y) / (2 * (blurring_factor * blurring_factor))));
                sum += gaussianKernel[x + half_step, y + half_step];
            }
        }

        //Iterate through map
        for (int x = -1 * half_step; x <= half_step; x++)
        {
            for (int y = -1 * half_step; y <= half_step; y++)
            {

                //Normalise the values
                gaussianKernel[x + half_step, y + half_step] = gaussianKernel[x + half_step, y + half_step] / sum;
            }
        }
    }
    //^^^^^^^^^^^^^^^^^^^^^^^^


}
