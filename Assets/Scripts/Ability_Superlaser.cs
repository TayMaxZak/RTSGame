using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Superlaser : Ability
{
	[SerializeField]
	private Hitscan shotTemplate;
	[SerializeField]
	private Transform[] sourcePositions;
	[SerializeField]
	private Effect_Line laserEffectPrefab;
	private Effect_Line[] laserEffects;
	[SerializeField]
	private Effect_Point startEffectPrefab;
	private Effect_Point[] startEffects;
	[SerializeField]
	private GameObject pointEffectBreakPrefab;
	private GameObject pointEffectBreak;

	[SerializeField]
	private Effect_Point countdownStartPrefab;
	private Effect_Point countdownStart;
	[SerializeField]
	private Effect_Point countdownStartRangePrefab;
	private Effect_Point countdownStartRange;
	[SerializeField]
	private GameObject countdownFinishPrefab;
	[SerializeField]
	private GameObject countdownFinishExplosion;

	private Unit targetUnit;
	private bool checkIfDead = false;
	private float initDistance = 0;
	private Vector3 initPosition;

	private float aimThreshold = 0.001f;
	private int attemptShotWhen;

	private int state = 0; // 0 = standby, 1 = targeting (has a target, is turning towards it), 2 = countdown (will shoot after a fixed time delay)
	private Coroutine countdownCoroutine;

	private UI_AbilBar_Superlaser abilityBar;

	private Manager_Hitscan hitscans;

	new void Awake()
	{
		base.Awake();
		abilityType = AbilityType.Superlaser;
		InitCooldown();

		stacks = gameRules.ABLYsuperlaserInitStacks;

		displayInfo.displayStacks = true;

		hitscans = GameObject.FindGameObjectWithTag("HitscanManager").GetComponent<Manager_Hitscan>();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		abilityBar = parentUnit.hpBar.GetComponent<UI_AbilBar_Superlaser>();
		Display();

		laserEffects = new Effect_Line[sourcePositions.Length];
		startEffects = new Effect_Point[sourcePositions.Length];
		for (int i = 0; i< sourcePositions.Length; i++)
		{
			laserEffects[i] = Instantiate(laserEffectPrefab, sourcePositions[i].position, Quaternion.identity);
			laserEffects[i].SetEffectActive(0);
			startEffects[i] = Instantiate(startEffectPrefab, sourcePositions[i].position, Quaternion.identity);
			startEffects[i].SetEffectActive(false);
		}
	}

	void Display()
	{
		displayInfo.stacks = stacks;
		if (stacks <= 0)
			displayInfo.displayInactive = true;
		else
			displayInfo.displayInactive = false;
		UpdateDisplay(abilityIndex, true);
		UpdateAbilityBar();
	}

	protected override void UpdateAbilityBar()
	{
		abilityBar.SetStacks(stacks, false);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (!offCooldown)
			return;

		base.UseAbility(target);

		BeginTargeting(target.unit);
		ResetCooldown(); // Cooldown is applied later
	}

	void BeginTargeting(Unit unit)
	{
		if (state != 0) // Must be on standby
			return;

		if (stacks <= 0) // At least one stack is required to activate this ability
			return;

		if (unit.team == team) // Cannot target allies
			return;

		if (InRange(unit.transform, gameRules.ABLYsuperlaserRangeUse)) // In range
		{
			targetUnit = unit; // Set new target
			checkIfDead = true; // Our target may become null

			parentUnit.SetAbilityGoal(new AbilityTarget(targetUnit)); // Turn towards it
			attemptShotWhen = Time.frameCount + 3; // We should start aim checking in 1 frame from now
			state = 1; // Targeting state

			ResetCooldown(); // Don't use cooldown yet
		}
	}

	void BeginCountdown()
	{
		if (countdownCoroutine == null) // Only run once
		{
			state = 2; // Countdown state

			countdownStart = Instantiate(countdownStartPrefab, transform.position, Quaternion.identity);

			countdownCoroutine = StartCoroutine(CountdownCoroutine()); // Fire after a delay

			for (int i = 0; i < sourcePositions.Length; i++)
			{
				startEffects[i].SetEffectActive(true);
			}
		}
	}

	IEnumerator CountdownCoroutine()
	{
		yield return new WaitForSeconds(gameRules.ABLYsuperlaserDelay);
		Fire();
	}

	void Fire()
	{
		// Make sure this shot counts for damage
		Status markStatus = new Status(gameObject, StatusType.SuperlaserMark);
		markStatus.SetTimeLeft(CalcDamage() * 2);
		//targetUnit.AddStatus(markStatus); // Apply mark
		//targetUnit.Damage(GetDamage(), 0, DamageType.Superlaser); // Deal damage to target

		Hitscan shot = new Hitscan(shotTemplate);
		shot.SetDamage(0);
		for (int i = 1; i < sourcePositions.Length; i++)
		{
			hitscans.SpawnHitscan(shot, sourcePositions[i].position, sourcePositions[0].forward, parentUnit, markStatus);
		}
		shot.SetDamage(CalcDamage());
		hitscans.SpawnHitscan(shot, sourcePositions[0].position, sourcePositions[0].forward, parentUnit, markStatus);

		//Instantiate(countdownFinishExplosion, targetUnit.transform.position, Quaternion.identity); // Explosion
		Instantiate(countdownFinishPrefab, transform.position, Quaternion.identity); // Sound TODO: ???

		Destroy(countdownStart); // TODO: ???
		Reset();
		StartCooldown(); // Now start ability cooldown
	}

	float CalcDamage()
	{
		return gameRules.ABLYsuperlaserDmgBase + gameRules.ABLYsuperlaserDmgByStacks[Mathf.Clamp(stacks, 0, gameRules.ABLYsuperlaserDmgByStacks.Length - 1)];
	}

	public override void End()
	{
		for (int i = 0; i < sourcePositions.Length; i++)
		{
			laserEffects[i].End();
			startEffects[i].End();
		}
	}

	new void Update()
	{
		base.Update();

		if (Time.frameCount > attemptShotWhen && state == 1) // In targeting state
		{
			if (targetUnit) // We have something to aim at
			{
				if (InRange(targetUnit.transform, gameRules.ABLYsuperlaserRangeUse)) // In range
				{
					if (Mathf.Abs(parentUnit.AimValue()) < aimThreshold) // Aimed close enough
					{
						// Start countdown once we are properly aimed
						BeginCountdown();
					}
				}
				else
				{
					Reset();
					StartCooldown(); // Now start cooldown
				} // in range
			}
			else // Target died
			{
				if (checkIfDead)
				{
					Reset();
				}
			} // target alive
		} // state
		
		// Effects
		if (state > 0)
		{
			for (int i = 0; i < sourcePositions.Length; i++)
			{
				laserEffects[i].SetEffectActive(1, sourcePositions[i].position, sourcePositions[i].position + sourcePositions[0].forward * gameRules.ABLYsuperlaserRangeUse);
			}

			if (state == 2)
			{
				countdownStart.transform.position = transform.position;

				for (int i = 0; i < sourcePositions.Length; i++)
				{
					startEffects[i].transform.position = sourcePositions[i].position;
					startEffects[i].transform.rotation = Quaternion.LookRotation(targetUnit.transform.position - sourcePositions[i].position);
				}
			}
		}
	}

	public void GiveStack()
	{
		stacks++;
		Display();
	}

	void Reset()
	{
		targetUnit = null;
		checkIfDead = false;

		parentUnit.ClearAbilityGoal();

		//StopCoroutine(countdownCoroutine); // Once countdown starts, it should not be stopped
		countdownCoroutine = null;

		Destroy(countdownStart); // TODO: ???
		ClearEffects();

		state = 0; // Back to standby state
	}

	void ClearEffects()
	{
		for (int i = 0; i < sourcePositions.Length; i++)
		{
			laserEffects[i].SetEffectActive(0);
			startEffects[i].SetEffectActive(false);
		}
	}

	bool InRange(Transform tran, float range)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < range * range)
			return true;
		else
			return false;
	}

	// Visualize range of turrets in editor
	void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(1.0f, 0.5f, 0.0f);
		Gizmos.DrawWireSphere(transform.position, 60); // We dont have a reference to gameRules at editor time
	}
}
