using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera_DepthTexture: MonoBehaviour
{
	private Camera cam;

	// Use this for initialization
	void Start ()
	{
		cam = GetComponent<Camera>();
		cam.depthTextureMode = DepthTextureMode.Depth;
	}
	
	// Update is called once per frame
	void Update ()
	{

	}
}
