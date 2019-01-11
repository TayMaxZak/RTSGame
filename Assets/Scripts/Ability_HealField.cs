using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_HealField : Ability
{
	//private AbilityOld ability;
	[SerializeField]
	private Effect_Point pointEffectPrefab;
	private Effect_Point pointEffect;

	private bool isActive = false;
	private bool isBorrowing = false; // Are we holding resources from our team's commander

	private Manager_Game gameManager;

	private Commander command;

	private Coroutine giveResourcesCoroutine;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.HealField;
		InitCooldown();

		gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		command = gameManager.GetCommander(team);

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(isActive);
	}

	public override void End()
	{
		if (isBorrowing)
		{
			GameObject go = new GameObject();
			Util_ResDelay resDelay = go.AddComponent<Util_ResDelay>();
			resDelay.GiveResAfterDelay(gameRules.ABLY_healFieldResCost, gameRules.WRCK_lifetime, team);
		}
		pointEffect.End();
	}

	new void Update()
	{
		base.Update();

		pointEffect.transform.position = transform.position; // Move effect to center of user

		if (isActive)
		{
			Collider[] cols = Physics.OverlapSphere(transform.position, gameRules.ABLY_healFieldRange, gameRules.entityLayerMask);
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

				if (unit.GetHP().x >= unit.GetHP().y) // If at full HP, don't attempt to heal
					continue;

				if (unit.Type == EntityType.Flagship) // Can't heal Flagships
					continue;

				if (unit.team != team) // Must be on our team
					continue;

				units.Add(unit);
			}

			for (int i = 0; i < units.Count; i++) // For each ally unit, add health
			{
				units[i].AddFragileHealth((gameRules.ABLY_healFieldAllyGPS + gameRules.ABLY_healFieldAllyGPSBonusMult * units[i].GetHP().y) * Time.deltaTime);
				units[i].AddStatus(new Status(gameObject, StatusType.CriticalBurnImmune));
			}

			parentUnit.AddFragileHealth((gameRules.ABLY_healFieldAllyGPS * gameRules.ABLY_healFieldUserGPSMult + gameRules.ABLY_healFieldAllyGPSBonusMult * parentUnit.GetHP().y) * Time.deltaTime);
			parentUnit.AddStatus(new Status(gameObject, StatusType.CriticalBurnImmune));
		}
		else
			CheckDisplayConditions();
	}

	void CheckDisplayConditions()
	{
		if (!isBorrowing && command.GetResources() < gameRules.ABLY_healFieldResCost)
			DisplayUsable(true);
		else
			DisplayUsable(false);
	}

	void DisplayUsable(bool newVal)
	{
		if (newVal == displayInfo.displayInactive)
			return;

		displayInfo.displayInactive = newVal;
		UpdateDisplay(abilityIndex, false);
	}

	void DisplayBorrowing(bool isBorrowing)
	{
		displayInfo.displayIconB = isBorrowing;
		if (isBorrowing)
		{
			// Active has to be inverted here because where this function is called, isActive is only flipped afterwards
			displayInfo.iconBState = !isActive ? 1 : 2;
		}

		UpdateDisplay(abilityIndex, false, true);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (suspended)
			return;

		if (!offCooldown)
			return;

		base.UseAbility(target);

		ToggleActive();
	}

	void ToggleActive()
	{
		SetActive(!isActive);
	}

	void SetActive(bool newActive)
	{
		if (newActive) // About to become active
		{
			// If we are not already borrowing and there are no resources to borrow, don't activate this ability
			if (!isBorrowing && !command.TakeResources(gameRules.ABLY_healFieldResCost))
			{
				ResetCooldown();
				return;
			}

			isBorrowing = true;
			DisplayBorrowing(isBorrowing);

			if (giveResourcesCoroutine != null)
				StopCoroutine(giveResourcesCoroutine);
		}
		else
		{
			if (isBorrowing)
				giveResourcesCoroutine = StartCoroutine(GiveResourcesCoroutine(gameRules.ABLY_healFieldResTime));
		}

		isActive = newActive;

		pointEffect.SetEffectActive(isActive);
	}

	IEnumerator GiveResourcesCoroutine(float time)
	{
		DisplayBorrowing(isBorrowing);
		yield return new WaitForSeconds(time);
		command.GiveResources(gameRules.ABLY_healFieldResCost);

		isBorrowing = false;
		DisplayBorrowing(isBorrowing);
	}

	public override void Suspend()
	{
		base.Suspend();

		SetActive(false);
		StartCooldown();
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
