using UnityEngine;
using System.Collections;

public class SceneManager : MonoBehaviour
{

    // Main terrain object
    public TerrainGenerator terrain;
    RiverGenerator riverGenerator;
    FilterManager filterManager;
    ErosionManager erosionManager;

    int frameMark = 0;


    //CWater water;

    // Camera position
    Vector3 camera_position;

    // Mesh sizes
    int patchSize = 64;       // The mesh is composed by a matrix of patches of terrain of sizes 2^i on which the algorithms operate
    int patchMatrixSize = 3; //  Patch matrix size  (valueXvalue)

    // Diamond-square algorithm strength
    float diSqStrength = 0.7f;

    // Low-pass/Spikes filter strength
    float spikesStrength = 0.995f;

    // Gaussian filter amount, and kernel size
    float gaussAmount = 3.5f; //strength
    int kernelSize = 5;    // kernel size (valueXvalue)
                           // Helper for resizing the kernel
    float kernelTemp = 2;

    // Thermal erosion parameters
    int thermalIterations = 500;      // Iterations count
    float thermalIterationsTemp = 500; // Float copy
    float thermalSlope = 0.00002f;    //  Minimum slope reach
                                      //1.0 - Terrain density
    float thermalC = 0.2f; // Strength or (1 - Terrain density)

    // Mesh position helpers
    Vector3 nullVec = new Vector3(0, 0, 0);
    Vector3 endPoint;

    // On-the-fly generation minimum camera distance from edge of mesh 
    float cameraOffset = 100;

    // Interactive menu initial settings
    bool mainMenuFlag = true;
    bool filtersMenuFlag = false;
    bool erosionMenuFlag = false;
    bool waterMenuFlag = false;
    bool flightModeFlag = false;

    // Interactive menu control properties flags
    bool infiniteTerrain = false; // On-the-fly generation
    bool diSqFlag = true;        // Diamond-square switch
    bool gaussFlag = false;     // On-the-fly gaussian blur generation
    bool thermalFlag = true;   // On-the-fly thermal erosion generation
    bool thermalGenFlag = false;  // Thermal erosion switch
    bool rainFlag = true;        // Hydraulic erosion switch
    bool windAltFlag = true;   // Altitude controlling the wind strength flag
    bool waterGenFlag = false;// Hydraulic erosion switch
    bool proceduralTextureFlag = false;  // Procedural texture switch
    bool heightMapFlag = false;         // Heightmap texture switch
    bool procTextureMenuFlag = false;  // Procedural texture menu flag

    // Hydraulic erosion components
    int rainFreq = 500;            // Rain frequency (drops per iteration)
    float rainFreqTemp = 500.0f;  // Float copy
    float rainAmount = 0.05f;    // Rain amount - weight of droplet
    float waterViscosity = 0.07f;  // Water viscosity / sediment capacity
    float waterDeposition = 0.05f; // Water deposition scalar
    float waterEvap = 0.06f;      // Water evaporation speed
    float waterTerrDens = 0.05f; // Terrain density
    float waterGrav = 9.81f;    // Gravity
    Vector2 windStrength = new Vector2(0.0f, 0.0f); // Wind strength (South<->North, West<->East)
    float windCover = 1.0f; // Maximum slope size reach on opposite-facing surfaces

    // Procedural texture components
    Vector3 sandColor = new Vector3(0.90f, 0.90f, 0.00f);  // Sand r,g,b
    Vector3 grassColor = new Vector3(0.00f, 0.70f, 0.00f); // Grass r,g,b
    Vector3 rockColor = new Vector3(0.20f, 0.05f, 0.00f); // Rock r,g,b
    Vector3 snowColor = new Vector3(1.00f, 1.00f, 1.00f); // Snow r,g,b
    float sandLimit = 0.3f;     // Sand maximum altitude
    float sandStrength = 7.0f;  // Sand color multiplier
    float sandCoverage = 0.02f; // Sand maximum slope size
    float grassStrength = 1.0f; // Grass color multiplier
    float snowLimit = 0.55f;    // Snow minimum altitude
    float snowStrength = 6.0f;  // Snow color multiplier
    float snowCoverage = 0.02f; // Snow minumum slope size
    float slopeLimit = 0.0f;    // Rock altitude
    float slopeStrength = 6.0f; // Rock multiplier
    float noiseTexValue = 0.2f; // Random noise range


    void Start()
    {


        // Initialise main Class object
        terrain = new TerrainGenerator();

        
        // Calling the constructor (Unity's C# does not allow conventional constructors)
        terrain.initialise(patchSize, patchMatrixSize);

        // Updating end-point of main mesh 
        endPoint = new Vector3(terrain.terrainSize - 1, 0, terrain.terrainSize - 1);

        // Generate fractal Diamond-Square turbulence
        terrain.applyDiamondSquare(diSqStrength);

        // Initialise procedural texture variables
        terrain.applyProceduralTex(true, sandColor, sandLimit, sandStrength, sandCoverage, true, grassColor, grassStrength, true, snowColor, snowLimit, snowStrength, snowCoverage, true, rockColor, slopeLimit, slopeStrength, noiseTexValue);

        

        

        // Build mesh
        terrain.build();

        // erosionManager has to be created when
        erosionManager = new ErosionManager(terrain);
        erosionManager.initHydraulicMaps();
        filterManager = new FilterManager(terrain);
        //riverGenerator has to be after filterManager!
        riverGenerator = new RiverGenerator(terrain);

        //doesn't work well with on-fly generation
        riverGenerator.ftm.PerserveMountains(5, 30, 20);
    }

    // Main update loop
    void Update()
    {

        // Update camera position and 'on-the-fly' generation offset
        camera_position = GameObject.Find("Main Camera").transform.position;

        //sometimes it alerts null reference
        if(terrain.scaleTerrain.x != null)
            cameraOffset = terrain.scaleTerrain.x / 3.5f;

        // Lock/Unlock mouse-look view if 'C' key is pressed
        if (!flightModeFlag)
            if (Input.GetKeyDown(KeyCode.C))
            {

                if (!GameObject.Find("Main Camera").GetComponent<MouseLook>().enabled)
                    GameObject.Find("Main Camera").GetComponent<MouseLook>().enabled = true;
                else
                    GameObject.Find("Main Camera").GetComponent<MouseLook>().enabled = false;
            }

        // On-the-fly generation - flag check -> boundary check -> function call.
        if (infiniteTerrain)
        {
            int pathSize = terrain.patchSize;
            int terrainSize = terrain.terrainSize;
            int scaleFactor = 10;
            int offset = 10;

            if (camera_position.z > terrain.endOf.z - cameraOffset)
            {
                terrain.goNorth(diSqFlag, diSqStrength, gaussFlag, gaussAmount, kernelSize, thermalFlag, thermalIterations, thermalSlope, thermalC);
                
                //apply mountain function on new part of map
                riverGenerator.ftm.PerserveMountains(2, 30, scaleFactor, 0, terrainSize, terrainSize - patchSize, terrainSize);
                
                riverGenerator.ftm.MirrorEdge(patchSize, 10, Direction.up);


                riverGenerator.currentRiver.UpdateDirection(Direction.up);
                riverGenerator.currentRiver.UpdatePosition(Direction.up);   

                Debug.Log("orig");
                Debug.Log(riverGenerator.currentRiver);
                Vertex newStart = new Vertex(riverGenerator.currentRiver.topVertex.x, riverGenerator.currentRiver.topVertex.z);
                //Debug.Log("starting from:" + newStart);
                Debug.Log("starting from:" + riverGenerator.currentRiver.topVertex);
                riverGenerator.frp.FloodFromPoint(riverGenerator.currentRiver.topVertex,
                    0, terrainSize - 1, terrainSize -1-pathSize- offset, terrainSize -1,
                    false, false, true, false,
                    Direction.up);

                Debug.Log("new");
                Debug.Log(riverGenerator.currentRiver);

            }
            if (camera_position.z < terrain.startOf.z + cameraOffset)
            {
                terrain.goSouth(diSqFlag, diSqStrength, gaussFlag, gaussAmount, kernelSize, thermalFlag, thermalIterations, thermalSlope, thermalC);

                //apply mountain function on new part of map
                riverGenerator.ftm.PerserveMountains(2, 30, scaleFactor, 0, terrainSize, 0, patchSize);

                riverGenerator.ftm.MirrorEdge(patchSize, 10, Direction.down);


                riverGenerator.currentRiver.UpdateDirection(Direction.down);
                riverGenerator.currentRiver.UpdatePosition(Direction.down);

                Debug.Log("orig");
                Debug.Log(riverGenerator.currentRiver);

                //Debug.Log("starting from:" + riverGenerator.currentRiver.botVertex);
                riverGenerator.frp.FloodFromPoint(riverGenerator.currentRiver.botVertex,
                    0, terrainSize-1, 0, patchSize+offset,
                    true, false, false, false,
                    Direction.down);

                Debug.Log("new");
                Debug.Log(riverGenerator.currentRiver);

            }
            if (camera_position.x < terrain.startOf.x + cameraOffset)
            {
                terrain.goWest(diSqFlag, diSqStrength, gaussFlag, gaussAmount, kernelSize, thermalFlag, thermalIterations, thermalSlope, thermalC);

                //apply mountain function on new part of map
                riverGenerator.ftm.PerserveMountains(2, 30, scaleFactor, 0, patchSize, 0, terrainSize);

                riverGenerator.ftm.MirrorEdge(patchSize, 10, Direction.left);
            }
            if (camera_position.x > terrain.endOf.x - cameraOffset)
            {
                terrain.goEast(diSqFlag, diSqStrength, gaussFlag, gaussAmount, kernelSize, thermalFlag, thermalIterations, thermalSlope, thermalC);

                //apply mountain function on new part of map
                riverGenerator.ftm.PerserveMountains(2, 30, scaleFactor, terrainSize -patchSize, terrainSize, 0, terrainSize);

                riverGenerator.ftm.MirrorEdge(patchSize, 10, Direction.right);
            }
        }

        // Thermal erosion animation flag check
        if (thermalGenFlag)
        {

            // Function call
            erosionManager.applyThermalErosion(thermalIterations, thermalSlope, thermalC);
            // Refresh texture
            terrain.applyProceduralTex(true, sandColor, sandLimit, sandStrength, sandCoverage, true, grassColor, grassStrength, true, snowColor, snowLimit, snowStrength, snowCoverage, true, rockColor, slopeLimit, slopeStrength, noiseTexValue);
            // Refresh mesh
            terrain.build();
        }

        // Hydraulic erosion animation flag check
        if (waterGenFlag)
        {

            // Function call
            erosionManager.applyHydraulicErosion(rainFlag, rainFreq, waterViscosity, waterTerrDens, waterDeposition, waterEvap, rainAmount, waterGrav);
            // Refresh texture
            terrain.applyProceduralTex(true, sandColor, sandLimit, sandStrength, sandCoverage, true, grassColor, grassStrength, true, snowColor, snowLimit, snowStrength, snowCoverage, true, rockColor, slopeLimit, slopeStrength, noiseTexValue);
            // Refresh mesh
            terrain.build();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }

    // On-screen Menu Loop
    void OnGUI()
    {
        //This section stores the data regarding the interactive menu.

        int offset = 100;   // pointer to the y offset value
        int menuWidth = 230;  // menu width
        int rightOffset = 5;   // right-side gap

        // Title
        GUI.Box(new Rect(Screen.width - menuWidth, 10, menuWidth - rightOffset, 30), "Fractal Nature");


        // The following 'if' statements declare the functions of the main top menu buttons

        //General settings button
        if (GUI.Button(new Rect(Screen.width - menuWidth, 42, menuWidth - rightOffset, 40), "General Settings"))
        {

            // Deactivate other menus
            filtersMenuFlag = false;
            erosionMenuFlag = false;

            // Set menu flag
            if (mainMenuFlag) mainMenuFlag = false;
            else mainMenuFlag = true;
        }

        //Filters menu button
        if (GUI.Button(new Rect(Screen.width - menuWidth, 84, menuWidth - rightOffset - menuWidth / 2 - 1, 40), "Filters"))
        {

            // Deactivate other menus
            mainMenuFlag = false;
            erosionMenuFlag = false;

            // Set menu flag
            if (filtersMenuFlag) filtersMenuFlag = false;
            else filtersMenuFlag = true;
        }

        //Erosion menu button
        if (GUI.Button(new Rect(Screen.width - menuWidth + menuWidth / 2 + 1, 84, menuWidth - rightOffset - menuWidth / 2 - 1, 40), "Erosion"))
        {

            // Deactivate other menus
            mainMenuFlag = false;
            filtersMenuFlag = false;

            // Set menu flag
            if (erosionMenuFlag) erosionMenuFlag = false;
            else erosionMenuFlag = true;
        }


        // The following 'if' statements set the individual functions and properties of each menu

        // General settings menu settings
        if (mainMenuFlag)
        {

            // CAMERA MENU



            // RIVER MENU
            offset = 135;
            Rect riverMenuRectangle = new Rect(0, 0, menuWidth - 2*rightOffset, 30);
            GUI.Box(riverMenuRectangle, "River");



            // Apply button
            if (GUI.Button(riverMenuRectangle, "MAKE RIVER") 
                || (Input.GetKeyDown(KeyCode.T) && Time.frameCount > frameMark))
            {
                //Debug.Log(Time.frameCount);
                //Debug.Log(frameMark);
                //Debug.Log("!");
                frameMark = Time.frameCount + 30;
                riverGenerator.GenerateRiver();
            }

            Rect mountainRectangle = new Rect(0,30, menuWidth - 2*rightOffset, 30);
            if (GUI.Button(mountainRectangle, "MAKE MOUNTAIN"))
            {
                riverGenerator.ftm.PerserveMountains(5,30,20);
            }

            offset = 135; // Y offset value
            // Title
            GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 80), "Camera");

            // Display message if free mode is activated
            if (!flightModeFlag)
            {
                GUI.Label(new Rect(Screen.width - menuWidth + 10, offset + 55, menuWidth - rightOffset, 20), "*Press 'C' to lock view.");
                GUI.Box(new Rect(Screen.width - menuWidth, offset + 450, menuWidth - rightOffset, 100), "Movement");
                GUI.Label(new Rect(Screen.width - menuWidth + 10, offset + 25 + 450, menuWidth - rightOffset, 20), "*W,A,S,D & Arrow keys.");
                GUI.Label(new Rect(Screen.width - menuWidth + 10, offset + 45 + 450, menuWidth - rightOffset, 20), "*R,F - move up, move down.");
                GUI.Label(new Rect(Screen.width - menuWidth + 10, offset + 65 + 450, menuWidth - rightOffset, 20), "*C   - lock camera.");
            }
            else
            {
                GUI.Box(new Rect(Screen.width - menuWidth, offset + 450, menuWidth - rightOffset, 60), "Keyboard Controls");
                GUI.Label(new Rect(Screen.width - menuWidth + 10, offset + 25 + 450, menuWidth - rightOffset, 20), "*W,A,S,D & Arrow keys.");
            }

            // FREE button settings
            if (GUI.Button(new Rect(Screen.width - menuWidth + 10, offset + 25, menuWidth / 2 - rightOffset - 20, 25), "Free"))
            {
                //terrain.ColorPixels();

                // Deactivate flight script, activate free mouse look and movement
                GameObject.Find("Airplane").GetComponent<PlanePilot>().enabled = false;
                GameObject.Find("Main Camera").GetComponent<MouseLook>().enabled = true;
                GameObject.Find("Main Camera").GetComponent<cameraMovement>().enabled = true;

                // Detach camera from the airplane
                GameObject.Find("Main Camera").transform.parent = null;

                // Move the airplane object below the mesh and the camera above, both aligned to the mesh
                GameObject.Find("Airplane").transform.position = new Vector3(terrain.middleOf.x, -300, terrain.middleOf.z);
                GameObject.Find("Main Camera").transform.position = new Vector3(terrain.middleOf.x, 200, terrain.middleOf.z);
                GameObject.Find("Main Camera").transform.LookAt(new Vector3(terrain.middleOf.x + 20, 200, terrain.middleOf.z));

                // Deactivate FLIGHT mode
                flightModeFlag = false;
            }

            // FLIGHT button settings
            if (GUI.Button(new Rect(Screen.width - menuWidth / 2 - rightOffset + 15, offset + 25, menuWidth / 2 - rightOffset - 20, 25), "Flight"))
            {

                // Activate flight script and deactivate the free mouse movement scripts
                GameObject.Find("Airplane").GetComponent<PlanePilot>().enabled = true;
                GameObject.Find("Main Camera").GetComponent<MouseLook>().enabled = false;
                GameObject.Find("Main Camera").GetComponent<cameraMovement>().enabled = false;

                // Move airplane object above the mesh, in the centre, align it to the world and link the camera to the object
                GameObject.Find("Airplane").transform.position = new Vector3(terrain.middleOf.x, 200, terrain.middleOf.z);
                GameObject.Find("Airplane").transform.LookAt(new Vector3(terrain.middleOf.x + 20, 200, terrain.middleOf.z));
                GameObject.Find("Main Camera").transform.parent = GameObject.Find("Airplane").transform;

                // Activate FLIGHT mode
                flightModeFlag = true;
            }


            //TEXTURE MENU

            offset = 220; // Y offset value

            bool texTemp; // Copy of procedural texture flag

            // Assign value
            if (proceduralTextureFlag)
                texTemp = true;
            else
                texTemp = false;

            // Background box
            GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 75), "Texture");

            // Procedural toggle
            proceduralTextureFlag = GUI.Toggle(new Rect(Screen.width - menuWidth + 15, 25 + offset, 95, 25), proceduralTextureFlag, "  Procedural");

            // Assign values according to the toggled option
            if (proceduralTextureFlag)
                heightMapFlag = false;
            else
                heightMapFlag = true;

            // Height map toggle
            heightMapFlag = GUI.Toggle(new Rect(Screen.width - menuWidth + 115, 25 + offset, 95, 25), heightMapFlag, "  Height Map");

            // Assign values according to the toggled option
            if (heightMapFlag)
                proceduralTextureFlag = false;
            else
                proceduralTextureFlag = true;

            // Check flags and apply procedural texture
            if (proceduralTextureFlag != texTemp)
                terrain.setTexture(proceduralTextureFlag);

            // Check procedural texture settings menu flag and create the SETTINGS button
            if (!procTextureMenuFlag)
            {
                if (GUI.Button(new Rect(Screen.width - menuWidth / 2 - rightOffset - 95, offset + 50, menuWidth / 2 - rightOffset - 21, 20), "Settings"))
                {
                    // Set procedural settings menu flag 
                    procTextureMenuFlag = true;
                }
            }
            else // Create the HIDE SETTINGS button otherwise
                if (GUI.Button(new Rect(Screen.width - menuWidth / 2 - rightOffset - 95, offset + 50, menuWidth / 2 - rightOffset - 21, 20), "Hide Settings"))
            {
                // Set procedural texture settings menu flag 
                procTextureMenuFlag = false;
            }

            // Check procedural texture settings menu flag and create menu if true
            if (procTextureMenuFlag)
            {
                offset -= 85; // Y offset value

                // Background box
                GUI.Box(new Rect(Screen.width - menuWidth * 2 + 20, offset, menuWidth - rightOffset - 20, 460), "Texture Settings");

                offset += 20; // Y offset value

                // Sand labels
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 30, offset, menuWidth - rightOffset - 20, 75), "Sand");
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 10, offset + 20, menuWidth - rightOffset, 20), "     Strength       Limit      Coverage");

                // Sand values linked to sliders
                sandStrength = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 30, 40 + offset, menuWidth - 170 - rightOffset, 20), sandStrength, 0, 10);
                sandLimit = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 95, 40 + offset, menuWidth - 170 - rightOffset, 20), sandLimit, 0, 1);
                sandCoverage = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 160, 40 + offset, menuWidth - 170 - rightOffset, 20), sandCoverage, 0, 0.1f);

                offset += 30; // Y offset value

                // RGB label
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 10, offset + 20, menuWidth - rightOffset, 20), "          R              G              B");

                // RGB values linked to sliders
                sandColor.x = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 30, 40 + offset, menuWidth - 170 - rightOffset, 20), sandColor.x, 0, 1);
                sandColor.y = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 95, 40 + offset, menuWidth - 170 - rightOffset, 20), sandColor.y, 0, 1);
                sandColor.z = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 160, 40 + offset, menuWidth - 170 - rightOffset, 20), sandColor.z, 0, 1);

                offset += 60; // Y offset value

                // Grass labels
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 30, offset, menuWidth - rightOffset - 20, 75), "Grass");
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 10, offset + 20, menuWidth - rightOffset, 20), "     Strength       ");

                // Grass strength linked to slider
                grassStrength = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 30, 40 + offset, menuWidth - 170 - rightOffset, 20), grassStrength, 0, 10);

                offset += 30; // Y offset value

                // RGB label
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 10, offset + 20, menuWidth - rightOffset, 20), "          R              G              B");

                // RGB values linked to sliders
                grassColor.x = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 30, 40 + offset, menuWidth - 170 - rightOffset, 20), grassColor.x, 0, 1);
                grassColor.y = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 95, 40 + offset, menuWidth - 170 - rightOffset, 20), grassColor.y, 0, 1);
                grassColor.z = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 160, 40 + offset, menuWidth - 170 - rightOffset, 20), grassColor.z, 0, 1);

                offset += 60; // Y offset value

                // Rocks labels
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 30, offset, menuWidth - rightOffset - 20, 75), "Rocks");
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 10, offset + 20, menuWidth - rightOffset, 20), "     Strength       Limit      ");

                // Slope values linked to sliders
                slopeStrength = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 30, 40 + offset, menuWidth - 170 - rightOffset, 20), slopeStrength, 0, 10);
                slopeLimit = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 95, 40 + offset, menuWidth - 170 - rightOffset, 20), slopeLimit, 0, 1);

                offset += 30; // Y offset value

                // RGB label
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 10, offset + 20, menuWidth - rightOffset, 20), "          R              G              B");

                // RGB values linked to sliders
                rockColor.x = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 30, 40 + offset, menuWidth - 170 - rightOffset, 20), rockColor.x, 0, 1);
                rockColor.y = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 95, 40 + offset, menuWidth - 170 - rightOffset, 20), rockColor.y, 0, 1);
                rockColor.z = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 160, 40 + offset, menuWidth - 170 - rightOffset, 20), rockColor.z, 0, 1);

                offset += 60; // Y offset value

                // Snow labels
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 30, offset, menuWidth - rightOffset - 20, 75), "Snow");
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 10, offset + 20, menuWidth - rightOffset, 20), "     Strength       Limit      Coverage");

                // Snow values linked to sliders
                snowStrength = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 30, 40 + offset, menuWidth - 170 - rightOffset, 20), snowStrength, 0, 10);
                snowLimit = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 95, 40 + offset, menuWidth - 170 - rightOffset, 20), snowLimit, 0, 1);
                snowCoverage = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 160, 40 + offset, menuWidth - 170 - rightOffset, 20), snowCoverage, 0, 0.1f);

                offset += 30; // Y offset value

                // RGB label
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 10, offset + 20, menuWidth - rightOffset, 20), "          R              G              B");

                // RGB values linked to sliders
                snowColor.x = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 30, 40 + offset, menuWidth - 170 - rightOffset, 20), snowColor.x, 0, 1);
                snowColor.y = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 95, 40 + offset, menuWidth - 170 - rightOffset, 20), snowColor.y, 0, 1);
                snowColor.z = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 160, 40 + offset, menuWidth - 170 - rightOffset, 20), snowColor.z, 0, 1);

                offset += 30; // Y offset value

                // Noise label
                GUI.Label(new Rect(Screen.width - menuWidth * 2 + 30, offset + 30, menuWidth - rightOffset - 20, 75), "Noise value");
                // Noise value linked to slider
                noiseTexValue = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth * 2 + 45, 55 + offset, menuWidth - 80 - rightOffset, 20), noiseTexValue, 0, 0.85f);

                // Apply changes when button press occurs
                if (GUI.Button(new Rect(Screen.width - menuWidth * 2 + 30, 80 + offset, 65, 25), "APPLY"))
                    terrain.applyProceduralTex(true, sandColor, sandLimit, sandStrength, sandCoverage, true, grassColor, grassStrength, true, snowColor, snowLimit, snowStrength, snowCoverage, true, rockColor, slopeLimit, slopeStrength, noiseTexValue);

            }


            // MESH CONTROL MENU

            offset = 300; // Y offset value

            // Background box
            GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 210), "Mesh Control");

            // 'On-the-fly' mesh generation toggle
            infiniteTerrain = GUI.Toggle(new Rect(Screen.width - menuWidth + 10, offset + 25, menuWidth - rightOffset, 20), infiniteTerrain, "  Allow on-the-fly generation");

            offset += 30; // Y offset value

            // Patches labels
            GUI.Label(new Rect(Screen.width - menuWidth + 10, offset + 25, menuWidth - rightOffset, 20), "Patch Matrix Size");
            GUI.Label(new Rect(Screen.width - menuWidth + 10, offset + 75, menuWidth - rightOffset, 20), "Patch Size");
            GUI.Label(new Rect(Screen.width - menuWidth + 10, offset + 125, menuWidth - rightOffset, 20), "Scale");
            GUI.Label(new Rect(Screen.width - menuWidth + 10, offset + 140, menuWidth - rightOffset, 20), "        x               y               z");

            // 2x2 patch matrix
            if (GUI.Button(new Rect(Screen.width - menuWidth / 2 - rightOffset - 100, offset + 50, menuWidth / 2 - rightOffset - 47, 20), "2x2"))
            {
                //Clear maps and reset program
                //terrain.destroyMeshes();
                destroyMeshes();
                patchMatrixSize = 2;
                Start();
            }

            // 3x3 patch matrix
            if (GUI.Button(new Rect(Screen.width - menuWidth / 2 - rightOffset - 30, offset + 50, menuWidth / 2 - rightOffset - 47, 20), "3x3")

                || (Input.GetKeyDown(KeyCode.E) && Time.frameCount > frameMark))            
            {
                //Debug.Log("!");
                frameMark = Time.frameCount + 30;
                //Clear maps and reset program
                destroyMeshes();
                patchMatrixSize = 3;
                Start();
            }

            // 4x4 patch matrix
            if (patchSize != 128)
                if (GUI.Button(new Rect(Screen.width - menuWidth / 2 - rightOffset + 40, offset + 50, menuWidth / 2 - rightOffset - 47, 20), "4x4"))
                {
                    //Clear maps and reset program
                    destroyMeshes();
                    patchMatrixSize = 4;
                    Start();
                }

            offset += 50; // Y offset value

            // 32x32 patch size
            if (GUI.Button(new Rect(Screen.width - menuWidth / 2 - rightOffset - 100, offset + 50, menuWidth / 2 - rightOffset - 47, 20), "32x32"))
            {
                //Clear maps and reset program
                destroyMeshes();
                patchSize = 32;
                Start();
            }

            // 64x64 patch size
            if (GUI.Button(new Rect(Screen.width - menuWidth / 2 - rightOffset - 30, offset + 50, menuWidth / 2 - rightOffset - 47, 20), "64x64"))
            {
                //Clear maps and reset program
                destroyMeshes();
                patchSize = 64;
                Start();
            }

            // 128x128 patch size
            if (patchMatrixSize != 4)
                if (GUI.Button(new Rect(Screen.width - menuWidth / 2 - rightOffset + 40, offset + 50, menuWidth / 2 - rightOffset - 47, 20), "128x128"))
                {
                    //Clear maps and reset program
                    destroyMeshes();
                    patchSize = 128;
                    Start();
                }

            offset += 50; // Y offset value
            Vector3 scaleTemp = new Vector3(1,1,1);

            if (terrain.scaleTerrain != null)
                scaleTemp  = terrain.scaleTerrain;

            // Scaling parameters linked to sliders
            terrain.scaleTerrain.x = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 10, 60 + offset, menuWidth - 160 - rightOffset, 20), terrain.scaleTerrain.x, 20, 1400);
            terrain.scaleTerrain.y = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 80, 60 + offset, menuWidth - 160 - rightOffset, 20), terrain.scaleTerrain.y, 20, 380);
            terrain.scaleTerrain.z = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 150, 60 + offset, menuWidth - 160 - rightOffset, 20), terrain.scaleTerrain.z, 20, 1400);

            // If changes in scaling occur, rebuild mesh
            if (scaleTemp != terrain.scaleTerrain)
                terrain.build();


            offset += 85; // Y offset value


            // EXPORT MESH MENU -- button calling the export .obj library -- Exporting 4 meshes AT DESTINATION 'C:/' ON THE SYSTEM
            GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 65), " ");
            GUI.Label(new Rect(Screen.width - menuWidth + 15, offset + 40, menuWidth - rightOffset, 20), "Path: 'C:/'");
            if (GUI.Button(new Rect(Screen.width - menuWidth + 10, offset + 5, menuWidth - rightOffset - 20, 35), "EXPORT MESH"))
            {
                terrain.exportObj();
            }
        }

        // Filter menu settings
        if (filtersMenuFlag)
        {

            // FRACTAL TURBULENCE MENU

            offset = 135; // Y offset value

            // Background box
            GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 60), "Fractal Turbulence");

            // Diamond-Square strength linked to slider
            diSqStrength = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 80, 35 + offset, menuWidth - 85 - rightOffset, 30), diSqStrength, 0.0f, 2.0f);

            // Name label
            GUI.Label(new Rect(Screen.width - menuWidth + 10, 64 - 35 + offset, menuWidth - rightOffset, 30), "Strength");

            offset = 200; // Y offset value

            // GAUSSIAN FILTER MENU

            // Background box
            GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 120), "Gaussian Filter");

            // Blur amount value linked to slider
            gaussAmount = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 85, 35 + offset, menuWidth - 90 - rightOffset, 20), gaussAmount, 1.0f, 4.5f);

            // Name label
            GUI.Label(new Rect(Screen.width - menuWidth + 10, 64 - 35 + offset, menuWidth - rightOffset, 30), "Strength");

            // Kernel size copy linked to slider
            kernelTemp = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 85, 55 + offset, menuWidth - 90 - rightOffset, 20), kernelTemp, 0.8f, 2.9f);

            // Name label
            GUI.Label(new Rect(Screen.width - menuWidth + 10, 84 - 35 + offset, menuWidth - rightOffset, 30), "Kernel Size");

            // Kernel size converted to an integer*+1  (values: 2, 5, 7, ...)
            kernelSize = (int)kernelTemp * 2 + 1;

            // Apply button
            if (GUI.Button(new Rect(Screen.width - menuWidth + 15, 80 + offset, 65, 25), "APPLY"))
            {

                // Apply algorithm and build procedural texture
                filterManager.applyGaussianBlur(gaussAmount, kernelSize, nullVec, endPoint);
                terrain.applyProceduralTex(true, sandColor, sandLimit, sandStrength, sandCoverage, true, grassColor, grassStrength, true, snowColor, snowLimit, snowStrength, snowCoverage, true, rockColor, slopeLimit, slopeStrength, noiseTexValue);
                terrain.build();
            }

            // Flag for keeping the algorithm active through the on-the-fly generation of terrain
            gaussFlag = GUI.Toggle(new Rect(Screen.width - menuWidth + 95, 80 + offset, 95, 25), gaussFlag, "  Keep Active");

            offset = 325; // Y offset value

            // LOW PASS FILTER / SPIKES REMOVAL MENU

            // Background box
            GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 95), "Low-Pass Filter / Spikes Removal");

            // Strength of the algorithm linked to a slider
            spikesStrength = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 85, 35 + offset, menuWidth - 90 - rightOffset, 30), spikesStrength, 0.9f, 1.0f);

            // Name label
            GUI.Label(new Rect(Screen.width - menuWidth + 10, 64 - 35 + offset, menuWidth - rightOffset, 30), "Strength");

            // Apply button
            if (GUI.Button(new Rect(Screen.width - menuWidth + 15, 60 + offset, 65, 25), "APPLY"))
            {

                // Apply algorithm and reset procedural texture
                filterManager.applySpikesFilter(1.0f - spikesStrength);
                terrain.applyProceduralTex(true, sandColor, sandLimit, sandStrength, sandCoverage, true, grassColor, grassStrength, true, snowColor, snowLimit, snowStrength, snowCoverage, true, rockColor, slopeLimit, slopeStrength, noiseTexValue);
                terrain.build();
            }





        }

        //Erosion menu settings
        if (erosionMenuFlag)
        {

            // THERMAL EROSION MENU

            offset = 135; // Y offset value

            // Background box
            GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 140), "Thermal Erosion");

            // Iterations copy
            thermalIterationsTemp = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 105, 35 + offset, menuWidth - 110 - rightOffset, 20), thermalIterationsTemp, 20, 1500);

            // Iterations name label
            GUI.Label(new Rect(Screen.width - menuWidth + 10, 64 - 35 + offset, menuWidth - rightOffset, 30), "Iter. per step");

            // Converting to integer values
            thermalIterations = (int)thermalIterationsTemp;

            // Thermal slope limit linked to slider
            thermalSlope = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 105, 55 + offset, menuWidth - 110 - rightOffset, 20), thermalSlope, 0.002f, 0.00001f);

            // Thermal slope name label
            GUI.Label(new Rect(Screen.width - menuWidth + 10, 84 - 35 + offset, menuWidth - rightOffset, 30), "Temperature");

            // Thermal strength linked to slider, values are inverted. Low strength => high terrain density and viceversa
            thermalC = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 105, 75 + offset, menuWidth - 110 - rightOffset, 30), thermalC, 0.5f, 0.01f);

            // Thermal strength name label as terrain density
            GUI.Label(new Rect(Screen.width - menuWidth + 10, 104 - 35 + offset, menuWidth - rightOffset, 30), "Terrain Density");

            // Apply / stop label according to the thermal generation flag.
            if (thermalGenFlag == false)
            {
                if (GUI.Button(new Rect(Screen.width - menuWidth + 15, 100 + offset, 65, 25), "APPLY"))
                {
                    // Set flag to true
                    thermalGenFlag = true;
                }
            }
            else
            {
                if (GUI.Button(new Rect(Screen.width - menuWidth + 15, 100 + offset, 65, 25), "STOP"))
                {

                    // Setting the flag to false and applying a low pass filter to remove any artifacts
                    thermalGenFlag = false;
                    filterManager.applySpikesFilter(0.005f);

                    // Resetting the texture and refreshing mesh
                    terrain.applyProceduralTex(true, sandColor, sandLimit, sandStrength, sandCoverage, true, grassColor, grassStrength, true, snowColor, snowLimit, snowStrength, snowCoverage, true, rockColor, slopeLimit, slopeStrength, noiseTexValue);
                    terrain.build();
                }
            }

            // Flag for applying one algorithm pass to the on-the-fly generation of terrain
            thermalFlag = GUI.Toggle(new Rect(Screen.width - menuWidth + 95, 100 + offset, 95, 25), thermalFlag, "  Keep Active");

            offset = 280; // Y offset value 


            //HYDRAULIC EROSION MENU

            // Disabling it if the program is in flight mode for an increased performance
            if (!flightModeFlag)
            {

                // Background box
                GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 380), "Hydraulic Erosion");

                // Rain parameters title
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 64 - 35 + offset, menuWidth - rightOffset, 30), "              Rain Parameters");

                offset = 300; // Y offset value

                // Rain frequency copy linked to slider
                rainFreqTemp = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 35 + offset, menuWidth - 115 - rightOffset, 20), rainFreqTemp, 0.0f, 800.0f);

                // Name label
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 64 - 35 + offset, menuWidth - rightOffset, 20), "Frequency");

                // Assigning the integer value
                rainFreq = (int)rainFreqTemp;


                // Rain amount value linked to slider
                rainAmount = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 55 + offset, menuWidth - 115 - rightOffset, 20), rainAmount, 0.0f, 0.1f);

                // Name labels
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 84 - 35 + offset, menuWidth - rightOffset, 30), "Amount");
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 104 - 35 + offset, menuWidth - rightOffset, 30), "              Fluid Parameters");

                offset = 320; // Y offset value

                // Viscosity value linked to slider and label name
                waterViscosity = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 75 + offset, menuWidth - 115 - rightOffset, 20), waterViscosity, 0.0f, 0.1f);
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 104 - 35 + offset, menuWidth - rightOffset, 30), "Viscosity");

                // Deposition value linked to slider and label name
                waterDeposition = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 95 + offset, menuWidth - 115 - rightOffset, 20), waterDeposition, 0.0f, 0.1f);
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 124 - 35 + offset, menuWidth - rightOffset, 30), "Deposition rate");

                // Evaporation value linked to slider and label name
                waterEvap = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 115 + offset, menuWidth - 115 - rightOffset, 20), waterEvap, 0.0f, 0.1f);
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 144 - 35 + offset, menuWidth - rightOffset, 30), "Evaporation rate");


                // Ground parameters title
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 164 - 35 + offset, menuWidth - rightOffset, 30), "             Ground Parameters");

                offset = 340; // Y offset value

                // Density value linked to slider and label name
                waterTerrDens = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 135 + offset, menuWidth - 115 - rightOffset, 20), waterTerrDens, 0.1f, 0.0f);
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 164 - 35 + offset, menuWidth - rightOffset, 30), "Terrain Density");

                // Gravity value linked to slider and label name
                waterGrav = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 155 + offset, menuWidth - 115 - rightOffset, 20), waterGrav, 0.0f, 15.0f);
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 184 - 35 + offset, menuWidth - rightOffset, 30), "Gravity");


                // Wind parameters title
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 204 - 35 + offset, menuWidth - rightOffset, 20), "             Wind Parameters");

                offset = 360; // Y offset value

                // Copies of values for later check of any changes
                float NStemp = windStrength.x;
                float WEtemp = windStrength.y;
                float Ctemp = windCover;
                bool Atemp = windAltFlag;

                // NS wind strength linked to slider // Name label
                windStrength.x = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 175 + offset, menuWidth - 115 - rightOffset, 20), windStrength.x, -1.0f, 1.0f);
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 204 - 35 + offset, menuWidth - rightOffset, 20), "North <-> South");

                // WE wind strength linked to slider // Name label
                windStrength.y = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 195 + offset, menuWidth - 115 - rightOffset, 20), windStrength.y, -1.0f, 1.0f);
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 224 - 35 + offset, menuWidth - rightOffset, 20), "West <-> East");

                // wind coverage linked to slider // Name label
                windCover = GUI.HorizontalSlider(new Rect(Screen.width - menuWidth + 110, 215 + offset, menuWidth - 115 - rightOffset, 20), windCover, 0.0f, 1.0f);
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 244 - 35 + offset, menuWidth - rightOffset, 20), "Coverage");


                // Flag for using altitude linked to toggle button
                windAltFlag = GUI.Toggle(new Rect(Screen.width - menuWidth + 15, 230 + offset, 175, 25), windAltFlag, "  Increase with altitude");

                // Check for changes and, if any, change the wind parameters of the mesh
                if (NStemp != windStrength.x || WEtemp != windStrength.y || Ctemp != windCover || Atemp != windAltFlag) erosionManager.setWindParam(windStrength, windCover, windAltFlag);


                // Show/Hide button for the hydraulic erosion
                if (!waterMenuFlag)
                {
                    if (GUI.Button(new Rect(Screen.width - menuWidth + 15, 265 + offset - 50, 145, 25), "SHOW CONTROLS"))
                        waterMenuFlag = true;
                }
                else
                {
                    if (GUI.Button(new Rect(Screen.width - menuWidth + 15, 265 + offset - 50, 145, 25), "HIDE CONTROLS"))
                        waterMenuFlag = false;
                }

            }

            // If program is in flight mode, display message of unavailability
            else
            {
                // Background box and label
                GUI.Box(new Rect(Screen.width - menuWidth, offset, menuWidth - rightOffset, 57), "Hydraulic Erosion");
                GUI.Label(new Rect(Screen.width - menuWidth + 10, 64 - 35 + offset, menuWidth - rightOffset, 30), "Unavailable in 'Flight' mode.");
            }

            // Hydraulic erosion controls menu
            if (waterMenuFlag)
            {

                //Background box
                GUI.Box(new Rect(Screen.width - menuWidth * 2 + 90, offset - 80, menuWidth - rightOffset - 90, 125), "Controls");

                // GENERATE/STOP button
                if (!waterGenFlag)
                {
                    if (GUI.Button(new Rect(Screen.width - menuWidth * 2 + 105, offset - 50, menuWidth - rightOffset - 120, 25), "GENERATE"))
                    {
                        waterGenFlag = true;
                    }
                }
                else
                {
                    if (GUI.Button(new Rect(Screen.width - menuWidth * 2 + 105, offset - 50, menuWidth - rightOffset - 120, 25), "STOP"))
                    {
                        waterGenFlag = false;
                    }
                }

                // START/STOP RAIN button
                if (rainFlag)
                {
                    if (GUI.Button(new Rect(Screen.width - menuWidth * 2 + 105, offset - 20, menuWidth - rightOffset - 120, 25), "STOP RAIN"))
                    {
                        rainFlag = false;
                    }
                }
                else
                    if (GUI.Button(new Rect(Screen.width - menuWidth * 2 + 105, offset - 20, menuWidth - rightOffset - 120, 25), "START RAIN"))
                {
                    rainFlag = true;
                }


                // Clear hydraulic erosion maps
                if (GUI.Button(new Rect(Screen.width - menuWidth * 2 + 105, offset + 10, menuWidth - rightOffset - 120, 25), "CLEAR ALL"))
                {

                    //Reinitialise erosion maps
                    erosionManager.initHydraulicMaps();
                    //Refresh mesh
                    terrain.build();
                }


            }
        }

        // If flight mode is active, 
        // set hydraulic erosion menu flags to false
        if (flightModeFlag)
        {
            waterMenuFlag = false;
            waterGenFlag = false;
        }
    }

    public void destroyMeshes()
    {

        //Meshes destructor
        Mesh[] myMesh = terrain.myMesh;
        Mesh[] myWaterMesh = terrain.myWaterMesh;

        //Adam J
        //missing destruction of terrain and water GameObject
        //I believe that destruction of meshes is not necessary


        for (int i = 0; i < 4; i++)
        {

            Destroy(myMesh[i]);
            Destroy(myWaterMesh[i]);

            Destroy(terrain.myTerrain[i]);
            Destroy(terrain.myWater[i]);
        }
    }

}

