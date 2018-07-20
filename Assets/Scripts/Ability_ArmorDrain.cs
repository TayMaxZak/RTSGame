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
	private bool hasSuperlaser = false;

	void Awake()
	{
		abilityType = AbilityType.ArmorDrain;
		InitCooldown();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();
		displayInfo.displayFill = true;

		energy = 1;
		deltaDurations = AbilityUtils.GetDeltaDurations(AbilityType.ArmorDrain);

		foreach (Ability a in parentUnit.abilities)
			if (a.GetAbilityType() == AbilityType.Superlaser)
				hasSuperlaser = true;

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(isActive);
	}

	public override void End()
	{
		pointEffect.End();
	}

	new void Update()
	{
		base.Update();

		pointEffect.transform.position = transform.position; // Move effect to center of user

		if (isActive)
		{
			if (energy > 0) // Needs energy to run
			{
				// Consume energy according to active duration
				energy -= deltaDurations.y * Time.deltaTime;
				Display(1 - energy);

				Collider[] cols = Physics.OverlapSphere(transform.position, gameRules.ABLYarmorDrainRange, gameRules.entityLayerMask);
				List<Unit> units = new List<Unit>();
				for (int i = 0; i < cols.Length; i++)
				{
					Unit unit = GetUnitFromCol(cols[i]);

					if (!unit) // Only works on units
						continue;

					if (units.Contains(unit)) // Ignore multiple colliders for one unit
						continue;

					if (unit == parentUnit) // Don't add ourselves
						continue;

					if (unit.Type == EntityType.Flagship) // Can't drain Flagships
						continue;

					if (unit.GetHP().z <= 0) // They must have some armor
						continue;

					bool hasDrain = false; // Can't drain another drain-capable unit
					foreach (Ability a in unit.abilities)
						if (a.GetAbilityType() == AbilityType.ArmorDrain)
							hasDrain = true;
					if (hasDrain)
						continue;
					
					units.Add(unit);
				}

				// TODO: Sort unit list to have allies first

				int allyCount = 0;
				int enemyCount = 0;

				for (int i = 0; i < units.Count && i < gameRules.ABLYarmorDrainMaxVictims; i++) // For each unit, subtract armor
				{
					if (units[i].team == parentUnit.team) // Ally
					{
						if (parentUnit.GetHP().z < parentUnit.GetHP().w) // Only damage allies if we have missing armor
						{
							units[i].DamageSimple(0, gameRules.ABLYarmorDrainDPSAlly * Time.deltaTime);
							allyCount++;
						}
					}
					else // Enemy
					{
						units[i].DamageSimple(0, gameRules.ABLYarmorDrainDPSEnemy * Time.deltaTime);
						if (hasSuperlaser)
						{
							Status markStatus = new Status(gameObject, StatusType.SuperlaserMark); // TODO: Optimize?
							markStatus.SetTimeLeft(gameRules.ABLYarmorDrainDPSEnemy * Time.deltaTime);
							units[i].AddStatus(markStatus);
						}
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
			else
			{
				Toggle(); // Toggle to inactive and put on cooldown
				StartCooldown();
			}
		}
		else // Inactive
		{
			if (energy < 1)
			{
				// Restore energy according to reset duration
				energy += deltaDurations.z * Time.deltaTime;
				Display(1 - energy);
			}
		}
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (!offCooldown)
			return;

		base.UseAbility(target);

		Toggle();
	}

	void Toggle()
	{
		isActive = !isActive;

		pointEffect.SetEffectActive(isActive);
	}

	void Display(float fill)
	{
		displayInfo.fill = fill;
		UpdateDisplay(abilityIndex, false);
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
