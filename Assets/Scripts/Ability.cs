using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
	ArmorDrain,
	Swarm,
	SelfDamage
}

[System.Serializable]
public class Ability
{
	//[SerializeField]
	public AbilityType type;
	[HideInInspector]
	public GameRules gameRules;

	[System.NonSerialized]
	[HideInInspector]
	public bool isActive;

	[HideInInspector]
	public Ability_Effect effect;
	[HideInInspector]
	public Unit user;
	[HideInInspector]
	public Ability_Target target;
	[HideInInspector]
	public List<Unit> unitList; // If ability involves finding multiple units, they are stored in this field
	//[HideInInspector]
	//public 

	[System.NonSerialized]
	[HideInInspector]
	public float curEnergy = 1; // How long toggle can continue to operate
	[System.NonSerialized]
	[HideInInspector]
	public float curCooldown = 0; // Cooldown for reactivating / deactivating ability

	// TODO: How to give stacks to an ability?
	private int stacks = 0;

	public void Init(Unit user, GameRules gameRules)
	{
		// Set user and game rules to use later
		this.user = user;
		this.gameRules = gameRules;

		// Spawn ability effect prefab
		AbilityUtils.InitAbility(this);
	}

	public void Activate(Ability_Target target)
	{
		this.target = target;
		AbilityUtils.StartAbility(this);
	}

	public void AbilityTick()
	{
		AbilityUtils.TickAbility(this);
	}


	public AbilityType GetAbilityType()
	{
		return type;
	}
}

public static class AbilityUtils
{
	public static void InitAbility(Ability ability)
	{
		switch (ability.type)
		{
			case AbilityType.ArmorDrain:
				GameObject go = Object.Instantiate(Resources.Load("ArmorDrainEffect") as GameObject, ability.user.transform.position, Quaternion.identity);
				ability.effect = go.GetComponent<Ability_Effect>();
				break;
			case AbilityType.Swarm:
				break;
			case AbilityType.SelfDamage:
				ability.user.TrueDamage(50, 100);
				break;
			default:
				break;
		}

		ability.effect.SetEffectActive(ability.isActive);
	}

	public static void StartAbility(Ability ability)
	{
		if (ability.curCooldown > 0)
			return;
		ability.curCooldown = 1;

		if (InstantOrToggle(ability.type))
			ability.isActive = true;
		else
			ability.isActive = !ability.isActive;

		ability.effect.SetEffectActive(ability.isActive);

		switch (ability.type)
		{
			case AbilityType.ArmorDrain:
				/*
				Collider[] cols = Physics.OverlapSphere(ability.user.transform.position, ability.gameRules.ABLYarmorDrainRange, ability.gameRules.entityLayerMask);
				for (int i = 0; i < cols.Length; i++)
				{
					ability.unitList.Add(GetUnitFromCol(cols[i]));
				}
				

				//GameObject go = Object.Instantiate(Resources.Load("ArmorDrainEffect") as GameObject, ability.user.transform.position, Quaternion.identity);
				//go.transform.SetParent(ability.user.transform);

				if (ability.isActive)
				{
					AudioSource aS = AudioUtils.PlayClipAt(Resources.Load("ArmorDrainOnSFX") as AudioClip, ability.user.transform.position);
					aS.transform.SetParent(ability.user.transform);
				}
				else
				{
					AudioSource aS = AudioUtils.PlayClipAt(Resources.Load("ArmorDrainOffSFX") as AudioClip, ability.user.transform.position);
					aS.transform.SetParent(ability.user.transform);
				}
				*/
				break;
			case AbilityType.Swarm:
				break;
			case AbilityType.SelfDamage:
				ability.user.TrueDamage(50, 100);
				break;
			default:
				break;
		}
	}

	public static void TickAbility(Ability ability)
	{
		ability.curCooldown = ability.curCooldown - Time.deltaTime * DeltaDurations(ability.type).x; // Restore cooldown

		if (ability.isActive && ability.curEnergy < 0) // Out of energy
		{
			ability.isActive = false;
			ability.effect.SetEffectActive(ability.isActive);
			//StartAbility(ability); // This would expel cooldown or fail entirely
			return;
		}

		// TODO: Fix
		if (!ability.isActive) // If ability is not being used
		{
			
			//Debug.Log("In TICK ABILITY NOT ACTIVE: " + ability.curEnergy);
			if (!InstantOrToggle(ability.type)) // If it's a toggle ability
				ability.curEnergy = Mathf.Min(ability.curEnergy + Time.deltaTime * DeltaDurations(ability.type).z, 1); // Restore energy according to its reset duration
			return; // Leave, ability isn't being used
		}

		if (!InstantOrToggle(ability.type)) // If it's a toggle ability
		{
			ability.curEnergy -= Time.deltaTime * DeltaDurations(ability.type).y; // Consume energy
		}

		//Debug.Log("In TICK ABILITY ACTIVE: " + ability.curEnergy);

		switch (ability.type)
		{
			case AbilityType.ArmorDrain: // Find all enemies in a radius and damage them over time
				ability.effect.transform.position = ability.user.transform.position; // Move effect to center of user

				Collider[] cols = Physics.OverlapSphere(ability.user.transform.position, ability.gameRules.ABLYarmorDrainRange, ability.gameRules.entityLayerMask);
				List<Unit> units = new List<Unit>();
				for (int i = 0; i < cols.Length; i++)
				{
					Unit unit = GetUnitFromCol(cols[i]);
					if (unit && unit != ability.user) // Don't add ourselves
						units.Add(unit);
				}

				// TODO: Sort unit list to have allies first

				for (int i = 0; i < units.Count && i < ability.gameRules.ABLYarmorDrainMaxVictims; i++) // For each unit, subtract apropriate armor and add armor to us
				{
					if (units[i].team == ability.user.team)
						units[i].TrueDamage(0, ability.gameRules.ABLYarmorDrainDPSAlly * Time.deltaTime);
					else
						units[i].TrueDamage(0, ability.gameRules.ABLYarmorDrainDPSEnemy * Time.deltaTime);
					if (units[i].GetHP().z > 0)
					ability.user.TrueDamage(0, -ability.gameRules.ABLYarmorDrainRegen * Time.deltaTime);
				}

				if (units.Count == 0)
					ability.effect.SetEffectActive(ability.isActive, false);
				else
					ability.effect.SetEffectActive(ability.isActive, true);

				break;
			case AbilityType.Swarm:
				break;
			default:
				break;
		}
	}

	public static bool InstantOrToggle(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return false;
			default:
				return true; // Most abilities are instant actives
		}
	}

	// X = Cooldown, Y = Active Duration, Z = Reset Duration
	public static Vector3 DeltaDurations(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return DeltaOf(new Vector3(2.0f, 12.0f, 30.0f));
			case AbilityType.Swarm:
				return DeltaOf(new Vector3(24.0f, 0, 0));
			case AbilityType.SelfDamage:
				return DeltaOf(new Vector3(2.0f, 0, 0));
			default:
				return DeltaOf(new Vector3());
		}
	}

	// Converts durations to multipliers per second
	static Vector3 DeltaOf(Vector3 vector)
	{
		// Avoid division by zero
		if (vector.x == 0)
			vector.x = 1;
		if (vector.y == 0)
			vector.y = 1;
		if (vector.z == 0)
			vector.z = 1;

		return new Vector3(1.0f / vector.x, 1.0f / vector.y, 1.0f / vector.z);
	}

	// 0 = no, 1 = unit, 2 = position
	public static int RequiresTarget(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return 0;
			case AbilityType.Swarm:
				return 1;
			default:
				return 0;
		}
	}

	// 0 = no, 1 = unit, 2 = position
	public static string AbilityName(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return "Degrade";
			case AbilityType.Swarm:
				return "Swarm";
			default:
				return "default";
		}
	}

	static Unit GetUnitFromCol(Collider col)
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
