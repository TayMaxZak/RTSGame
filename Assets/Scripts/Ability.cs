using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
	ArmorDrain,
	ArmorRegen,
	SpawnSwarm,
	MoveSwarm,
	SelfDamage,
	ShieldProject
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
	public Ability_Effect pointEffect; // Ability VFX and SFX at a particular position
	[HideInInspector]
	public Unit user;
	[HideInInspector]
	public AbilityTarget target;
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
	[System.NonSerialized]
	[HideInInspector]
	public int stacks = 0;
	[System.NonSerialized]
	[HideInInspector]
	public float pool = 0; // Arbitrary float pool which sits alongside energy pool

	public void Init(Unit user, GameRules gameRules)
	{
		// Set user and game rules to use later
		this.user = user;
		this.gameRules = gameRules;

		// Spawn ability effect prefab
		AbilityUtils.InitAbility(this);
	}

	public void Activate(AbilityTarget target)
	{
		this.target = target;
		AbilityUtils.StartAbility(this);
	}

	public void End()
	{
		if (!pointEffect) // Should never happen
			AbilityUtils.InitAbility(this);
		AbilityUtils.EndAbility(this);
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
				{
					//GameObject go = Object.Instantiate(Resources.Load(ability.type.ToString() + "Effect") as GameObject, ability.user.transform.position, Quaternion.identity);
					//ability.pointEffect = go.GetComponent<Ability_Effect>();
				}
				break;
			case AbilityType.SpawnSwarm:
				{
					ability.stacks = ability.gameRules.ABLYswarmMaxUses;
					//ability.unitList.Add(ability.user);
				}
				break;
			case AbilityType.ShieldProject:
				{
					ability.pool = ability.gameRules.ABLYshieldProjectMaxPool;
				}
				break;
			default:
				break;
		}

		if (ability.pointEffect)
			ability.pointEffect.SetEffectActive(ability.isActive);
	}

	public static void StartAbility(Ability ability)
	{
		if (ability.curCooldown > 0)
		{
			return;
		}
			
		ability.curCooldown = 1;

		if (ActivationStyle(ability.type) == 1)
			ability.isActive = true;
		else if (ActivationStyle(ability.type) == 2)
			ability.isActive = !ability.isActive;

		if (ability.pointEffect)
			ability.pointEffect.SetEffectActive(ability.isActive);

		switch (ability.type) // Ability is off cooldown
		{
			case AbilityType.ArmorDrain:
				break;
			case AbilityType.SpawnSwarm:
				{
					if (ability.stacks > 0) // If we have swarms in reserve, spawn one and tell all swarms to move
					{
						Particles_Swarming swarmManager = ability.user.GetComponent<Particles_Swarming>();

						swarmManager.SetTarget(ability.target);
					
						ability.stacks--;
						swarmManager.SpawnSwarm();
					}
				}
				break;
			case AbilityType.MoveSwarm:
				{
					Particles_Swarming swarmManager = ability.user.GetComponent<Particles_Swarming>();
					swarmManager.SetTarget(ability.target);
				}
				break;
			case AbilityType.SelfDamage:
				Object.Instantiate(Resources.Load(ability.type.ToString() + "Effect") as GameObject, ability.user.transform.position, Quaternion.identity);
				ability.user.TrueDamage(ability.user.GetHP().y * 0.8f, 0);
				break;
			case AbilityType.ShieldProject:
				{
					// TODO: Clean code up
					if (ability.target.unit.GetShieldPool() <= Mathf.Epsilon || ability.target.unit == ability.user)
					{
						Flagship flag = ability.target.unit.gameObject.GetComponent<Flagship>();

						if (!flag)
						{
							if (ability.target.unit != ability.user)
								Object.Instantiate(Resources.Load(ability.type.ToString() + "Effect") as GameObject, ability.target.unit.transform.position, Quaternion.identity);
							else
							{
								if (ability.unitList.Count > 0 && ability.unitList[0] != ability.user)
									Object.Instantiate(Resources.Load(ability.type.ToString() + "Effect") as GameObject, ability.target.unit.transform.position, Quaternion.identity);
							}

							if (ability.unitList.Count > 0)
							{
								if (ability.unitList[0] != ability.user)
								{
									ability.pool = ability.unitList[0].GetShieldPool();
									ability.unitList[0].RemoveShield(); // Remove shield from previous user ONLY IF ITS OUR SHIELD
								}
							}

							if (ability.target.unit == ability.user)
							{


								if (ability.unitList.Count > 0)
									ability.unitList[0] = ability.target.unit; // Remember this user as "previous user" for the future
								else
									ability.unitList.Add(ability.target.unit); // Remember this user as "previous user" for the future
							}
							else
							{
								ability.target.unit.RecieveShield(ability.pool, ability.user.team); // Apply shield to new user

								if (ability.unitList.Count > 0)
									ability.unitList[0] = ability.target.unit; // Remember this user as "previous user" for the future
								else
									ability.unitList.Add(ability.target.unit); // Remember this user as "previous user" for the future
							}
						}
					}
					break;
				}
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
			if (ability.pointEffect)
				ability.pointEffect.SetEffectActive(ability.isActive);

			return;
		}

		if (!ability.isActive) // If ability is not being used. Passives are always off
		{
			
			if (ActivationStyle(ability.type) == 2) // If it's a toggle ability
				ability.curEnergy = Mathf.Min(ability.curEnergy + Time.deltaTime * DeltaDurations(ability.type).z, 1); // Restore energy according to its reset duration


			switch (ability.type)
			{
				case AbilityType.ArmorDrain:
					{
						if (!ability.pointEffect)
						{
							GameObject go = Object.Instantiate(Resources.Load(ability.type.ToString() + "Effect") as GameObject, ability.user.transform.position, Quaternion.identity);
							ability.pointEffect = go.GetComponent<Ability_Effect>();
							ability.pointEffect.SetEffectActive(ability.isActive);
						}

						ability.pointEffect.transform.position = ability.user.transform.position; // Move effect to center of user
					}
					break;
				case AbilityType.ArmorRegen: // Regenerate armor over time based on missing armor
					{
						int regenIndex = Mathf.Max(Mathf.CeilToInt(5 * (1 - (ability.user.GetHP().z / ability.user.GetHP().w))) - 1, 0);
						ability.user.TrueDamage(0, -ability.gameRules.ABLYarmorRegenHPS[regenIndex] * Time.deltaTime);
					}
					break;
				
				default:
					break;
			}
			return; // Leave, ability isn't being used
		}

		if (ActivationStyle(ability.type) == 2) // If it's a toggle ability
		{
			ability.curEnergy -= Time.deltaTime * DeltaDurations(ability.type).y; // Consume energy
		}

		// Active ability tick
		switch (ability.type)
		{
			case AbilityType.ArmorDrain: // Find all enemies in a radius and damage them over time
				{
					ability.pointEffect.transform.position = ability.user.transform.position; // Move effect to center of user

					Collider[] cols = Physics.OverlapSphere(ability.user.transform.position, ability.gameRules.ABLYarmorDrainRange, ability.gameRules.entityLayerMask);
					List<Unit> units = new List<Unit>();
					for (int i = 0; i < cols.Length; i++)
					{
						bool hasDrainAbility = false;
						Unit unit = GetUnitFromCol(cols[i]);

						if (!unit)
							continue;

						if (units.Contains(unit)) // Multiple colliders for one unit
							continue;

						foreach (Ability a in unit.abilities)
							if (a.type == AbilityType.ArmorDrain) // Can't drain another drain-capable unit
								hasDrainAbility = true;

						if (unit != ability.user && !hasDrainAbility && unit.GetHP().z > 0) // Don't add ourselves
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
							ability.user.TrueDamage(0, -(ability.gameRules.ABLYarmorDrainAPS + ability.gameRules.ABLYarmorDrainAPSBonusMult * units.Count) * Time.deltaTime);
					}

					if (units.Count == 0)
						ability.pointEffect.SetEffectActive(ability.isActive, false);
					else
						ability.pointEffect.SetEffectActive(ability.isActive, true);
				}
				break;
			case AbilityType.ShieldProject:
				{
					// TODO: Fix pool zeroing bug!
					if (ability.unitList.Count > 0)
					{
						if (ability.unitList[0] == ability.user) // Regenerate pool rapidly while shield is inacive
						{
							ability.pool = Mathf.Clamp(ability.pool + ability.gameRules.ABLYshieldProjectInactivePPS * Time.deltaTime, 0, ability.gameRules.ABLYshieldProjectMaxPool);
						}
						else if (!ability.unitList[0]) // If the shield is active but our charge accidentally dies, self-cast
						{
							ability.target.unit = ability.user; // Avoid manually setting target if possible, but necessary here
							StartAbility(ability);
						}
					}
				}
				break;
			case AbilityType.SpawnSwarm:
				{
					if (ability.stacks < ability.gameRules.ABLYswarmMaxUses) // If we've already used the ability (so we should have a target already set)
					{
						Particles_Swarming swarmManager = ability.user.GetComponent<Particles_Swarming>();
						if (!swarmManager.GetTarget().unit) // Self-cast if the target dies
						{
							swarmManager.SetTarget(new AbilityTarget(ability.user));
						}
					}
				}
				break;
			default:
				break;
		}
	}


	public static void EndAbility(Ability ability)
	{
		switch (ability.type)
		{
			case AbilityType.ArmorDrain:
				{
					ability.pointEffect.End();
				}
				break;
			case AbilityType.ShieldProject:
				{
					if (ability.unitList.Count > 0)
					{
						Object.Instantiate(Resources.Load(ability.type.ToString() + "Effect") as GameObject, ability.target.unit.transform.position, Quaternion.identity);

						if (ability.unitList[0] != ability.user)
						{
							ability.pool = ability.unitList[0].GetShieldPool();
							ability.unitList[0].RemoveShield(); // Remove shield from previous user ONLY IF ITS OUR SHIELD
						}
					}
					else
					{
						Object.Instantiate(Resources.Load(ability.type.ToString() + "Effect") as GameObject, ability.user.transform.position, Quaternion.identity);
					}
				}
				break;
			case AbilityType.SelfDamage:
				{

				}
				break;
			default:
				break;
		}
	}


	public static int ActivationStyle(AbilityType type) // 0 = passive, 1 = instant, 2 = toggle
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return 2;
			case AbilityType.ArmorRegen:
				return 0;
			default:
				return 1; // Most abilities are instant actives
		}
	}

	// X = Cooldown, Y = Active Duration, Z = Reset Duration
	// All default to 1 second
	public static Vector3 DeltaDurations(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return DeltaOf(new Vector3(2.0f, 15.0f, 30.0f));
			case AbilityType.SpawnSwarm:
				return DeltaOf(new Vector3(3.0f, 0, 0));
			case AbilityType.MoveSwarm:
				return DeltaOf(new Vector3(0.5f, 0, 0));
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
			case AbilityType.SpawnSwarm:
				return 1;
			case AbilityType.MoveSwarm:
				return 1;
			case AbilityType.ShieldProject:
				return 1;
			default:
				return 0;
		}
	}

	// Display name of ability
	public static string AbilityName(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return "Disintegrate";
			case AbilityType.SpawnSwarm:
				return "Deploy Swarm";
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
