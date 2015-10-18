using UnityEngine;
using System.Collections;

//SCRIPT CONTROLLING THE ROTATIION OF THE PLANE'S BLADES

public class PlaneBlades : MonoBehaviour {
	
	public float rotationSpeed = 20.0f;
	public bool  orientation = true;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		
		//check orientation
		int ind;
		if (orientation) ind = 1;
		else ind = -1;
		
		//Rotate
		transform.Rotate(Vector3.forward * rotationSpeed * ind);
	}
}
