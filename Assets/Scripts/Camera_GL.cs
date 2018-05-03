using UnityEngine;
using System.Collections;

public class Camera_GL : MonoBehaviour
{
	void OnPreRender()
	{
		GL.wireframe = true;
	}
	void OnPostRender()
	{
		GL.wireframe = false;
	}
}