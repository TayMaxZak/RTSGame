using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_ArmorDrain : Ability
{
	private float energy;
	private Vector3 deltaDurations;

	[Header("Effects")]
	[SerializeField]
	private Effect_Point pointEffectPrefab;
	private Effect_Point pointEffect;

	[Header("Audio")]
	[SerializeField]
	private AudioEffect_Loop audioLoopPrefab;
	private AudioEffect_Loop audioLoop;

	private bool isActive = false;
	private bool hasSuperlaser = false;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.ArmorDrain;
		InitCooldown();

		energy = 1;
		deltaDurations = AbilityUtils.GetDeltaDurations(abilityType);

		displayInfo.displayFill = true;
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		foreach (Ability a in parentUnit.abilities)
			if (a.GetAbilityType() == AbilityType.Superlaser)
				hasSuperlaser = true;

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(isActive);

		audioLoop = Instantiate(audioLoopPrefab, transform.position, Quaternion.identity);
		audioLoop.SetEffectActive(isActive);
	}

	public override void End()
	{
		pointEffect.End();
		audioLoop.End();
	}

	new void Update()
	{
		base.Update();

		pointEffect.transform.position = transform.position; // Move effect to center of user
		audioLoop.transform.position = transform.position; // Move effect to center of user

		if (isActive)
		{
			if (energy > 0) // Needs energy to run
			{
				// Consume energy according to active duration
				energy -= deltaDurations.y * Time.deltaTime;
				Display(1 - energy);

				Collider[] cols = Physics.OverlapSphere(transform.position, gameRules.ABLY_armorDrainRange, gameRules.entityLayerMask);
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

				for (int i = 0; i < units.Count && i < gameRules.ABLY_armorDrainMaxVictims; i++) // For each unit, subtract armor
				{
					if (units[i].Team == parentUnit.Team) // Ally
					{
						if (parentUnit.GetHP().z < parentUnit.GetHP().w) // Only damage allies if we have missing armor
						{
							units[i].DamageSimple(0, gameRules.ABLY_armorDrainDPSAlly * Time.deltaTime, true);
							allyCount++;
						}
					}
					else // Enemy
					{
						units[i].DamageSimple(0, gameRules.ABLY_armorDrainDPSEnemy * Time.deltaTime, true);
						if (hasSuperlaser)
						{
							Status markStatus = new Status(gameObject, StatusType.SuperlaserMark); // TODO: Optimize?
							markStatus.SetTimeLeft(gameRules.ABLY_armorDrainDPSEnemy * Time.deltaTime);
							units[i].AddStatus(markStatus);
						}
						enemyCount++;
					}
				}

				// Add armor to us based on number of units
				parentUnit.DamageSimple(0, -(gameRules.ABLY_armorDrainGPSEnemy + gameRules.ABLY_armorDrainGPSBonusMult * enemyCount) * (enemyCount) * Time.deltaTime, true);
				parentUnit.DamageSimple(0, -gameRules.ABLY_armorDrainGPSAlly * (allyCount) * Time.deltaTime, true);

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
		if (suspended)
			return;

		if (!offCooldown)
			return;

		base.UseAbility(target);

		Toggle();
	}

	void Toggle()
	{
		SetActive(!isActive);
	}

	void SetActive(bool newActive)
	{
		isActive = newActive;

		pointEffect.SetEffectActive(isActive);
		audioLoop.SetEffectActive(isActive);
	}

	public override void Suspend()
	{
		base.Suspend();

		SetActive(false);
		StartCooldown();
	}

	public override void SetEffectsVisible(bool visible)
	{
		pointEffect.SetVisible(visible);
		audioLoop.SetVisible(visible);
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
