using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flagship : Unit
{

	// Use this for initialization
	new void Start()
	{
		base.Start(); // Init Unit base class
	}

	// Banking
	//float bank = bankAngle * -Vector3.Dot(transform.right, direction);
	//banker.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.forward);

	// Update is called once per frame
	new void Update ()
	{
		base.Update(); // Unit base class
		UpdateUI();
	}

	void UpdateUI()
	{

	}
}
