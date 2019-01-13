using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour {

	public void PlayGame()
	{
		//Get the next scene in the build queue
		SceneManager.LoadScene (SceneManager.GetActiveScene ().buildIndex + 1);
	}

	public void ExitGame()
	{
		Debug.Log ("Quit");
		Application.Quit();
	}
}
