using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_NoReload : Ability
{
	private float energy;
	private Vector3 deltaDurations;

	[SerializeField]
	private Effect_Point pointEffectPrefab;
	private Effect_Point pointEffect;
	//private ParticleSystem.MainModule mainModule;

	private Turret[] turrets;
	private Ability_SelfDestruct selfDestruct;

	private bool isActive = false;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.NoReload;
		InitCooldown();

		energy = 1;
		deltaDurations = AbilityUtils.GetDeltaDurations(abilityType);

		displayInfo.displayFill = true;
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		turrets = parentUnit.GetTurrets();
		selfDestruct = parentUnit.GetComponent<Ability_SelfDestruct>();

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(isActive, isActive);
		//mainModule = pointEffect.GetMainPS().main;
	}

	public override void End()
	{
		pointEffect.End();
	}

	new void Update()
	{
		base.Update();

		pointEffect.transform.position = transform.position; // Move effect to center of user
		pointEffect.transform.rotation = transform.rotation; // Move effect to center of user

		if (isActive)
		{
			if (energy > 0) // Needs energy to run
			{
				// Consume energy according to active duration (unless we are self destructing)
				if (!selfDestruct.GetIsActive())
				{
					pointEffect.SetEffectActive(isActive, isActive);
					//mainModule.simulationSpeed = 1f;

					energy -= deltaDurations.y * Time.deltaTime;
				}
				else
				{
					pointEffect.SetEffectActive(isActive, isActive);
					//mainModule.simulationSpeed = 0.1f;

					
				}
				Display(1 - energy);
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

		foreach (Turret t in turrets)
			t.SetInfiniteAmmo(isActive);

		pointEffect.SetEffectActive(isActive, isActive);
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
		//audioLoop.SetVisible(visible);
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
