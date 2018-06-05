using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flagship : Unit
{
	private float shieldRegenTimer;

	// Use this for initialization
	new void Start()
	{
		gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>();
		gameRules = gameManager.GameRules; // Grab copy of Game Rules
		base.Start(); // Init Unit base class
		maxShield = gameRules.FLAGshieldMax;
		UpdateHPBarVal();
	}

	// Update is called once per frame
	new void Update ()
	{
		shieldRegenTimer -= Time.deltaTime;
		if (shieldRegenTimer <= 0)
			curShield = Mathf.Clamp(curShield + gameRules.FLAGshieldRegenGPS * Time.deltaTime, 0, gameRules.FLAGshieldMax);
		base.Update(); // Unit base class
	}

	protected override void OnDamage()
	{
		shieldRegenTimer = gameRules.FLAGshieldRegenDelay; // Reset shield regen out-of-combat timer
	}
}
