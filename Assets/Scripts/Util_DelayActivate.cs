using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Util_DelayActivate: MonoBehaviour
{
	[SerializeField]
	private GameObject toActivate;

	private bool used = false;

	void Awake()
	{
		toActivate.SetActive(false);
	}

	// Update is called once per frame
	void Update ()
	{
		if (Time.time > 0.25f && !used)
		{
			toActivate.SetActive(true);
			used = true;
		}
	}
}
