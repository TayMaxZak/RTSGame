using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Util_SceneRestart: MonoBehaviour
{
	[SerializeField]
	private int sceneID = -1;
	
	// Update is called once per frame
	void Update ()
	{
		if (sceneID >= 0 && Input.GetKeyDown("backspace"))
		{
			SceneManager.LoadScene(sceneID);
		}

		if (Input.GetKeyDown("escape"))
		{
			Application.Quit();
		}
	}
}
