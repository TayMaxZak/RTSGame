using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Util_DeNetwork: MonoBehaviour
{
	// Update is called once per frame
	void Start()
	{
		Transform[] kids = transform.GetComponentsInChildren<Transform>(true);
		for (int i = 1; i < kids.Length; i++)
		{
			if (kids[i].parent == transform)
				kids[i].gameObject.SetActive(true);
		}
	}
}
