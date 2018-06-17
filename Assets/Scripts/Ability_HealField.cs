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

	void Awake()
	{
		abilityType = AbilityType.HealField;
		InitCooldown();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>();

		command = gameManager.GetCommander(team);

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(isActive);
	}

	public override void End()
	{
		if (isBorrowing)
		{
			GameObject go = Instantiate(new GameObject());
			Util_ResDelay resDelay = go.AddComponent<Util_ResDelay>();
			resDelay.GiveResAfterDelay(gameRules.ABLYhealFieldResCost, gameRules.ABLYhealFieldResTime, team);
		}
		pointEffect.End();
	}

	new void Update()
	{
		base.Update();

		pointEffect.transform.position = transform.position; // Move effect to center of user

		if (isActive)
		{
			Collider[] cols = Physics.OverlapSphere(transform.position, gameRules.ABLYhealFieldRange, gameRules.entityLayerMask);
			List<Unit> units = new List<Unit>();
			for (int i = 0; i < cols.Length; i++)
			{
				Unit unit = GetUnitFromCol(cols[i]);

				if (!unit) // Only works on units
					continue;

				if (units.Contains(unit)) // Ignore multiple colliders for one unit
					continue;

				if (unit.GetHP().x >= unit.GetHP().y) // If at full HP, don't attempt to heal
					continue;

				if (unit.GetType() == typeof(Unit_Flagship)) // Can't heal Flagships
					continue;

				if (unit.team != team) // Must be on our team
					continue;

				if (unit != parentUnit) // Don't add ourselves
					units.Add(unit);
			}

			for (int i = 0; i < units.Count; i++) // For each ally unit, add health
			{
				Vector4 getHP = units[i].GetHP();
				float missingHealth = getHP.y - getHP.x; // Bonus based on missing health
				units[i].DamageSimple(-(gameRules.ABLYhealFieldAllyGPS + gameRules.ABLYhealFieldAllyGPSBonusMult * missingHealth) * Time.deltaTime, 0); // Add health

				units[i].AddStatus(new Status(gameObject, StatusType.CriticalBurnImmune));

				if (units.Count > 0) // If at least one other unit is being healed, heal ourselves by a flat amount regardless of ally count
				{
					parentUnit.DamageSimple(-(gameRules.ABLYhealFieldUserGPS * Time.deltaTime) / units.Count, 0);
					parentUnit.AddStatus(new Status(gameObject, StatusType.CriticalBurnImmune));
					
				}
			}

			if (units.Count == 0)
				pointEffect.SetEffectActive(true, false);
			else
				pointEffect.SetEffectActive(true, true);
		}
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (!offCooldown)
			return;

		base.UseAbility(target);

		if (!isActive) // About to become active
		{
			// If we are not already borrowing and there are no resources to borrow, don't activate this ability
			if (!isBorrowing && !command.TakeResources(gameRules.ABLYhealFieldResCost))
				return;

			isBorrowing = true;
			//ability.stacks = -1;

			if (giveResourcesCoroutine != null)
				StopCoroutine(giveResourcesCoroutine);
		}
		else
		{
			giveResourcesCoroutine = StartCoroutine(GiveResourcesCoroutine(gameRules.ABLYhealFieldResTime));
		}

		isActive = !isActive;

		pointEffect.SetEffectActive(isActive);
	}

	IEnumerator GiveResourcesCoroutine(float time)
	{
		yield return new WaitForSeconds(time);
		command.GiveResources(gameRules.ABLYhealFieldResCost);

		isBorrowing = false;
		//ability.stacks = 0;
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
