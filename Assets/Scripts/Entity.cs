using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
	[SerializeField]
	private string displayName = "Default Name";

	public string DisplayName
	{
		get
		{
			return displayName;
		}
	}

	// Use this for initialization
	protected void Start ()
	{
		Debug.Log(DisplayName + " BTW");
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	}

	void OnSelect(Commander selector)
	{
	}
}
