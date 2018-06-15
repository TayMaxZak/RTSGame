using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_ArmorDrain : Ability
{
	private float energy;
	private Vector3 deltaDurations;

	[SerializeField]
	private Effect_Point pointEffectPrefab;
	private Effect_Point pointEffect;

	private bool isActive = false;

	void Awake()
	{
		abilityType = AbilityType.ArmorDrain;
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		energy = 1;
		deltaDurations = AbilityUtils.GetDeltaDurations(AbilityType.ArmorDrain);

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(isActive);
	}

	public override void End()
	{
		pointEffect.End();
	}

	void Update()
	{
		pointEffect.transform.position = transform.position; // Move effect to center of user

		if (isActive)
		{
			if (energy <= 0)
			{
				UseAbility(null); // Toggle to inactive and put on cooldown
			}

			energy -= deltaDurations.y * Time.deltaTime;

			Collider[] cols = Physics.OverlapSphere(transform.position, gameRules.ABLYarmorDrainRange, gameRules.entityLayerMask);
			List<Unit> units = new List<Unit>();
			for (int i = 0; i < cols.Length; i++)
			{
				Unit unit = GetUnitFromCol(cols[i]);

				if (!unit) // Only works on units
					continue;

				if (units.Contains(unit)) // Ignore multiple colliders for one unit
					continue;

				if (unit.GetType() == typeof(Unit_Flagship)) // Can't drain Flagships
					continue;

				if (unit.GetHP().z <= 0) // They must have some armor
					continue;

				foreach (Ability a in unit.abilities)
					if (a.GetAbilityType() == AbilityType.ArmorDrain) // Can't drain another drain-capable unit
						continue;

				if (unit != parentUnit) // Don't add ourselves
					units.Add(unit);
			}

			// TODO: Sort unit list to have allies first

			int allyCount = 0;
			int enemyCount = 0;

			for (int i = 0; i < units.Count && i < gameRules.ABLYarmorDrainMaxVictims; i++) // For each unit, subtract armor
			{
				if (units[i].team == parentUnit.team)
				{
					if (parentUnit.GetHP().z < parentUnit.GetHP().w) // Only damage allies if we have missing armor
					{
						units[i].DamageSimple(0, gameRules.ABLYarmorDrainDPSAlly * Time.deltaTime);
						allyCount++;
					}
				}
				else
				{
					units[i].DamageSimple(0, gameRules.ABLYarmorDrainDPSEnemy * Time.deltaTime);
					enemyCount++;
				}
			}

			// Add armor to us based on number of units
			parentUnit.DamageSimple(0, -(gameRules.ABLYarmorDrainGPS + gameRules.ABLYarmorDrainGPSBonusMult * enemyCount) * (allyCount + enemyCount) * Time.deltaTime);

			if ((allyCount + enemyCount) == 0)
				pointEffect.SetEffectActive(true, false);
			else
				pointEffect.SetEffectActive(true, true);
		}
		else // Inactive
		{
			energy += deltaDurations.z * Time.deltaTime;
		}
	}

	public override void UseAbility(AbilityTarget targ)
	{
		base.UseAbility(null);

		isActive = !isActive;

		pointEffect.SetEffectActive(isActive);
	}

	Unit GetUnitFromCol(Collider col)
	{
		Entity ent = col.GetComponentInParent<Entity>();
		if (ent)
		{
			if (ent.GetType() == typeof(Unit) || ent.GetType().IsSubclassOf(typeof(Unit)))
				return (Unit)ent;
			else
				return null;
		}
		else
		{
			return null;
		}
	}
}
