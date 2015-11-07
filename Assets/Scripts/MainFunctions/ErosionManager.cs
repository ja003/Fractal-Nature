using UnityEngine;
using System.Collections;

public class ErosionManager {

    public TerrainGenerator terrain;
    int terrainSize;
    public Vector3[,] vertices;

    public float[,] W; //water
    public float[,] S; //sediment
    public Vector2[,] V; //velocity
    public Vector4[,] F; //outflow flux


    //Wind parameters
    Vector2 windStrength = new Vector2(0.0f, 0.0f);
    float windCoverage = 1.0f;
    bool windAltitude = true;





    public ErosionManager(TerrainGenerator terrain)
    {
        this.terrain = terrain;
        vertices = terrain.vertices;
        terrainSize = terrain.terrainSize;
        terrain.erosionManager = this;
    }


    //HYDRAULIC EROSION MODEL
    public void SetHydraulicErosionMaps()
    {
        W = terrain.W;
        S = terrain.S;
        V = terrain.V;
        F = terrain.F;
    }

    public void initHydraulicMaps()
    {
        SetHydraulicErosionMaps();
        

        //Assign starting values to the hydraulic erosion maps
        for (int x = 0; x < terrainSize; x++)
        {
            for (int y = 0; y < terrainSize; y++)
            {

                W[x, y] = 0.0f; //water map
                S[x, y] = 0;   //sediment map
                V[x, y] = new Vector2(0, 0);      //velocity map
                F[x, y] = new Vector4(0, 0, 0, 0); //outflow flux map (left, right, bottom, top)
            }
        }
    }

    public void applyHydraulicErosion(bool rainFlag, int rainIntensity, float Kc, float Ks, float Kd, float Ke, float Kr, float G)
    {
        SetHydraulicErosionMaps();

        // Kc = water viscosity
        // Ks = strength or (1.0 - terrainDensity)
        // Kd = deposition factor
        // Ke = evaporation speed
        // Kr = droplet weight
        // G  = gravity

        //If you increase time, you must increase pipe length and cell area
        //The function is not designed to be continuous over large time scale 

        float T = 0.1f;  //delta time
        float L = 1.0f;  //pipe cross-sectional area
        float A = 1.0f;  //cell area

        //Scale variables
        Kr /= 10;
        Ke *= 10;


        //Step 1: DISTRIBUTING WATER AS RAIN
        if (rainFlag)
            //Iterate through rainIntensity
            for (int k = 0; k < rainIntensity; k++)
            {

                //Pick random spot
                int x = Random.Range(0, terrainSize - 1);
                int y = Random.Range(0, terrainSize - 1);

                //Add water to spot
                W[x, y] += Kr;
            }

        // Initialise wind direction flags
        int SNflag = 0;
        int WEflag = 0;

        // Set wind direction flags : 1 if > 0;  0 if == 0;  1 if > 0;
        if (windStrength.x < 0) SNflag = -1;
        else if (windStrength.x > 0) SNflag = 1;
        if (windStrength.y < 0) WEflag = -1;
        else if (windStrength.y > 0) WEflag = 1;

        //Debug.Log(SNflag);


        //Step 2: UPDATING OUTFLOW FLUX MAP
        for (int xVal = 0; xVal < terrainSize; xVal++)
        {
            for (int yVal = 0; yVal < terrainSize; yVal++)
            {

                // Initialise flux variables
                float fluxL, fluxB, fluxR, fluxT;

                // Get current point height
                float currentH = vertices[xVal, yVal].y + W[xVal, yVal] + S[xVal, yVal];

                // Retrieve potential wind forces at this cell over the X and Y directions
                // These values are to be further checked for applicability
                float windPotentialX = windValue(windStrength.x, currentH, xVal, yVal);
                float windPotentialY = windValue(windStrength.y, currentH, xVal, yVal);


                //LEFT OUTFLOW
                if (xVal > 0)
                {

                    //Get total height and height difference with neigbour
                    float hN = vertices[xVal - 1, yVal].y + W[xVal - 1, yVal] + S[xVal - 1, yVal];
                    float deltaH = currentH - hN;

                    //Initialise wind force
                    float windForce = 0;

                    //Check against slopes and for compatibility with the user's windCoverage variable 
                    //And apply as suited
                    if (WEflag == 1 && deltaH > 0.0f - windCoverage) windForce = -1 * windPotentialY;
                    else
                    if (WEflag == -1 && deltaH < 0.0f + windCoverage) windForce = -1 * windPotentialY;

                    //Clamp value
                    fluxL = Mathf.Max(0.0f, F[xVal, yVal].x + T * A * ((G * deltaH + windForce) / L));
                }
                else
                    fluxL = 0;


                //RIGHT OUTFLOW
                if (xVal < terrainSize - 1)
                {

                    //Get total height and height difference with neigbour
                    float hN = vertices[xVal + 1, yVal].y + W[xVal + 1, yVal] + S[xVal + 1, yVal];
                    float deltaH = currentH - hN;

                    //Initialise wind force
                    float windForce = 0;

                    //Check against slopes and for compatibility with the user's windCoverage variable 
                    //And apply as suited
                    if (WEflag == 1 && deltaH < 0.0f + windCoverage) windForce = windPotentialY;
                    else
                    if (WEflag == -1 && deltaH > 0.0f - windCoverage) windForce = windPotentialY;

                    //Clamp value
                    fluxR = Mathf.Max(0.0f, F[xVal, yVal].y + T * A * ((G * deltaH + windForce) / L));
                }
                else
                    fluxR = 0;


                // BOTTOM OUTFLOW
                if (yVal > 0)
                {

                    //Get total height and height difference with neigbour
                    float hN = vertices[xVal, yVal - 1].y + W[xVal, yVal - 1] + S[xVal, yVal - 1];
                    float deltaH = currentH - hN;

                    //Initialise wind force
                    float windForce = 0;

                    //Check against slopes and for compatibility with the user's windCoverage variable 
                    //And apply as suited
                    if (SNflag == 1 && (deltaH > 0.0f - windCoverage)) windForce = -1 * windPotentialX;
                    //else 
                    if (SNflag == -1 && (deltaH < 0.0f + windCoverage)) windForce = -1 * windPotentialX;

                    //Clamp value
                    fluxB = Mathf.Max(0.0f, F[xVal, yVal].z + T * A * ((G * deltaH + windForce) / L));
                }
                else
                    fluxB = 0;


                // TOP OUTFLOW
                if (yVal < terrainSize - 1)
                {

                    //Get total height and height difference with neigbour
                    float hN = vertices[xVal, yVal + 1].y + W[xVal, yVal + 1] + S[xVal, yVal + 1];
                    float deltaH = currentH - hN;

                    //Initialise wind force
                    float windForce = 0;

                    //Check against slopes and for compatibility with the user's windCoverage variable 
                    //And apply as suited
                    if (SNflag == 1 && (deltaH < 0.0f + windCoverage)) windForce = windPotentialX;
                    //else 
                    if (SNflag == -1 && (deltaH > 0.0f - windCoverage)) windForce = windPotentialX;

                    //Clamp value
                    fluxT = Mathf.Max(0.0f, F[xVal, yVal].w + T * A * ((G * deltaH + windForce) / L));
                }
                else
                    fluxT = 0;



                //If the sum of the outflow flux exceeds the water amount of the cell,
                //the flux value will be scaled down by a factor K to avoid negative
                //updated water height

                float K = Mathf.Min(1.0f, (W[xVal, yVal] * L * L) / ((fluxL + fluxR + fluxT + fluxB) * T));

                if ((fluxL + fluxR + fluxT + fluxB) * T > W[xVal, yVal])
                {

                    F[xVal, yVal].x = fluxL * K;
                    F[xVal, yVal].y = fluxR * K;
                    F[xVal, yVal].z = fluxB * K;
                    F[xVal, yVal].w = fluxT * K;
                }
                else
                {

                    F[xVal, yVal].x = fluxL;
                    F[xVal, yVal].y = fluxR;
                    F[xVal, yVal].z = fluxB;
                    F[xVal, yVal].w = fluxT;
                }
            }
        }


        // VELOCITY UPDATE AND EROSION-DEPOSITION STEP
        for (int xVal = 0; xVal < terrainSize; xVal++)
        {
            for (int yVal = 0; yVal < terrainSize; yVal++)
            {

                //Get inflow and outflow data
                //Clamping to mesh size
                float inL, inR, inB, inT;
                if (xVal > 0) inL = F[xVal - 1, yVal].y;
                else inL = 0;
                if (yVal > 0) inB = F[xVal, yVal - 1].w;
                else inB = 0;
                if (xVal < terrainSize - 1) inR = F[xVal + 1, yVal].x;
                else inR = 0;
                if (yVal < terrainSize - 1) inT = F[xVal, yVal + 1].z;
                else inT = 0;

                //Compute inflow and outflow for velocity update
                float fluxIN = inL + inR + inB + inT;
                float fluxOUT = F[xVal, yVal].x + F[xVal, yVal].y + F[xVal, yVal].z + F[xVal, yVal].w;

                //V is net volume change for the water over time
                float V = Mathf.Max(0.0f, T * (fluxIN - fluxOUT));

                //The water is updated according to the volume change 
                //and cross-sectional area of pipe
                W[xVal, yVal] += (V / (L * L));

                //Step 3: UPDATING THE VELOCITY FIELD
                Vector2 velocityField;
                velocityField.x = inL - F[xVal, yVal].x + F[xVal, yVal].y - inR;
                velocityField.y = inT - F[xVal, yVal].w + F[xVal, yVal].z - inB;
                velocityField *= 0.5f;

                // Compute maximum sediment capacity
                float C = Kc * velocityField.magnitude * findSlope(xVal, yVal);


                //Step 4: EROSION AND DEPOSITION STEP

                //If sediment transport capacity greater than sediment in water 
                //remove some land and add to sediment scaled by dissolving constant (Ks)
                //Else if sediment transport capacity less than sediment in water 
                //remove some sediment and add to land scaled by deposition constant (Kd)

                float KS = Mathf.Max(0, Ks * (C - S[xVal, yVal]));
                float KD = Mathf.Max(0, Kd * (S[xVal, yVal] - C));

                //Sediment capacity check
                if ((C > S[xVal, yVal]) && (vertices[xVal, yVal].y - KS > 0.0f) && (W[xVal, yVal] > S[xVal, yVal]))
                {
                    vertices[xVal, yVal].y -= KS;
                    S[xVal, yVal] += KS;
                }
                else
                {
                    vertices[xVal, yVal].y += KD;
                    S[xVal, yVal] -= KD;
                }

                //Step 5: SEDIMENT MAP UPDATE
                S[xVal, yVal] = S[(int)(xVal - velocityField.x), (int)(yVal - velocityField.y)];

                //Step 6: WATER EVAPORATION DUE TO HEAT (Ke)
                W[xVal, yVal] *= (1.0f - (Ke) * T);
            }
        }
    }

    public void setWindParam(Vector2 strength, float coverage, bool altitudeFlag)
    {

        //Update wind parameters

        windStrength = strength;
        windCoverage = coverage;
        windAltitude = altitudeFlag;
    }

    private float windValue(float strength, float height, int x, int y)
    {

        //Retrieve wind potential force at vertices[x,y]

        //Get slope value
        float slope = findSlope(x, y);

        //Clamp to 0.005 
        if (slope < 0.005f) slope = 0.005f;

        //Check if altitude scaling is not on 
        //Set value to null if so, 1
        if (!windAltitude) height = 1;

        //Return wind potential force
        return strength * height * slope;
    }

    //THERMAL EROSION MODEL

    public void applyThermalErosion(int iterations, float slopeMin, float c)
    {

        //Thermal erosion main algorithm

        //Start iterating
        for (int iter = 0; iter < iterations; iter++)
        {

            //Pick random position and start the main algorithm
            int xVal = Random.Range(0, terrainSize - 1);
            int zVal = Random.Range(0, terrainSize - 1);

            //Call the sediment transportation recursive step
            thermalRecursion(xVal, zVal, slopeMin, c);
        }
    }

    public void localThermalErosion(Vector3 start, Vector3 end, int iterations, float slopeMin, float c)
    {

        //Localised thermal erosion for 'on-the-fly' content generation

        //Iterate 
        for (int iter = 0; iter < iterations; iter++)
        {

            //Pick random value inside the enclosed area and start the main algorithm
            int xVal = Random.Range((int)start.x, (int)end.x);
            int zVal = Random.Range((int)start.z, (int)end.z);

            //Call the sediment transportation recursive step
            thermalRecursion(xVal, zVal, slopeMin, c);
        }
    }

    private void thermalRecursion(int xVal, int zVal, float T, float c)
    {

        //Recursive sediment transportation with slope checking

        //Find lowest neighbour coordinates
        Vector2 lowestNeighbour = findLowestNeighb(xVal, zVal);

        //Calculate distance/slope
        float dist = vertices[xVal, zVal].y - vertices[(int)lowestNeighbour.x, (int)lowestNeighbour.y].y;

        //Check bounds
        if (dist > T)
        {

            //Move sediment 
            float sedAmount = c * (dist - T);

            vertices[xVal, zVal].y -= sedAmount;
            vertices[(int)lowestNeighbour.x, (int)lowestNeighbour.y].y += sedAmount;

            //Recall
            thermalRecursion((int)lowestNeighbour.x, (int)lowestNeighbour.y, T, c);
        }
    }

    private Vector2 findLowestNeighb(int xVal, int zVal)
    {

        //Function to find lowest height in the Moore neighborhood

        int indexX = 0;
        int indexY = 0;

        float min = 10;

        //Clamp values and check neighbours' heights

        if (xVal > 0 && zVal > 0 && vertices[xVal - 1, zVal - 1].y < min) { indexX = xVal - 1; indexY = zVal - 1; min = vertices[xVal - 1, zVal - 1].y; }
        if (xVal > 0 && zVal < terrainSize - 1 && vertices[xVal - 1, zVal + 1].y < min) { indexX = xVal - 1; indexY = zVal + 1; min = vertices[xVal - 1, zVal + 1].y; }
        if (xVal < terrainSize - 1 && zVal < terrainSize - 1 && vertices[xVal + 1, zVal + 1].y < min) { indexX = xVal + 1; indexY = zVal + 1; min = vertices[xVal + 1, zVal + 1].y; }
        if (xVal < terrainSize - 1 && zVal > 0 && vertices[xVal + 1, zVal - 1].y < min) { indexX = xVal + 1; indexY = zVal - 1; min = vertices[xVal + 1, zVal - 1].y; }

        if (xVal > 0 && vertices[xVal - 1, zVal].y < min) { indexX = xVal - 1; indexY = zVal; min = vertices[xVal - 1, zVal].y; }
        if (xVal < terrainSize - 1 && vertices[xVal + 1, zVal].y < min) { indexX = xVal + 1; indexY = zVal; min = vertices[xVal + 1, zVal].y; }
        if (zVal > 0 && vertices[xVal, zVal - 1].y < min) { indexX = xVal; indexY = zVal - 1; min = vertices[xVal, zVal - 1].y; }
        if (zVal < terrainSize - 1 && vertices[xVal, zVal + 1].y < min) { indexX = xVal; indexY = zVal + 1; min = vertices[xVal, zVal + 1].y; }

        //Return index of lowest neighbour as 2D vector
        return new Vector2(indexX, indexY);
    }

    private float findSlope(int xVal, int yVal)
    {

        //Find the slope at position xVal, yVal

        float[] neighbH = new float[4];

        //Find neighbours
        if (xVal > 0)
            neighbH[0] = vertices[xVal - 1, yVal].y;
        else
            neighbH[0] = vertices[xVal, yVal].y;
        if (xVal < terrainSize - 1)
            neighbH[1] = vertices[xVal + 1, yVal].y;
        else
            neighbH[1] = vertices[xVal, yVal].y;
        if (yVal > 0)
            neighbH[2] = vertices[xVal, yVal - 1].y;
        else
            neighbH[2] = vertices[xVal, yVal].y;
        if (yVal < terrainSize - 1)
            neighbH[3] = vertices[xVal, yVal + 1].y;
        else
            neighbH[3] = vertices[xVal, yVal].y;

        //Find normal
        Vector3 va = new Vector3(1.0f, 0.0f, neighbH[1] - neighbH[0]);
        Vector3 vb = new Vector3(0.0f, 1.0f, neighbH[3] - neighbH[2]);
        Vector3 n = Vector3.Cross(va.normalized, vb.normalized);

        //Return dot product of normal with the Y axis
        return Mathf.Max(0.05f, 1.0f - Mathf.Abs(Vector3.Dot(n, new Vector3(0, 1, 0))));

    }


}
