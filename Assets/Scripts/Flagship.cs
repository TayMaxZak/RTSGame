using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flagship : Unit
{

	// Use this for initialization
	new void Start()
	{
		base.Start(); // Init Unit base class
		maxShield = gameRules.FLAGshieldPool;
	}

	// Update is called once per frame
	/*
	new void Update ()
	{
		base.Update(); // Unit base class
	}
	*/
}
