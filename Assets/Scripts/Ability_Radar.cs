using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Radar : Ability
{
	private float energy;
	private Vector3 deltaDurations;

	private float remainingPulseTime = -1;
	private float currentRadius;

	[Header("Effects")]
	[SerializeField]
	private Effect_Point pointEffectPrefab;
	private Effect_Point pointEffect;
	[SerializeField]
	private GameObject detectEffect;
	[SerializeField]
	private GameObject ghostCorvette;
	[SerializeField]
	private GameObject ghostFrigate;
	[SerializeField]
	private GameObject ghostCruiser;

	[Header("Pulse")]
	[SerializeField]
	private AudioSource pulseSound;
	[SerializeField]
	private GameObject scanningSphere;

	[Header("Audio")]
	[SerializeField]
	private AudioEffect_Loop audioLoopPrefab;
	private AudioEffect_Loop audioLoop;

	private bool isActive = true;

	private List<Unit> unitsThisPulse;
	private List<GameObject> ghostsThisPulse;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.Radar;
		InitCooldown();

		energy = 1;
		deltaDurations = AbilityUtils.GetDeltaDurations(abilityType);

		displayInfo.displayFill = true;

		unitsThisPulse = new List<Unit>();
		ghostsThisPulse = new List<GameObject>();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		scanningSphere.transform.SetParent(null);
		scanningSphere.SetActive(false);

		//pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		//pointEffect.SetEffectActive(isActive);

		//audioLoop = Instantiate(audioLoopPrefab, transform.position, Quaternion.identity);
		//audioLoop.SetEffectActive(isActive);
	}

	public override void End()
	{
		//pointEffect.End();
		//audioLoop.End();
	}

	new void Update()
	{
		base.Update();

		if (remainingPulseTime > 0)
		{
			if (!scanningSphere.activeSelf)
				scanningSphere.SetActive(true);

			//scanningSphere.transform.position = transform.position;
			currentRadius = (gameRules.ABLY_radarPulseTime - remainingPulseTime) * gameRules.ABLY_radarPulseRange / gameRules.ABLY_radarPulseTime;
			Debug.Log(currentRadius);

			scanningSphere.transform.localScale = currentRadius * Vector3.one * 2;
			Collider[] cols = Physics.OverlapSphere(scanningSphere.transform.position, Mathf.RoundToInt(currentRadius), gameRules.entityLayerMask);

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

				if (unit.team == team) // They must be enemies
					continue;

				bool hasRadar = false; // Can't detect another radar-capable unit
				foreach (Ability a in unit.abilities)
					if (a.GetAbilityType() == AbilityType.Radar)
						hasRadar = true;
				if (hasRadar)
					continue;

				if (unitsThisPulse.Contains(unit))
					continue;

				//Debug.Log("team " + unit.team);
				units.Add(unit);
				unitsThisPulse.Add(unit);
			}

			for (int i = 0; i < units.Count; i++) // For each unit, subtract armor
			{
				//units[i].Damage(1, 0, DamageType.Internal);
				Instantiate(detectEffect, units[i].transform.position, units[i].transform.rotation);
				if (!units[i].VisibleBy(team)) // Don't add a ghost for units we can see
				{
					if (units[i].GetSize() == EntitySize.Corvette)
					{
						GameObject ghost = Instantiate(ghostCorvette, units[i].transform.position, units[i].transform.rotation);
						ghostsThisPulse.Add(ghost);
					}
					else if (units[i].GetSize() == EntitySize.Frigate)
					{
						GameObject ghost = Instantiate(ghostFrigate, units[i].transform.position, units[i].transform.rotation);
						ghostsThisPulse.Add(ghost);
					}
					else if (units[i].GetSize() == EntitySize.Cruiser)
					{
						GameObject ghost = Instantiate(ghostCruiser, units[i].transform.position, units[i].transform.rotation);
						ghostsThisPulse.Add(ghost);
					}
				}
				//if (hasSuperlaser)
				//{
				//	Status markStatus = new Status(gameObject, StatusType.SuperlaserMark); // TODO: Optimize?
				//	markStatus.SetTimeLeft(gameRules.ABLY_armorDrainDPSEnemy * Time.deltaTime);
				//	units[i].AddStatus(markStatus);
				//}
			}

			remainingPulseTime -= Time.deltaTime; // This is after to ensure we don't go past the range
			if (remainingPulseTime < 0)
				scanningSphere.SetActive(false);
		}

		//pointEffect.transform.position = transform.position; // Move effect to center of user
		//audioLoop.transform.position = transform.position; // Move effect to center of user

		//if (isActive)
		//{
		//	if (energy > 0) // Needs energy to run
		//	{
		//		// Consume energy according to active duration
		//		energy -= deltaDurations.y * Time.deltaTime;
		//		Display(1 - energy);

		//		Collider[] cols = Physics.OverlapSphere(transform.position, gameRules.ABLY_armorDrainRange, gameRules.entityLayerMask);
		//		List<Unit> units = new List<Unit>();
		//		for (int i = 0; i < cols.Length; i++)
		//		{
		//			Unit unit = GetUnitFromCol(cols[i]);

		//			if (!unit) // Only works on units
		//				continue;

		//			if (units.Contains(unit)) // Ignore multiple colliders for one unit
		//				continue;

		//			if (unit == parentUnit) // Don't add ourselves
		//				continue;

		//			if (unit.Type == EntityType.Flagship) // Can't drain Flagships
		//				continue;

		//			if (unit.GetHP().z <= 0) // They must have some armor
		//				continue;

		//			bool hasDrain = false; // Can't drain another drain-capable unit
		//			foreach (Ability a in unit.abilities)
		//				if (a.GetAbilityType() == AbilityType.ArmorDrain)
		//					hasDrain = true;
		//			if (hasDrain)
		//				continue;
					
		//			units.Add(unit);
		//		}

		//		// TODO: Sort unit list to have allies first

		//		int allyCount = 0;
		//		int enemyCount = 0;

		//		for (int i = 0; i < units.Count && i < gameRules.ABLY_armorDrainMaxVictims; i++) // For each unit, subtract armor
		//		{
		//			if (units[i].team == parentUnit.team) // Ally
		//			{
		//				if (parentUnit.GetHP().z < parentUnit.GetHP().w) // Only damage allies if we have missing armor
		//				{
		//					units[i].DamageSimple(0, gameRules.ABLY_armorDrainDPSAlly * Time.deltaTime, true);
		//					allyCount++;
		//				}
		//			}
		//			else // Enemy
		//			{
		//				units[i].DamageSimple(0, gameRules.ABLY_armorDrainDPSEnemy * Time.deltaTime, true);
		//				enemyCount++;
		//			}
		//		}

		//		// Add armor to us based on number of units
		//		parentUnit.DamageSimple(0, -(gameRules.ABLY_armorDrainGPSEnemy + gameRules.ABLY_armorDrainGPSBonusMult * enemyCount) * (enemyCount) * Time.deltaTime, true);
		//		parentUnit.DamageSimple(0, -gameRules.ABLY_armorDrainGPSAlly * (allyCount) * Time.deltaTime, true);

		//		if ((allyCount + enemyCount) == 0)
		//			pointEffect.SetEffectActive(true, false);
		//		else
		//			pointEffect.SetEffectActive(true, true);
		//	}
		//	else
		//	{
		//		Toggle(); // Toggle to inactive and put on cooldown
		//		StartCooldown();
		//	}
		//}
		//else // Inactive
		//{
		//	if (energy < 1)
		//	{
		//		// Restore energy according to reset duration
		//		energy += deltaDurations.z * Time.deltaTime;
		//		Display(1 - energy);
		//	}
		//}
	}

	public void Pulse()
	{
		pulseSound.Play();
		scanningSphere.transform.position = transform.position;
		remainingPulseTime = gameRules.ABLY_radarPulseTime;
		unitsThisPulse.Clear();
		//List<GameObject> toDelete = new List<GameObject>();
		foreach (GameObject ghost in ghostsThisPulse)
		{
			//toDelete.Add(ghost);
			Destroy(ghost);
		}
		ghostsThisPulse.Clear();
		//foreach (GameObject del in toDelete)
		//{
		//	Destroy(del);
		//}
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (suspended)
			return;

		if (!offCooldown)
			return;

		base.UseAbility(target);

		Pulse();
		//Toggle();
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
		//pointEffect.SetVisible(visible);
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
