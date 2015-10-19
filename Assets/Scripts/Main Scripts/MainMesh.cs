using UnityEngine;
using System.Collections;

//Getting the necessary Unity components for the rendering of the mesh
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]

//Main Mesh class, storing all mesh rendering 
//processes and procedural methods
public class MainMesh : MonoBehaviour {
	
	//Terrain object and mesh
	GameObject[] myTerrain;
	Mesh[]     myMesh;
	
	//Water object and mesh
	GameObject[] myWater;
	Mesh[]       myWaterMesh;
	
	//Textures
	Texture2D heightMap;
	Texture2D[] waterMap;
	Texture2D proceduralTexture;
	
	//Mesh limits 
	public Vector3 endOf;
	public Vector3 startOf;
	public Vector3 middleOf;
	
	//Terrain sizes
	public int terrainSize;
	int patchSize;
	int patchCount;
	int meshSize;
	int individualMeshSize;
	
	//Scaling vectors
	public Vector3 scaleTerrain = new Vector3(800, 100, 800);
	Vector2 uvScale;
	Vector3 vertsScale;
	Vector2 waterUvScale;
	
	//Meshes data
	Vector3[][]  verticesWater;
	public Vector3[,]   vertices;
	Vector3[][]  verticesOut;
	
	Vector2[]    waterUv;
	Vector2[][]  uv;
	int[]        triangles;
	Vector3[][]  normals;
	
	//Blurring kernel
	float[,] gaussianKernel;
	
	//On-the-fly generation offsets
	int blurOffset;
	int thermalOffset;
	
	//Hydraulic erosion maps
	float[,]   W; //water
	float[,]   S; //sediment
	Vector2[,] V; //velocity
	Vector4[,] F; //outflow flux
	
	//Diamond-square algorithm welding flags
	bool a_corner, b_corner, c_corner, d_corner;
	
	//Wind parameters
	Vector2 windStrength = new Vector2(0.0f, 0.0f);
	float windCoverage = 1.0f;
	bool windAltitude = true;

	
	
	
	//_________________________________
	//CLASS AND OBJECTS INITIALISATION
	//---------------------------------
	
	public  void initialise          (int patch_size, int patch_count) {
		
		//Class Constructor
		//Initialising all data and setting variables to null value
		
		
		patchSize  = patch_size;  //the size of a single patch of terrain
		patchCount = patch_count;//the number of patches valueXvalue
		
		//The terrain size is built with a row of quads between the patches, to avoid vertex overlapping
		//while the meshSize is the renderable 2^i X 2^i mesh.
		terrainSize = patchSize*patchCount + patchCount;
		meshSize    = patchSize*patchCount + 1;
		individualMeshSize = meshSize/2 + 1;
		
		//Textures initialisation
		heightMap         = new Texture2D(meshSize, meshSize, TextureFormat.RGBA32, false);
		waterMap          = new Texture2D[4];
		proceduralTexture = new Texture2D(terrainSize, terrainSize);
		
		//Meshes initialisation. 4 Meshes are built for each due to 
		//Unity's inability to hold more than 65 000 vertices/mesh
		myMesh      = new Mesh[4];
		myWaterMesh = new Mesh[4];
		
		
		
		//The offset considered when generating on-the-fly geometry
		blurOffset = 2;
		thermalOffset = 5;
		
		//Init hydraulic erosion maps
		W = new float[terrainSize, terrainSize]; //water map
		S = new float[terrainSize, terrainSize]; //sediment map
		V = new Vector2[terrainSize, terrainSize]; //velocity map
		F = new Vector4[terrainSize,terrainSize]; //outflow map
	
		//Mesh values
		int numVerts     = (individualMeshSize + 1)*(individualMeshSize + 1);
		int numQuads     = (individualMeshSize - 1)*(individualMeshSize - 1);
		int numTriangles = numQuads*2;
		
		//Mesh data
		vertices    = new Vector3[terrainSize, terrainSize];
		verticesOut = new Vector3[4][];
		normals     = new Vector3[4][];
		uv          = new Vector2[4][];
		triangles   = new int[numTriangles*3];
		
		//Water mesh data
		waterUv     = new Vector2[numVerts];
		verticesWater= new Vector3[4][];
		
		//Initialising meshes data
		for (int i=0; i<4; i++) {
			verticesOut[i]   = new Vector3[numVerts];
			uv[i]            = new Vector2[numVerts];
			verticesWater[i] = new Vector3[numVerts];
			normals[i]       = new Vector3[numVerts];
			waterMap[i]      = new Texture2D(individualMeshSize, individualMeshSize, TextureFormat.RGBA32, false);
		}
		
		//Initialising scaling factors according to mesh sizes
		uvScale    = new Vector2(1.0f/(terrainSize-1), 1.0f/(terrainSize-1));
		waterUvScale    = new Vector2(1.0f/(individualMeshSize-1), 1.0f/(individualMeshSize-1));
		vertsScale = new Vector3(scaleTerrain.x / (terrainSize-1), scaleTerrain.y, scaleTerrain.z / (terrainSize-1));
	
		//Initialising primary height map to null
		for (int z = 0; z < terrainSize; z++) 
				for (int x = 0; x < terrainSize; x++) 
					vertices   [x,z] = new Vector3(x, 0, z);
		
		
		int meshIndex = 0;
		
		//Build vertices, normals and uv's for each of the four meshes
		for (int i=0; i<2; i++) {
			for (int j=0; j<2; j++) {
				
				for (int z = 0; z < individualMeshSize; z++) {
					for (int x = 0; x < individualMeshSize; x++) {

							verticesOut[meshIndex][(z*individualMeshSize) + x] = vertices[x + individualMeshSize*j - j, z + individualMeshSize*i - i];
							verticesOut[meshIndex][(z*individualMeshSize) + x].Scale(vertsScale);
							uv         [meshIndex][(z*individualMeshSize) + x] = Vector2.Scale(new Vector2(x + individualMeshSize*j - j, z + individualMeshSize*i - i), uvScale);
							waterUv    [(z*individualMeshSize) + x]            = Vector2.Scale(new Vector2(x,z), waterUvScale);
							normals    [meshIndex][(z*individualMeshSize) + x] = new Vector3(0,1,0);
					}
				}
				++meshIndex;
			}
		}

		//Build triangles, used for both meshes
		int index = 0;
		for (int z = 0; z < individualMeshSize - 1; z++) {
			for (int x = 0; x < individualMeshSize - 1; x++) {
					
				triangles[index++] = (z     * (individualMeshSize)) + x;
				triangles[index++] = ((z+1) * (individualMeshSize)) + x;
				triangles[index++] = (z     * (individualMeshSize)) + x + 1;
	
				triangles[index++] = ((z+1) * (individualMeshSize)) + x;
				triangles[index++] = ((z+1) * (individualMeshSize)) + x + 1;
				triangles[index++] = (z     * (individualMeshSize)) + x + 1;
			}
		}
		
		//Call function to assign data structures to the meshes
		initMeshes();
		//Initialise hydraulic erosion model's maps
		initHydraulicMaps();
	}

	public  void build               () {
		
		//Function called to update the renderables when changes occur
		
		//Initialise scaling values according to terrain size and user-controlled sizing
		vertsScale = new Vector3(scaleTerrain.x / (terrainSize-1), scaleTerrain.y, scaleTerrain.z / (terrainSize-1));
	
		int meshIndex = 0;
		
		//Rebuild mesh data
		for (int i=0; i<2; i++) {
			for (int j=0; j<2; j++) {
				
				for (int z = 0; z < individualMeshSize; z++) {
					for (int x = 0; x < individualMeshSize; x++) {
				
						//Set output vertices
						verticesOut[meshIndex][(z*individualMeshSize) + x] = vertices[x + individualMeshSize*j - j, z + individualMeshSize*i - i];
						verticesOut[meshIndex][(z*individualMeshSize) + x].Scale(vertsScale);
						
						//Set normals
						normals[meshIndex][(z*individualMeshSize) + x] = getNormalAt(vertices[x + individualMeshSize*j - j, z + individualMeshSize*i - i], x + individualMeshSize*j - j, z + individualMeshSize*i - i);
						
						//Set heightmap texture pixel
						float this_color = vertices[x + individualMeshSize*j, z + individualMeshSize*i].y;
						heightMap.SetPixel(x + individualMeshSize*j, z + individualMeshSize*i, new Color(this_color, this_color, this_color));
						
						//Set water data if water is present
						if (W[x + individualMeshSize*j - j, z + individualMeshSize*i - i] > 0.0001f) {
							int tex = 15;
							
							//Set transparency
							float alpha = W[x + individualMeshSize*j - j, z + individualMeshSize*i - i]*120;
							if (alpha > 0.9f) alpha = 1.0f;
							
							//Set water texture pixel
							waterMap[meshIndex].SetPixel(x,z, new Color(this_color * 1-W[x + individualMeshSize*j - j, z + individualMeshSize*i - i]*tex,this_color * 1-W[x + individualMeshSize*j - j, z + individualMeshSize*i - i]*tex,1,alpha));
							
							//Set water output vertex
							verticesWater[meshIndex][(z*individualMeshSize) + x] = vertices[x + individualMeshSize*j - j, z + individualMeshSize*i - i];
							verticesWater[meshIndex][(z*individualMeshSize) + x].y += W[x + individualMeshSize*j - j, z + individualMeshSize*i - i];
							verticesWater[meshIndex][(z*individualMeshSize) + x].Scale(vertsScale);
						}
						else {
							//Set water vertex just under the mesh
							verticesWater[meshIndex][(z*individualMeshSize) + x] = vertices[x + individualMeshSize*j - j, z + individualMeshSize*i - i];
							verticesWater[meshIndex][(z*individualMeshSize) + x].y -= 0.02f;
							verticesWater[meshIndex][(z*individualMeshSize) + x].Scale( vertsScale);
						}
					}
				}
				++meshIndex;
			}
		}	
		
		//Apply changes to heighmap teture
		heightMap.Apply();
		
		//Assign data structures to the meshes
		for (int i=0; i<4; i++) {
			myMesh[i].vertices = verticesOut[i];
			myMesh[i].normals = normals[i];
			myMesh[i].RecalculateBounds();
			
			waterMap[i].Apply();
			
			myWaterMesh[i].vertices = verticesWater[i];
			myWaterMesh[i].RecalculateNormals();
			myWaterMesh[i].RecalculateBounds();
		}
		//Set bounds
		endOf   = verticesOut[3][individualMeshSize*individualMeshSize - 1];
		startOf = verticesOut[0][0];	
		middleOf = (startOf + endOf)/2;
	}
	
	private void initMeshes          () {
		
		//Function to assign data structures to meshes and link to Renderer
		
		
		//Create game objects
		myTerrain   = new GameObject[4];
		myWater     = new GameObject[4];

		//Initialise terrain and water meshes and link to the data structures
		for (int i=0; i<4; i++) {
			
			//TERRAIN
			myMesh[i]         = new Mesh();
			myTerrain[i]      = new GameObject();
			myTerrain[i].name = "Terrain";
			
			myTerrain[i].AddComponent<MeshFilter>  ().mesh = myMesh[i];
			myTerrain[i].AddComponent<MeshRenderer>();
			
			myMesh[i].vertices = verticesOut[i];
			myMesh[i].triangles = triangles;
			myMesh[i].uv = uv[i];
			myMesh[i].normals = normals[i];
			
			myTerrain[i].GetComponent<Renderer>().material.mainTexture = heightMap;
			myTerrain[i].GetComponent<Renderer>().material.mainTexture.wrapMode = TextureWrapMode.Clamp;
			
			//WATER
			myWaterMesh[i]  = new Mesh();
			myWater[i]      = new GameObject();
			myWater[i].name = "Water";
			
			myWater[i].AddComponent<MeshFilter>  ().mesh = myWaterMesh[i];
			myWater[i].AddComponent<MeshRenderer>();

			myWaterMesh[i].vertices  = verticesWater[i];
			myWaterMesh[i].triangles = triangles;
			myWaterMesh[i].uv        = waterUv;
			myWaterMesh[i].RecalculateNormals();
			
			//28 APRIL 2014  ||  18:30 pm  ||   OCCURED CHANGE DUE TO INCONSISTENCY OF STANDALONE SHADER COMPILER
			//SHADER HAS BEEN ADDED TO MATERIAL IN A NEW RESOURCES FOLDER INSIDE THE ASSETS
			//MATERIAL IS ATTACHED AS FOLLOWS:
		
			//Apply texture and transparent shader 
			//myWater[i].renderer.material.shader = Shader.Find( "Transparent/BumpedDiffuse" );
			myWater[i].GetComponent<Renderer>().material = Resources.Load("Watermat", typeof(Material)) as Material;
			
			myWater[i].GetComponent<Renderer>().material.mainTexture = waterMap[i];
			myWater[i].GetComponent<Renderer>().material.mainTexture.wrapMode = TextureWrapMode.Clamp;
		}
	}
	
	public void destroyMeshes        () {
		
		//Meshes destructor
		
		for (int i=0; i<4; i++) {
			
			Destroy(myMesh[i]);
			Destroy(myWaterMesh[i]);
		}
	}

    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^


    //***********<RIVER...>*************
    /*
    public void GenerateRiver()
    {

        int riverWidth = 15;
        for (int x = 0; x < terrainSize; x++)
        {
            for (int z = terrainSize/2 - riverWidth; z < terrainSize/2 + riverWidth; z++)
            {
                float depth = (float)System.Math.Abs((float)riverWidth - (float)System.Math.Abs(terrainSize / 2 - z))/riverWidth;
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


    //***********<...RIVER>*************

    //_________________________________
    //PROCEDURAL GENERATION ALGORITHMS
    //---------------------------------


    //DIAMOND-SQUARE FRACTAL DISPLACEMENT MODEL

    public void applyDiamondSquare  (float scale) {
		
		//Diamond-Square method's patch iterator 
		
		//Iterate through patches
		for (int x=0; x<patchCount; x++) {
			for (int z=0; z<patchCount; z++) {
				
				//Initialise welding flags
				a_corner = b_corner = c_corner = d_corner = true;
				
				if (x>0) { a_corner = false; d_corner = false; }
				if (z>0) { a_corner = false; b_corner = false; }
				
				//Set starting position and call main algorithm
				Vector3 temp = new Vector3(x*patchSize + x, 0, z*patchSize + z);
				initDiamondSquare(temp, scale);
			}
		}
	}
	
	private void initDiamondSquare   (Vector3 start, float scale) {
		
		//Main diamond-square algorithm
		
		
		//Size of step at iteration 0
		int stepSize = patchSize;
		
		//Random numbers limit (-value, +value)
		float rand_value1 = 0.50f;
		float rand_value2 = 0.40f;
	
		int x = 0;
		int y = 0;
		
		float offset;
		
		int start_x = (int)start.x;
		int start_z = (int)start.z;
	
		
		//Displace the corners
		if (a_corner) {
		offset = Random.Range(0f,1f)*scale;
		vertices[start_x,start_z].y = offset;
		}
	
		if (b_corner) {
		offset = Random.Range(0f,1f)*scale;
		vertices[start_x+stepSize,start_z].y = offset;
		}
	
		if (c_corner) {
		offset = Random.Range(0f,1f)*scale;
		vertices[start_x+stepSize, start_z+stepSize].y = offset;
		}
	
		if (d_corner) {
		offset = Random.Range(0f,1f)*scale;
		vertices[start_x,start_z+stepSize].y = offset;
		}
		
		
		//Start the main displacement loop
		while (stepSize > 1) {
			
			//Halving the resolution each step
			int half_step = stepSize/2;
			
			//Square step
			for (x = start_x + half_step; x < start_x + patchSize + half_step; x = x + stepSize) {
				for (y = start_z + half_step; y < start_z + patchSize + half_step; y = y + stepSize) {
					stepSquare(x, y, rand_value1, scale, half_step);
				}
			}
			
			//Diamond step
			for (x = start_x + half_step; x < start_x + patchSize + half_step; x = x + stepSize) {
				for (y = start_z + half_step; y < start_z + patchSize + half_step; y = y + stepSize) {
					stepDiamond(x, y, rand_value2, scale, half_step, start);
				}
			}
			
			//Halving the resolution and the roughness parameter
			stepSize = stepSize/2;
			scale /=2;
		}
		
		
		//Copy margin values to neighbouring vertices belonging to nearby pathes 
		//to avoid unwanted artifacts/seams between patches
		
		//west
		if (start_x != 0)                                      
			for (int i=start_z; i<start_z + patchSize + 1; i++)
				vertices[start_x-1, i].y = vertices[start_x, i].y;
		//south
		if (start_z != 0)                                      
			for (int i=start_x; i<start_x + patchSize + 1; i++)
				vertices[i, start_z-1].y = vertices[i, start_z].y;
		//east
		if (start_x + patchSize != terrainSize-1)              
			for (int i=start_z; i<start_z + patchSize + 1; i++) 
				vertices[start_x+patchSize+1, i].y = vertices[start_x+patchSize, i].y;
		//north
		if (start_z + patchSize != terrainSize-1)             
			for (int i=start_x; i<start_x + patchSize + 1; i++) 
				vertices[i, start_z+patchSize+1].y = vertices[i, start_z+patchSize].y;
	}
		
	private void stepSquare          (int x, int y, float rand_value, float scale, int half_step) {
		
		//Get corner valuesorners
		float a = vertices[x - half_step, y - half_step].y;
		float b = vertices[x + half_step, y - half_step].y;
		float c = vertices[x - half_step, y + half_step].y;
		float d = vertices[x + half_step, y + half_step].y;
		
		//Set new averaged and randomised value of the centre
		vertices[x,y].y = (a+b+c+d)/4.0f + Random.Range(-rand_value, rand_value)*scale;
	}

	private void stepDiamond         (int x, int y, float rand_value, float scale, int half_step, Vector3 start) {
		
		//Get side points (diamond-shaped)
		float a = vertices[x - half_step, y - half_step].y;
		float b = vertices[x + half_step, y - half_step].y;
		float c = vertices[x - half_step, y + half_step].y;
		float d = vertices[x + half_step, y + half_step].y;
		
		float offset;
		
		//Set the final offset value according to the patch-welding flags
		
		if (a_corner || d_corner || x-half_step!=(int)start.x) {
			offset = (a+c)/2.0f + Random.Range(-rand_value, rand_value)*scale;
			vertices[x-half_step, y].y = offset;
		}
		
		if (c_corner || d_corner || y+half_step!=(int)start.z + patchSize) {
			offset = (c+d)/2.0f + Random.Range(-rand_value, rand_value)*scale;
			vertices[x, y+half_step].y = offset;
		}
		
		if (b_corner || c_corner || x+half_step!=(int)start.x + patchSize) {
			offset = (b+d)/2.0f + Random.Range(-rand_value, rand_value)*scale;
			vertices[x+half_step, y].y = offset;
		}
		
		if (a_corner || b_corner || y-half_step!=(int)start.z) {
			offset = (a+b)/2.0f + Random.Range(-rand_value, rand_value)*scale;
			vertices[x, y-half_step].y = offset;
		}
	}
	//^^^^^^^^^^^^^^^^^^^^^^^^
	
	
	//HYDRAULIC EROSION MODEL
	
	public void initHydraulicMaps    () {
	
		//Assign starting values to the hydraulic erosion maps
		for (int x=0; x<terrainSize; x++) {
			for (int y=0; y<terrainSize; y++) {

				W[x,y] = 0.0f; //water map
				S[x,y] = 0;   //sediment map
				V[x,y] = new Vector2(0,0);      //velocity map
				F[x,y] = new Vector4(0,0,0,0); //outflow flux map (left, right, bottom, top)
			}
		}
	}
	
	public void applyHydraulicErosion(bool rainFlag, int rainIntensity, float Kc, float Ks, float Kd, float Ke, float Kr, float G) {
		
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
			for (int k=0; k<rainIntensity; k++) {
			
			//Pick random spot
			int x = Random.Range(0, terrainSize-1);
			int y = Random.Range(0, terrainSize-1);
			
			//Add water to spot
			W[x,y] += Kr;
		}
		
		// Initialise wind direction flags
		int SNflag = 0;
		int WEflag = 0;
		
		// Set wind direction flags : 1 if > 0;  0 if == 0;  1 if > 0;
		if (windStrength.x < 0) SNflag = -1;
		else if (windStrength.x > 0) SNflag = 1;
		if (windStrength.y < 0) WEflag = -1;
		else if (windStrength.y > 0) WEflag = 1;
		
		Debug.Log(SNflag);
		
		
		//Step 2: UPDATING OUTFLOW FLUX MAP
		for (int xVal = 0; xVal<terrainSize; xVal++) {
			for (int yVal = 0; yVal<terrainSize; yVal++) {
				
				// Initialise flux variables
				float fluxL, fluxB, fluxR, fluxT;
				
				// Get current point height
				float currentH = vertices[xVal,yVal].y + W[xVal,yVal] + S[xVal,yVal];
				
				// Retrieve potential wind forces at this cell over the X and Y directions
				// These values are to be further checked for applicability
				float windPotentialX = windValue(windStrength.x, currentH, xVal, yVal);
				float windPotentialY = windValue(windStrength.y, currentH, xVal, yVal);
					
				
				//LEFT OUTFLOW
				if (xVal>0) {
					
					//Get total height and height difference with neigbour
					float hN     = vertices[xVal-1,yVal].y + W[xVal-1,yVal] + S[xVal-1,yVal];
					float deltaH = currentH - hN;
					
					//Initialise wind force
					float windForce = 0;
					
					//Check against slopes and for compatibility with the user's windCoverage variable 
					//And apply as suited
					if (WEflag == 1  && deltaH>0.0f - windCoverage) windForce = -1 * windPotentialY;
					else 
					if (WEflag == -1 && deltaH<0.0f + windCoverage) windForce = -1 * windPotentialY;
							
					//Clamp value
					fluxL = Mathf.Max(0.0f, F[xVal,yVal].x + T * A * ((G * deltaH + windForce) / L));
				}
				else 
					fluxL = 0;
					
				
				//RIGHT OUTFLOW
				if (xVal<terrainSize-1) {
					
					//Get total height and height difference with neigbour
					float hN     = vertices[xVal+1,yVal].y + W[xVal+1,yVal] + S[xVal+1,yVal];
					float deltaH = currentH - hN;
					
					//Initialise wind force
					float windForce = 0;
					
					//Check against slopes and for compatibility with the user's windCoverage variable 
					//And apply as suited
					if (WEflag == 1  && deltaH<0.0f + windCoverage) windForce = windPotentialY;
					else  
					if (WEflag == -1 && deltaH>0.0f - windCoverage) windForce = windPotentialY;
					
					//Clamp value
					fluxR = Mathf.Max(0.0f, F[xVal,yVal].y + T * A * ((G * deltaH + windForce) / L));		
				}
				else 
					fluxR = 0;
						
				
				// BOTTOM OUTFLOW
				if (yVal>0) {
					
					//Get total height and height difference with neigbour
					float hN     = vertices[xVal,yVal-1].y + W[xVal,yVal-1] + S[xVal,yVal-1];
					float deltaH = currentH - hN;
					
					//Initialise wind force
					float windForce = 0;
					
					//Check against slopes and for compatibility with the user's windCoverage variable 
					//And apply as suited
					if (SNflag == 1  && (deltaH>0.0f - windCoverage)) windForce = -1 * windPotentialX;
					//else 
					if (SNflag == -1 && (deltaH<0.0f + windCoverage)) windForce = -1 * windPotentialX;
					
					//Clamp value
					fluxB = Mathf.Max(0.0f, F[xVal,yVal].z + T * A * ((G * deltaH + windForce) / L));		
				}
				else 
					fluxB = 0;
						
				
				// TOP OUTFLOW
				if (yVal<terrainSize-1) {	
					
					//Get total height and height difference with neigbour
					float hN     = vertices[xVal,yVal+1].y + W[xVal,yVal+1] + S[xVal,yVal+1];
					float deltaH = currentH - hN;
					
					//Initialise wind force
					float windForce = 0;
					
					//Check against slopes and for compatibility with the user's windCoverage variable 
					//And apply as suited
					if (SNflag == 1  && (deltaH<0.0f + windCoverage)) windForce = windPotentialX;
					//else 
					if (SNflag == -1 && (deltaH>0.0f - windCoverage)) windForce = windPotentialX;
					
					//Clamp value
					fluxT = Mathf.Max(0.0f, F[xVal,yVal].w + T * A * ((G * deltaH + windForce) / L));	
				}
				else 
					fluxT = 0;
				
				
				
				//If the sum of the outflow flux exceeds the water amount of the cell,
				//the flux value will be scaled down by a factor K to avoid negative
				//updated water height
				
				float K = Mathf.Min(1.0f, (W[xVal,yVal] *L*L) / ((fluxL + fluxR + fluxT + fluxB) * T) );
				
				if ((fluxL + fluxR + fluxT + fluxB) * T > W[xVal,yVal]) {
					
					F[xVal,yVal].x = fluxL * K;
					F[xVal,yVal].y = fluxR * K;
					F[xVal,yVal].z = fluxB * K;
					F[xVal,yVal].w = fluxT * K;
				}
				else {
					
					F[xVal,yVal].x = fluxL;
					F[xVal,yVal].y = fluxR;
					F[xVal,yVal].z = fluxB;
					F[xVal,yVal].w = fluxT;	
				}
			}
		}
		
		
		// VELOCITY UPDATE AND EROSION-DEPOSITION STEP
		for (int xVal = 0; xVal<terrainSize; xVal++) {
			for (int yVal = 0; yVal<terrainSize; yVal++) {
				
				//Get inflow and outflow data
				//Clamping to mesh size
				float inL, inR, inB, inT;
				if (xVal>0) inL = F[xVal-1,yVal].y;
				else        inL = 0;
				if (yVal>0) inB = F[xVal,yVal-1].w;
				else        inB = 0;
				if (xVal<terrainSize-1) inR = F[xVal+1,yVal].x;
				else                    inR = 0;
				if (yVal<terrainSize-1) inT = F[xVal,yVal+1].z;
				else                    inT = 0;
				
				//Compute inflow and outflow for velocity update
				float fluxIN  = inL + inR + inB + inT;
				float fluxOUT = F[xVal,yVal].x + F[xVal,yVal].y + F[xVal,yVal].z + F[xVal,yVal].w;
				
				//V is net volume change for the water over time
				float V = Mathf.Max(0.0f, T * (fluxIN - fluxOUT));
				
				//The water is updated according to the volume change 
				//and cross-sectional area of pipe
				W[xVal,yVal] += (V / (L*L));
				
				//Step 3: UPDATING THE VELOCITY FIELD
				Vector2 velocityField;
				velocityField.x = inL - F[xVal,yVal].x + F[xVal,yVal].y - inR;
				velocityField.y = inT - F[xVal,yVal].w + F[xVal,yVal].z - inB;
				velocityField *= 0.5f;
				
				// Compute maximum sediment capacity
				float C = Kc * velocityField.magnitude * findSlope(xVal, yVal);
				
				
				//Step 4: EROSION AND DEPOSITION STEP
				
				//If sediment transport capacity greater than sediment in water 
				//remove some land and add to sediment scaled by dissolving constant (Ks)
				//Else if sediment transport capacity less than sediment in water 
				//remove some sediment and add to land scaled by deposition constant (Kd)
				
				float KS = Mathf.Max(0, Ks * (C - S[xVal,yVal]));
				float KD = Mathf.Max(0, Kd * (S[xVal,yVal] - C));
				
				//Sediment capacity check
				if((C > S[xVal,yVal]) && (vertices[xVal,yVal].y - KS > 0.0f) && (W[xVal,yVal] > S[xVal,yVal]))
				{
					vertices[xVal,yVal].y -= KS;
					S[xVal,yVal] += KS;
				}
				else
				{
					vertices[xVal,yVal].y += KD;
					S[xVal,yVal] -= KD;
				}
				
				//Step 5: SEDIMENT MAP UPDATE
				S[xVal,yVal] = S[(int)(xVal-velocityField.x ), (int)(yVal-velocityField.y)];
				
				//Step 6: WATER EVAPORATION DUE TO HEAT (Ke)
				W[xVal,yVal] *= (1.0f - (Ke)*T);
			}
		}
	}
	
	public void setWindParam         (Vector2 strength, float coverage, bool altitudeFlag) {
		
		//Update wind parameters
		
		windStrength = strength;
		windCoverage = coverage;
		windAltitude = altitudeFlag;
	}
	
	private float windValue          (float strength, float height, int x, int y) {
		
		//Retrieve wind potential force at vertices[x,y]
		
		//Get slope value
		float slope = findSlope(x,y);
		
		//Clamp to 0.005 
		if (slope < 0.005f) slope = 0.005f;
		
		//Check if altitude scaling is not on 
		//Set value to null if so, 1
		if (!windAltitude) height = 1;
		
		//Return wind potential force
		return strength * height * slope;
	}
	//^^^^^^^^^^^^^^^^^^^^^^^^
	
	
	//THERMAL EROSION MODEL
	
	public  void applyThermalErosion (int iterations, float slopeMin, float c) {
		
		//Thermal erosion main algorithm
		
		//Start iterating
		for (int iter = 0; iter<iterations; iter++) {
			
			//Pick random position and start the main algorithm
			int xVal = Random.Range(0,terrainSize-1);
			int zVal = Random.Range(0,terrainSize-1);
			
			//Call the sediment transportation recursive step
			thermalRecursion (xVal, zVal, slopeMin, c);
		}
	}
	
	private void localThermalErosion (Vector3 start, Vector3 end, int iterations, float slopeMin, float c) {
		
		//Localised thermal erosion for 'on-the-fly' content generation
		
		//Iterate 
		for (int iter = 0; iter<iterations; iter++) {
			
			//Pick random value inside the enclosed area and start the main algorithm
			int xVal = Random.Range((int)start.x, (int)end.x);
			int zVal = Random.Range((int)start.z, (int)end.z);
			
			//Call the sediment transportation recursive step
			thermalRecursion (xVal, zVal, slopeMin, c);
		}
	}
	
	private void thermalRecursion    (int xVal, int zVal, float T, float c) {
	
		//Recursive sediment transportation with slope checking
		
		//Find lowest neighbour coordinates
		Vector2 lowestNeighbour = findLowestNeighb(xVal, zVal);
		
		//Calculate distance/slope
		float dist = vertices[xVal, zVal].y - vertices[(int)lowestNeighbour.x, (int)lowestNeighbour.y].y;
		
		//Check bounds
		if (dist > T) {
				
			//Move sediment 
			float sedAmount = c*(dist-T);
				
			vertices[xVal,zVal].y -= sedAmount;
  			vertices[(int)lowestNeighbour.x,(int)lowestNeighbour.y].y += sedAmount;
			
			//Recall
			thermalRecursion((int)lowestNeighbour.x, (int)lowestNeighbour.y, T, c);
		}
	}
	//^^^^^^^^^^^^^^^^^^^^^^^^
	
	
	
	//LOW-PASS / SPIKES REMOVAL FILTER MODEL
	
	public  void applySpikesFilter   (float epsilon) {
		
		//Personal filter for smoothing out unwanted spikes
		
		//Iterate through the mesh
		for (int x=0; x<terrainSize; x++) {
			for (int z=0; z<terrainSize; z++) {
				
				float nSum = 0;
				float index = 0;
				
				//Find neighbours and add their values
				if (x>0                                ) { nSum += vertices[x-1, z].y;   index += 1; }
				if (x<terrainSize-1                    ) { nSum += vertices[x+1, z].y;   index += 1; }
				if (z>0                                ) { nSum += vertices[x,   z-1].y; index += 1; }
				if (z<terrainSize-1                    ) { nSum += vertices[x,   z+1].y; index += 1; }
				if (x>0 && z>0                         ) { nSum += vertices[x-1, z-1].y; index += 1; }
				if (x>0 && z<terrainSize-1             ) { nSum += vertices[x-1, z+1].y; index += 1; }
				if (x<terrainSize-1 && z<terrainSize-1 ) { nSum += vertices[x+1, z+1].y; index += 1; }	
				if (x<terrainSize-1 && z>0             ) { nSum += vertices[x+1, z-1].y; index += 1; }
				
				//Find neighbours height average
				float averageN = nSum/index;
				
				//Check offset parameters and assign new values
				if (vertices[x,z].y < averageN - epsilon) vertices[x,z].y = averageN - epsilon;
				if (vertices[x,z].y > averageN + epsilon) vertices[x,z].y = averageN + epsilon;
			}
		}
	}
	//^^^^^^^^^^^^^^^^^^^^^^^^
	
	
	
	//GAUSSIAN FILTER MODEL
	
	public  void applyGaussianBlur   (float blurring_factor, int kernel_size, Vector3 start, Vector3 end) {
		
		//Gaussian filter main loop
		
		
		//Build the kernel
		initGaussKernel(blurring_factor, kernel_size);
		int half_step = (int)(kernel_size/2);
		
		
		//Copy the vertices onto temporary map
		Vector3[,] temp;
		temp = vertices;
		
		//Iterate through the mesh
		for (int x=(int)start.x; x<(int)end.x+1; x++)
			for (int y=(int)start.z; y<(int)end.z+1; y++) {
				
				float sum = 0.0f;
				
				//Iterate through kernel
				for (int m = -1*half_step; m <= half_step; m++) 
					for (int n = -1*half_step; n <= half_step; n++) {
						
						//Average the values according to the kernel weights
						if      (x+m<0)             sum += vertices[x,y].y     *gaussianKernel[m+half_step, n+half_step];
						else if (y+n<0)             sum += vertices[x,y].y     *gaussianKernel[m+half_step, n+half_step];
						else if (x+m>terrainSize-1) sum += vertices[x,y].y     *gaussianKernel[m+half_step, n+half_step];
						else if (y+n>terrainSize-1) sum += vertices[x,y].y     *gaussianKernel[m+half_step, n+half_step];
						else                        sum += vertices[x+m,y+n].y *gaussianKernel[m+half_step, n+half_step];
				}
				
				//Assign new value to temporary map
				temp[x,y].y = sum;
		}
		
		//Swap maps
		vertices = temp;
	}
	
	private void initGaussKernel     (float blurring_factor, int kernel_size) {
		
		//Gaussian kernel build algorithm
		
		//Initialise kernel map
		gaussianKernel = new float[kernel_size, kernel_size];
		int half_step = (int)(kernel_size/2);
		
		float PI  = 3.14159265359f; 
		float sum = 0.0f;
		
		//Iterate through kernel map
		for (int x = -1*half_step; x <= half_step; x++) {
			for (int y = -1*half_step; y <= half_step; y++) {
				
				//Assign raw values to the kernel
				gaussianKernel[x+half_step,y+half_step] = (1.0f/2*PI*(blurring_factor*blurring_factor))*Mathf.Exp(-1.0f*((x*x+y*y)/(2*(blurring_factor*blurring_factor))));
				sum += gaussianKernel[x+half_step,y+half_step];
			}
		}
		
		//Iterate through map
		for (int x = -1*half_step; x <= half_step; x++) {
			for (int y = -1*half_step; y <= half_step; y++) {
				
				//Normalise the values
				gaussianKernel[x+half_step,y+half_step] = gaussianKernel[x+half_step,y+half_step]/sum;
			}
		}
	}
	//^^^^^^^^^^^^^^^^^^^^^^^^
	
	
	
	//PROCEDURAL TEXTURES MODEL
	
	public void setTexture           (bool flag) {
		
		//Set procedural texture if true, otherwise set heighmap texture
		
		if (flag)
			for (int i=0; i<4; i++)
				myTerrain[i].GetComponent<Renderer>().material.mainTexture = proceduralTexture;
		else 
			for (int i=0; i<4; i++)
				myTerrain[i].GetComponent<Renderer>().material.mainTexture = heightMap;
	}

	public void applyProceduralTex   (bool sandFlag, Vector3 sandColor, float sandLimit, float sandStrength, float sandCoverage, bool grassFlag, Vector3 grassColor, float grassStrength, bool snowFlag, Vector3 snowColor, float snowLimit, float snowStrength, float snowCoverage, bool slopeFlag, Vector3 slopeColor, float slopeLimit, float slopeStrength, float noiseLimit) {
		
		//Procedural texture main algorithm
		
		
		//Create temporary texture map as vectors for increased performance
		Texture2D tempTexture = new Texture2D(terrainSize, terrainSize);
		
		//Color layers index
		int texLayers = 4;
	
		//Initialise color layers and weights
		Vector3[] layer = new Vector3[texLayers];
		float[]   weights = new float[texLayers];
		
		layer[0] = sandColor; //sand
		layer[1] = grassColor; //grass
		layer[2] = slopeColor; //rock
		layer[3] = snowColor; //snow
		
		
		//Iterate through texture matrix
		for (int xVal=0; xVal<terrainSize; xVal++) {
			for (int yVal=0; yVal<terrainSize; yVal++) {
				
				//Initialise weights variables
				float random    = Random.Range(-noiseLimit, noiseLimit); //random noise
				float height    = vertices[xVal,yVal].y;                //height at current point
				float steepness = 1.0f - findSlope(xVal,yVal);         //get inverted slope value
					
				//Clamp slope to 0
				if (steepness<0) steepness = 0;
				
				
				//Compute weights
				
				//Sand weight
				if (height < sandLimit && steepness < sandCoverage && sandFlag)
					weights[0] = ((sandLimit - height) * sandStrength) + Random.Range(-(noiseLimit-0.1f), (noiseLimit-0.1f)); 
				else
					weights[0] = 0;
				
				//Grass weight
				if (grassFlag)
					weights[1] =  random + (1.0f - height) * grassStrength;
				else
					weights[1] = 0;
				
				//Slopes/Rocks weight
				if (slopeFlag && (steepness > slopeLimit))
					weights[2] = (steepness ) * slopeStrength + random;
				else
					weights[2] = 0;
				
				//Snow weight
				if (height > snowLimit && steepness < snowCoverage && snowFlag)
					weights[3] = ((height - snowLimit) * snowStrength) + Random.Range(-(noiseLimit-0.15f), (noiseLimit-0.15f));
				else
					weights[3] = 0;
				
				//Average the values
				float sum = (weights[0] + weights[1] + weights[2] + weights[3]);
				Vector3 finalColor = new Vector3(0,0,0);
				
				//Multiply by normalised weights
				for (int i=0; i<texLayers; i++) 
					finalColor += layer[i] * (weights[i] / sum);
				
				//Set final pixel color
				tempTexture.SetPixel(xVal,yVal,new Color(finalColor.x, finalColor.y, finalColor.z, 0.0f));
			}
		}
		tempTexture.Apply();
		
		//Cross-neighbours averaging for smoother transitions between colors
		for (int xVal=0; xVal<terrainSize; xVal++) {
			for (int yVal=0; yVal<terrainSize; yVal++) {
				
				Color allCol = new Color(0,0,0,0);
				int index = 0;
				
				if (xVal>0 && yVal>0) {
					allCol += tempTexture.GetPixel(xVal-1, yVal-1);
					index++;
				}
				
				if (xVal>0 && yVal<terrainSize-1) {
					allCol = tempTexture.GetPixel(xVal-1, yVal+1);
					index++;
				}
				
				if (xVal<terrainSize-1 && yVal<terrainSize-1) {
					allCol = tempTexture.GetPixel(xVal+1, yVal+1);
					index++;
				}
				
				if (xVal<terrainSize-1 && yVal>0) {
					allCol = tempTexture.GetPixel(xVal+1, yVal-1);
					index++;
				}
				
				//Averaging
				Color finalColor = allCol / index;
				//Setting final pixel color
				proceduralTexture.SetPixel(xVal,yVal,finalColor);
			}
		}
		proceduralTexture.Apply();
	}
	
	//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
	
	
	
	//________________________________________________
	//MESH CONTROL  /  'ON-THE-FLY' CONTENT GENERATION
	//------------------------------------------------
	
	public  void goNorth (bool diSqFlag, float diSqScale, bool blurFlag, float blurring_factor, int kernel_size, bool thermalFlag, int thermalIterations, float thermalSlope, float thermalC) {
		
		//Displace vertices
		for (int z=0; z<terrainSize-patchSize-1; z++) {
			for (int x=0; x<terrainSize; x++) {
				vertices[x,z] = vertices[x,z+patchSize];	
			}
		}
		
		//Initialise new vertices
		for (int z=terrainSize-patchSize-1; z<terrainSize; z++) {
			for (int x=0; x<terrainSize; x++) {
				vertices[x,z].y = 0;
				vertices[x,z].z += patchSize;
			}
		}
		
		//Apply Diamond-Square method to new patches
		if (diSqFlag) {
			
			//Copy neighbouring values
			for (int x=0; x<terrainSize; x++)
				vertices[x,terrainSize-patchSize-1].y = vertices[x,terrainSize-patchSize-2].y;
			
			//Set welding flags
			a_corner = b_corner = c_corner = d_corner = true;
			
			a_corner = false;
			b_corner = false;
			
			for (int x=0; x<patchCount; x++) {
					
				//Further set welding flags
				if (x>0) {
					a_corner = b_corner = c_corner = d_corner = true;
					
					a_corner = false;
					d_corner = false;
					b_corner = false;
				}
				
				//Set starting point and call Diamond-Square algorithm
				Vector3 temp = new Vector3(x*patchSize + x, 0, (patchCount-1)*patchSize+(patchCount-1));
				initDiamondSquare(temp, diSqScale);
			}
		}
		
		//Apply Thermal Erosion method to new patches
		if (thermalFlag) {
		
			localThermalErosion(new Vector3(0, 0, (patchCount-1)*patchSize+(patchCount-1)-thermalOffset), new Vector3(terrainSize-1, 0, terrainSize-1), thermalIterations, thermalSlope, thermalC);
			applySpikesFilter(0.005f);
		}
		
		if (blurFlag) 
			applyGaussianBlur(blurring_factor, kernel_size, new Vector3(0, 0, (patchCount-1)*patchSize+(patchCount-1)-blurOffset), new Vector3(terrainSize-1, 0, terrainSize-1));
		
		build ();
	}
	
	public  void goSouth (bool diSqFlag, float diSqScale, bool blurFlag, float blurring_factor, int kernel_size, bool thermalFlag, int thermalIterations, float thermalSlope, float thermalC) {
		
		//Same comments as in the 'goNorth' function
		
		for (int z=terrainSize-1; z>patchSize; z--) {
			for (int x=0; x<terrainSize; x++) {
				vertices[x,z] = vertices[x,z-patchSize];	
			}
		}
		
		for (int z=0; z<patchSize+1; z++) {
			for (int x=0; x<terrainSize; x++) {
				vertices[x,z].y = 0;
				vertices[x,z].z -= patchSize;
			}
		}
		
		if (diSqFlag) {
			
			for (int x=0; x<terrainSize; x++)
				vertices[x,patchSize].y = vertices[x,patchSize+1].y;
			
			a_corner = b_corner = c_corner = d_corner = true;
			
			c_corner = false;
			d_corner = false;
			
			for (int x=0; x<patchCount; x++) {
					
				if (x>0) {
					a_corner = b_corner = c_corner = d_corner = true;
					
					c_corner = false;
					d_corner = false;
					a_corner = false;
				}
				Vector3 temp = new Vector3(x*patchSize + x, 0, 0);
					
				initDiamondSquare(temp, diSqScale);
			}
		}
		
		if (thermalFlag) {
		
			localThermalErosion(new Vector3(0, 0, 0), new Vector3(terrainSize-1, 0, patchSize+thermalOffset), thermalIterations, thermalSlope, thermalC);
			applySpikesFilter(0.005f);
		}
		
		if (blurFlag) 
			applyGaussianBlur(blurring_factor, kernel_size, new Vector3(0, 0, 0), new Vector3(terrainSize-1, 0, patchSize+blurOffset));
		
		build ();
	}
	
	public  void goWest  (bool diSqFlag, float diSqScale, bool blurFlag, float blurring_factor, int kernel_size, bool thermalFlag, int thermalIterations, float thermalSlope, float thermalC) {
		
		//Same comments as in the 'goNorth' function
		
		for (int x=terrainSize-1; x>patchSize; x--) {
			for (int z=0; z<terrainSize; z++) {
				vertices[x,z] = vertices[x-patchSize,z];	
			}
		}
		
		for (int x=0; x<patchSize+1; x++) {
			for (int z=0; z<terrainSize; z++) {
				vertices[x,z].y = 0;
				vertices[x,z].x -= patchSize;
			}
		}
		
		if (diSqFlag) {
			
			for (int z=0; z<terrainSize; z++)
				vertices[patchSize,z].y = vertices[patchSize+1,z].y;
			
			a_corner = b_corner = c_corner = d_corner = true;
			
			b_corner = false;
			c_corner = false;
			
			for (int z=0; z<patchCount; z++) {
					
				if (z>0) {
					a_corner = b_corner = c_corner = d_corner = true;
					
					b_corner = false;
					c_corner = false;
					a_corner = false;
				}
				Vector3 temp = new Vector3(0, 0, z*patchSize + z);
					
				initDiamondSquare(temp, diSqScale);
			}
		}
		
		if (thermalFlag) {
		
			localThermalErosion(new Vector3(0, 0, 0), new Vector3(patchSize+thermalOffset, 0, terrainSize-1), thermalIterations, thermalSlope, thermalC);
			applySpikesFilter(0.005f);
		}
		
		if (blurFlag)
			applyGaussianBlur(blurring_factor, kernel_size, new Vector3(0, 0, 0), new Vector3(patchSize+blurOffset, 0, terrainSize-1));
		
		build ();
	}
	
	public  void goEast  (bool diSqFlag, float diSqScale, bool blurFlag, float blurring_factor, int kernel_size, bool thermalFlag, int thermalIterations, float thermalSlope, float thermalC) {
		
		//Same comments as in the 'goNorth' function
		
		for (int x=0; x<terrainSize-1-patchSize; x++) {
			for (int z=0; z<terrainSize; z++) {
				vertices[x,z] = vertices[x+patchSize,z];	
			}
		}
		
		for (int x=terrainSize-1-patchSize; x<terrainSize; x++) {
			for (int z=0; z<terrainSize; z++) {
				vertices[x,z].y = 0;
				vertices[x,z].x += patchSize;
			}
		}
		
		if (diSqFlag) {
			
			for (int z=0; z<terrainSize; z++)
				vertices[terrainSize-1-patchSize,z].y = vertices[terrainSize-2-patchSize,z].y;
			
			a_corner = b_corner = c_corner = d_corner = true;
			
			a_corner = false;
			d_corner = false;
			
			for (int z=0; z<patchCount; z++) {
					
				if (z>0) {
					a_corner = b_corner = c_corner = d_corner = true;
					
					a_corner = false;
					d_corner = false;
					b_corner = false;
				}
				Vector3 temp = new Vector3((patchCount-1)*patchSize+(patchCount-1), 0, z*patchSize + z);
					
				initDiamondSquare(temp, diSqScale);
			}
		}
		
		if (thermalFlag) {
		
			localThermalErosion(new Vector3((patchCount-1)*patchSize+(patchCount-1)-thermalOffset, 0, 0), new Vector3(terrainSize-1, 0, terrainSize-1), thermalIterations, thermalSlope, thermalC);
			applySpikesFilter(0.005f);
		}
		
		if (blurFlag) 
			applyGaussianBlur(blurring_factor, kernel_size, new Vector3((patchCount-1)*patchSize+(patchCount-1)-blurOffset, 0, 0), new Vector3(terrainSize-1, 0, terrainSize-1));
	
		build ();
	}
	
	//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
	
	
	
	//________________
	//HELPER FUNCTIONS
	//----------------
	
	private Vector3 getNormalAt      (Vector3 vertex, int x, int z) {
		
		//Function to return normal at vertex
		
		//Initialise neighboring values
		Vector3 n1 = new Vector3(0,0,0);
		Vector3 n2 = new Vector3(0,0,0);
		Vector3 n3 = new Vector3(0,0,0);
		Vector3 n4 = new Vector3(0,0,0);
			
		float coef = 0;
		int segments = meshSize;
		
		//Scale to renderable output
		Vector3 A = vertices[x,z];
		A.Scale(vertsScale);
		
		//Clamp to edge 
		if (x>0 && z<segments) {
			//Get neigbors
			Vector3 B = vertices[x-1,z];
			Vector3 C = vertices[x,z+1];
			
			//Set neighbors to renderable scale
			B.Scale(vertsScale);
			C.Scale(vertsScale);
			
			//Get the normalised cross-product and increment index
			n1 = Vector3.Cross (B-A, C-A).normalized;
			
			coef += 1;
		}
		//Clamp to edge 
		if (x<segments && z<segments) {
			//Get neigbors
			Vector3 C = vertices[x,z+1];
			Vector3 D = vertices[x+1,z];
			
			//Set neighbors to renderable scale
			C.Scale(vertsScale);
			D.Scale(vertsScale);
			
			//Get the normalised cross-product and increment index
			n2 = Vector3.Cross (C-A, D-A).normalized;
			coef += 1;
		}
		//Clamp to edge 
		if (x<segments && z>0) {
			//Get neigbors
			Vector3 D = vertices[x+1,z];
			Vector3 E = vertices[x,z-1];
			
			//Set neighbors to renderable scale
			D.Scale(vertsScale);
			E.Scale(vertsScale);
			
			//Get the normalised cross-product and increment index
			n3 = Vector3.Cross (D-A, E-A).normalized;
			coef += 1;
		}
		//Clamp to edge 
		if (x>0 && z>0) {
			//Get neigbors
			Vector3 E = vertices[x,z-1];
			Vector3 B = vertices[x-1,z];
			
			//Set neighbors to renderable scale
			E.Scale(vertsScale);
			B.Scale(vertsScale);
			
			//Get the normalised cross-product and increment index
			n4 = Vector3.Cross (E-A, B-A).normalized;
			coef +=1;
		}
		
		//Return normal
		return new Vector3((n1.x+n2.x+n3.x+n4.x)/coef, (n1.y+n2.y+n3.y+n4.y)/coef, (n1.z+n2.z+n3.z+n4.z)/coef);	
	}
	
	public Vector3 collisionCheck    (Vector3 objPosition) {
	
		//Check for collision and return new height
		//Simulating the sliding over terrain
		
		//Copy object position
		Vector3 newPos = objPosition;
		
		//Scale position to fit normalised vertices[,] map
		objPosition.x -= startOf.x;
		objPosition.z -= startOf.z;
		
		objPosition.x /= vertsScale.x;
		objPosition.y /= vertsScale.y;
		objPosition.z /= vertsScale.z;
		
		if (objPosition.x > terrainSize-1) objPosition.x = terrainSize-1;
		else
			if (objPosition.x < 0) objPosition.x = 0;
		
		if (objPosition.y > terrainSize-1) objPosition.y = terrainSize-1;
		else
			if (objPosition.y < 0) objPosition.y = 0;
		
		//Get height at current point
		float terrHeight = vertices[(int)objPosition.x, (int)objPosition.z].y;
		
		//Check heights and set new height if collision occurs
		if (objPosition.y < terrHeight) newPos.y = terrHeight * vertsScale.y;
		
		//Return new position
		return newPos;
	}
	
	private Vector2 findLowestNeighb (int xVal, int zVal) {
		
		//Function to find lowest height in the Moore neighborhood
		
		int indexX = 0;
		int indexY = 0;
		
		float min = 10;
		
		//Clamp values and check neighbours' heights
		
		if (xVal>0 && zVal>0                         && vertices[xVal-1, zVal-1].y < min) { indexX = xVal-1; indexY = zVal-1; min = vertices[xVal-1, zVal-1].y;}
		if (xVal>0 && zVal<terrainSize-1             && vertices[xVal-1, zVal+1].y < min) { indexX = xVal-1; indexY = zVal+1; min = vertices[xVal-1, zVal+1].y;}
		if (xVal<terrainSize-1 && zVal<terrainSize-1 && vertices[xVal+1, zVal+1].y < min) { indexX = xVal+1; indexY = zVal+1; min = vertices[xVal+1, zVal+1].y;}	
		if (xVal<terrainSize-1 && zVal>0             && vertices[xVal+1, zVal-1].y < min) { indexX = xVal+1; indexY = zVal-1; min = vertices[xVal+1, zVal-1].y;}
		
		if (xVal>0                                   && vertices[xVal-1, zVal].y   < min) { indexX = xVal-1; indexY = zVal;   min = vertices[xVal-1, zVal].y;  }
		if (xVal<terrainSize-1                       && vertices[xVal+1, zVal].y   < min) { indexX = xVal+1; indexY = zVal;   min = vertices[xVal+1, zVal].y;  }
		if (zVal>0                                   && vertices[xVal,   zVal-1].y < min) { indexX = xVal;   indexY = zVal-1; min = vertices[xVal,   zVal-1].y;} 
		if (zVal<terrainSize-1                       && vertices[xVal,   zVal+1].y < min) { indexX = xVal;   indexY = zVal+1; min = vertices[xVal,   zVal+1].y;}
		
		//Return index of lowest neighbour as 2D vector
		return new Vector2(indexX, indexY);
	}
	
	private float findSlope          (int xVal, int yVal) {
			
		//Find the slope at position xVal, yVal
		
		float[] neighbH = new float[4];
		
		//Find neighbours
		if (xVal > 0)
			neighbH[0] = vertices[xVal-1,yVal].y;
		else 
			neighbH[0] = vertices[xVal,yVal].y;
		if (xVal < terrainSize-1)
			neighbH[1] = vertices[xVal+1,yVal].y;
		else 
			neighbH[1] = vertices[xVal,yVal].y;
		if (yVal > 0)
			neighbH[2] = vertices[xVal,yVal-1].y;
		else 
			neighbH[2] = vertices[xVal,yVal].y;
		if (yVal < terrainSize-1)
			neighbH[3] = vertices[xVal,yVal+1].y;
		else 
			neighbH[3] = vertices[xVal,yVal].y;
		
		//Find normal
		Vector3 va = new Vector3 (1.0f, 0.0f, neighbH[1] - neighbH[0]);
		Vector3 vb = new Vector3 (0.0f, 1.0f, neighbH[3] - neighbH[2]);
		Vector3 n  = Vector3.Cross(va.normalized, vb.normalized);
		
		//Return dot product of normal with the Y axis
		return Mathf.Max(0.05f, 1.0f - Mathf.Abs ( Vector3.Dot(n, new Vector3(0,1,0))));
		
	}  
	
	public void exportObj            () {
	
		//Export mesh at destination c:/ on the system
		//onto 4 meshes due to Unity's incapability of
		//holding meshes of over 65000 vertices
		
		ObjExporter.MeshToFile(myTerrain[0].GetComponent<MeshFilter>(), "/myTerrain_0.obj");
		ObjExporter.MeshToFile(myTerrain[1].GetComponent<MeshFilter>(), "/myTerrain_1.obj");
		ObjExporter.MeshToFile(myTerrain[2].GetComponent<MeshFilter>(), "/myTerrain_2.obj");
		ObjExporter.MeshToFile(myTerrain[3].GetComponent<MeshFilter>(), "/myTerrain_3.obj");
	}
	
	//^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
}

















































