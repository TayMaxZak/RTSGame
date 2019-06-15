using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit_Flagship : Unit
{
	private float shieldRegenTimer;

	private ShieldMod shieldMod;

	// Use this for initialization
	new void Start()
	{
		gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>();
		gameRules = gameManager.GameRules; // Grab copy of Game Rules
		base.Start(); // Init Unit base class

		gameManager.GetCommander(Team).SetFlagship(this);

		shieldMod = new ShieldMod(this, 1, ShieldModType.Flagship);
		AddShieldMod(shieldMod); // Apply flagship shield to self
		UpdateHPBarVal(true); // Update with new shield
	}

	// Update is called once per frame
	new void Update ()
	{
		base.Update(); // Unit base class

		shieldRegenTimer -= Time.deltaTime;
		if (shieldRegenTimer <= 0)
		{
			// Regenerate shieldPercent
			if (shieldMod.shieldPercent < 1)
			{
				shieldMod.shieldPercent = Mathf.Min(shieldMod.shieldPercent + (gameRules.FLAG_shieldRegenGPS / gameRules.FLAG_shieldMaxPool) * Time.deltaTime, 1);
				// Apply shieldMod to the unit
				UpdateShield();
				// Already passing by reference, no need to add again
				//AddShieldMod(shieldMod);
			}
		}
	}

	protected override void OnDamage()
	{
		base.OnDamage();
		shieldRegenTimer = gameRules.FLAG_shieldRegenDelay; // Reset shield regen out-of-combat timer
	}

	public override void Die(DamageType damageType)
	{
		base.Die(damageType);

		gameManager.Defeat(Team);
	}
}
