﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Util_SceneRestart: MonoBehaviour
{
	[SerializeField]
	private int sceneID;
	
	// Update is called once per frame
	void Update ()
	{
		//if (Input.GetKeyDown("backspace"))
		//{
		//	SceneManager.LoadScene(sceneID);
		//}

		if (Input.GetKeyDown("escape"))
		{
			Application.Quit();
		}
	}
}
