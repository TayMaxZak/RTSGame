using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_HealField : MonoBehaviour {
	[SerializeField]
	private Effect_Point pointEffectPrefab;
	private Effect_Point pointEffect;

	private int team; // Doesn't need to be public

	private bool isActive = false;
	private bool isBorrowing = false; // Are we holding resources from our team's commander

	private Manager_Game gameManager;
	private GameRules gameRules;

	private Unit parentUnit;
	private Commander command;

	private Coroutine giveResourcesCoroutine;

	// Use this for initialization
	void Start()
	{
		parentUnit = GetComponent<Unit>();
		team = parentUnit.team;

		gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>();
		gameRules = gameManager.GameRules;

		command = gameManager.Commanders[team];

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(isActive);
	}

	void Update()
	{
		pointEffect.transform.position = transform.position; // Move effect to center of user

		if (isActive)
		{
			Collider[] cols = Physics.OverlapSphere(transform.position, gameRules.ABLYhealFieldRange, gameRules.entityLayerMask);
			List<Unit> units = new List<Unit>();
			for (int i = 0; i < cols.Length; i++)
			{
				Unit unit = GetUnitFromCol(cols[i]);

				if (!unit)
					continue;

				if (units.Contains(unit)) // Multiple colliders for one unit
					continue;

				if (unit.GetHP().x >= unit.GetHP().y) // If at full HP, don't attempt to heal
					continue;

				if (unit != parentUnit) // Don't add ourselves
					units.Add(unit);
			}

			for (int i = 0; i < units.Count; i++) // For each ally unit, add health
			{
				if (units[i].team == team)
				{
					float missingHealth = units[i].GetHP().x / units[i].GetHP().y;
					units[i].DamageSimple(-(gameRules.ABLYhealFieldAllyGPS + gameRules.ABLYhealFieldAllyGPSBonusMult * missingHealth) * Time.deltaTime, 0); // Add health based on missing health

					units[i].AddStatus(new Status(gameObject, StatusType.CriticalBurnImmune));
				}

				if (units.Count > 0) // If at least one other unit is being healed, heal ourselves by a flat amount regardless of ally count
				{
					parentUnit.DamageSimple(-(gameRules.ABLYhealFieldUserGPS * Time.deltaTime) / units.Count, 0);
					units[i].AddStatus(new Status(gameObject, StatusType.CriticalBurnImmune));
				}
			}

			if (units.Count == 0)
				pointEffect.SetEffectActive(true, false);
			else
				pointEffect.SetEffectActive(true, true);
		}
	}

	public bool ToggleActive()
	{


		if (!isActive) // About to become active
		{
			// If we are not already borrowing and there are no resources to borrow, don't activate this ability
			if (!isBorrowing && !command.TakeResources(gameRules.ABLYhealFieldResCost))
				return false;

			isBorrowing = true;

			if (giveResourcesCoroutine != null)
				StopCoroutine(giveResourcesCoroutine);
		}
		else
		{
			giveResourcesCoroutine = StartCoroutine(GiveResourcesCoroutine(gameRules.ABLYhealFieldResTime));
		}

		isActive = !isActive;

		pointEffect.SetEffectActive(isActive);

		return true;
	}

	IEnumerator GiveResourcesCoroutine(float time)
	{
		yield return new WaitForSeconds(time);
		command.GiveRes(gameRules.ABLYhealFieldResCost);
		isBorrowing = false;
	}

	public void End()
	{
		// TODO: This should happen with a delay as well
		if (isBorrowing)
		{
			GameObject go = Instantiate(new GameObject());
			Util_ResDelay resDelay = go.AddComponent<Util_ResDelay>();
			resDelay.GiveResAfterDelay(gameRules.ABLYhealFieldResCost, gameRules.ABLYhealFieldResTime, team);
		}
		pointEffect.End();
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
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
