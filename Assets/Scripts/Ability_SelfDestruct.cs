using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_SelfDestruct : Ability
{
	private float energy;
	private Vector3 deltaDurations;

	[SerializeField]
	private Effect_Point pointEffectPrefab;
	private Effect_Point pointEffect;

	[SerializeField]
	private GameObject blastEffectPrefab;
	[SerializeField]
	private GameObject damageCloudPrefab;

	private bool isActive = false;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.SelfDestruct;
		InitCooldown();

		energy = 1;
		deltaDurations = AbilityUtils.GetDeltaDurations(AbilityType.SelfDestruct);

		displayInfo.displayFill = true;
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(isActive, isActive);
	}

	public override void End()
	{
		pointEffect.End();
		//audioLoop.End();
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

				// Deal damage over time
				parentUnit.DamageSimple(gameRules.ABLY_selfDestructDPSSelf * Time.deltaTime, 0);
				parentUnit.AddFragileHealth(gameRules.ABLY_selfDestructDPSSelf * Time.deltaTime);

				// Getting closer to detonation
				if (energy < 0.5f)
					pointEffect.SetEffectActive(true, true);
				else
					pointEffect.SetEffectActive(true, false);
			}
			else
			{
				// Explode
				Explode();
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

		if (isActive)
		{
			parentUnit.AddStatus(new Status(gameObject, StatusType.SelfDestructSpeedBuff));
		}
		else
		{
			parentUnit.RemoveStatus(new Status(gameObject, StatusType.SelfDestructSpeedBuff));
		}
	}

	void Display(float fill)
	{
		displayInfo.fill = fill;
		UpdateDisplay(abilityIndex, false);
	}

	void Explode()
	{
		Collider[] cols = Physics.OverlapSphere(transform.position, gameRules.ABLY_selfDestructRange, gameRules.entityLayerMask);
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

			//if (unit.team != team) // Must be on our team
				//continue;

			units.Add(unit);
		}

		for (int i = 0; i < units.Count; i++) // For each ally unit, deal damage
		{
			float flagMult = units[i].Type == EntityType.Flagship ? gameRules.ABLY_selfDestructDamageFlagMult : 1;
			if (units[i].team != team)
				units[i].Damage(gameRules.ABLY_selfDestructDamage * flagMult, (units[i].transform.position - transform.position).magnitude, DamageType.Wreck);
			else
				units[i].Damage(gameRules.ABLY_selfDestructDamage * gameRules.DMG_ffDamageMultSplash * flagMult, (units[i].transform.position - transform.position).magnitude, DamageType.Wreck);
		}


		pointEffect.End();
		Instantiate(blastEffectPrefab, transform.position, Quaternion.identity);
		//Instantiate(damageCloudPrefab, transform.position, Quaternion.identity);
		
		parentUnit.Die(DamageType.Internal);
	}

	public override void Suspend()
	{
		base.Suspend();

		SetActive(false);
	}

	public override void SetEffectsVisible(bool visible)
	{
		pointEffect.SetVisible(visible);
		//audioLoop.SetVisible(visible);
	}

	public bool GetIsActive()
	{
		return isActive;
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
