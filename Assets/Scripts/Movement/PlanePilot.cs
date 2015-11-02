using UnityEngine;
using System.Collections;

[RequireComponent (typeof (MeshCollider))]

public class PlanePilot : MonoBehaviour {
	
	public float speed = 30.0f;
	float minSpeed = 20.0f;
	float maxSpeed = 80.0f;
	float rotParam = 0.0f;
	
	public Vector3 position;
	
	// Update is called once per frame
	void Update () {
		
		//Get position
		position = transform.position;
		
		//Move cam behind object - variable
		Vector3 moveCamTo = transform.position - transform.forward * 30.0f + Vector3.up * 15.0f;
		
		//Get position bias 
		float bias = 0.8f;
	
		//Set main camera
		GameObject.Find ("Main Camera").transform.position = GameObject.Find ("Main Camera").transform.position * bias + moveCamTo * (1.0f - bias);
		GameObject.Find ("Main Camera").transform.LookAt (transform.position + transform.forward * 50.0f);
		
		//move forward
		transform.position += transform.forward * Time.deltaTime * speed;
		
		//Change speed according to orientation
		speed -= transform.forward.y * Time.deltaTime * 30.0f;
		
		if (speed < minSpeed) speed = minSpeed;
		if (speed > maxSpeed) speed = maxSpeed;
	
		//Set left-right rotation
		transform.Rotate (Input.GetAxis("Vertical"), 0.0f, -1 * Input.GetAxis("Horizontal"));
		
		//Rotate onto the World Matrix according to plane wing inclination
		rotParam += Input.GetAxis("Horizontal");
		transform.Rotate (Vector3.up * rotParam/100, Space.World);

        //Check for collision and update position
		//transform.position = GameObject.Find("GameObject").GetComponent<SceneManager>().terrain.collisionCheck(transform.position);
	}
}









