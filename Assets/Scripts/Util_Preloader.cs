using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util_Preloader: MonoBehaviour
{
	[SerializeField]
	private string[] resourcesToLoad;

	// Use this for initialization
	void Start ()
	{
		foreach (string s in resourcesToLoad)
		{
			GameObject go = Instantiate(Resources.Load(s)) as GameObject;
			Destroy(go);
		}
	}
	
	// Update is called once per frame
	void Update ()
	{

	}
}
