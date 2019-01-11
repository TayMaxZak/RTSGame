using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Superlaser : Ability
{
	[SerializeField]
	private Hitscan shotTemplate;
	[SerializeField]
	private Transform[] sourcePositions;

	[Header("Cannon")]
	[SerializeField]
	private GameObject cannon;
	private Quaternion rotation;
	[SerializeField]
	private float verticalRS = 5;
	[SerializeField]
	private float minV = 50;
	[SerializeField]
	private float maxV = 170;
	private float aimThreshold = 0.001f;
	private int attemptShotWhen;

	[Header("Effects")]
	[SerializeField]
	private Effect_Line laserEffectPrefab;
	private Effect_Line[] laserEffects;
	[SerializeField]
	private Effect_Point startEffectPrefab;
	private Effect_Point[] startEffects;
	[SerializeField]
	private GameObject pointEffectBreakPrefab;
	private GameObject pointEffectBreak;

	[Header("Sound")]
	[SerializeField]
	private Effect_Point countdownStartPrefab;
	private Effect_Point countdownStart;
	[SerializeField]
	private Effect_Point countdownStartRangePrefab;
	private Effect_Point countdownStartRange;
	[SerializeField]
	private GameObject countdownFinishPrefab;

	private int state = 0; // 0 = standby, 1 = targeting (has a target, is turning towards it), 2 = countdown (will shoot after a fixed time delay)
	private Coroutine countdownCoroutine;
	private float startTargetTime;

	private UI_AbilBar_Superlaser abilityBar;

	private Manager_Hitscan hitscans;

	private Unit targetUnit;
	private bool checkIfDead = false;

	private List<Unit> claimedBounties;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.Superlaser;
		InitCooldown();

		stacks = gameRules.ABLY_superlaserInitStacks;

		displayInfo.displayStacks = true;
		//displayInfo.displayFill = true;

		claimedBounties = new List<Unit>();

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
		displayInfo.stacks = Mathf.Min(stacks, gameRules.ABLY_superlaserDmgByStacks.Length - 1);
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
		if (suspended)
			return;

		if (!offCooldown)
			return;

		if (state == 0)
		{
			base.UseAbility(target);
			ResetCooldown(); // Cooldown is applied later

			startTargetTime = Time.time;
			BeginTargeting(target.unit);
		}
		else if (state == 1)
		{
			if (Time.time > startTargetTime + gameRules.ABLY_superlaserCancelTime)
			{
				base.UseAbility(target);
				Reset();
				SetCooldown(gameRules.ABLY_superlaserCancelCDMult); // Reduced cooldown
			}
		}
	}

	void BeginTargeting(Unit unit)
	{
		if (stacks <= 0) // At least one stack is required to activate this ability
			return;

		if (unit.team == team) // Cannot target allies
			return;

		if (InRange(unit.transform, gameRules.ABLY_superlaserRangeTargeting)) // In range
		{
			state = 1; // Targeting state

			targetUnit = unit; // Set new target
			checkIfDead = true; // Our target may become null

			parentUnit.SetAbilityGoal(new AbilityTarget(targetUnit)); // Turn towards it
			attemptShotWhen = Time.frameCount + 1; // We should start aim checking in 1 frame from now

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
		yield return new WaitForSeconds(gameRules.ABLY_superlaserDelay);
		Fire();
	}

	void Fire()
	{
		// Make sure this shot counts for getting a stack
		Status markStatus = new Status(gameObject, StatusType.SuperlaserMark);
		markStatus.SetTimeLeft(CalcDamage());

		Hitscan shot = new Hitscan(shotTemplate);
		shot.SetDamage(0);
		for (int i = 1; i < sourcePositions.Length; i++)
		{
			hitscans.SpawnHitscan(shot, sourcePositions[i].position, sourcePositions[0].forward, parentUnit, markStatus);
		}
		shot.SetDamage(CalcDamage());
		hitscans.SpawnHitscan(shot, sourcePositions[0].position, sourcePositions[0].forward, parentUnit, markStatus);

		Destroy(countdownStart); // TODO: ???
		//Instantiate(countdownFinishExplosion, targetUnit.transform.position, Quaternion.identity); // Explosion
		Instantiate(countdownFinishPrefab, transform.position, Quaternion.identity); // Sound TODO: ???
		
		Reset();
		StartCooldown(); // Now start ability cooldown
	}

	void SelfFire()
	{
		StopCoroutine(countdownCoroutine);

		parentUnit.DamageSimple(CalcDamage(), 0, true);

		Destroy(countdownStart); // TODO: ???
		//Instantiate(selfFireExplosion, targetUnit.transform.position, Quaternion.identity); // Explosion
		Instantiate(countdownFinishPrefab, transform.position, Quaternion.identity); // Sound TODO: ???

		Reset();
		StartCooldown(); // Now start ability cooldown
	}

	float CalcDamage()
	{
		return gameRules.ABLY_superlaserDmgBase + gameRules.ABLY_superlaserDmgByStacks[Mathf.Clamp(stacks, 0, gameRules.ABLY_superlaserDmgByStacks.Length - 1)];
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
				if (InRange(targetUnit.transform, gameRules.ABLY_superlaserRangeTargeting)) // In range
				{
					if (Vector3.Dot(cannon.transform.forward, (targetUnit.transform.position - cannon.transform.position).normalized) > 1 - aimThreshold) // Aimed close enough
					{
						// Start countdown once we are properly aimed
						BeginCountdown();
					}
				}
				else
				{
					Reset();
					SetCooldown(gameRules.ABLY_superlaserCancelCDMult);  // Reduced cooldown
				} // in range
			}
			else // Target died early
			{
				if (checkIfDead)
				{
					Reset();
					// No cooldown
				}
			} // target alive
		} // state

		if (state > 0 && targetUnit)
		{
			// Construct an imaginary target that only differs from the cannon's current orientation by the Y-axis
			float dist = new Vector2(cannon.transform.position.x - targetUnit.transform.position.x, cannon.transform.position.z - targetUnit.transform.position.z).magnitude;
			Vector3 pos = cannon.transform.position + transform.forward * dist;
			pos.y = targetUnit.transform.position.y;
			// Rotate superlaser vertically
			Debug.DrawLine(cannon.transform.position, pos, Color.blue);
			Rotate((pos - cannon.transform.position).normalized);
		}
		else
			Rotate(transform.forward);

		// Effects
		if (state > 0)
		{
			for (int i = 0; i < sourcePositions.Length; i++)
			{
				laserEffects[i].SetEffectActive(1, sourcePositions[i].position, sourcePositions[i].position + sourcePositions[0].forward * gameRules.ABLY_superlaserRangeTargeting);
			}

			if (state == 2)
			{
				countdownStart.transform.position = transform.position;

				for (int i = 0; i < sourcePositions.Length; i++)
				{
					startEffects[i].transform.position = sourcePositions[i].position;
					startEffects[i].transform.rotation = Quaternion.LookRotation(sourcePositions[0].forward);
				}
			}
		}
	}

	void Rotate(Vector3 direction)
	{
		// Fixes strange RotateTowards bug
		Quaternion resetRot = Quaternion.identity;

		// Rotate towards the desired look rotation
		// TODO: Sometimes super slow
		Debug.DrawRay(cannon.transform.position, direction, Color.red);
		Quaternion newRotation = Quaternion.RotateTowards(cannon.transform.rotation, Quaternion.LookRotation(direction, Vector3.up), Time.deltaTime * verticalRS);

		// Limit rotation
		newRotation = LimitVerticalRotation(newRotation, rotation);
		rotation = newRotation;

		// Fixes strange RotateTowards bug
		if (Time.frameCount == attemptShotWhen + 1)
			newRotation = resetRot;

		// Apply to cannon
		//cannon.transform.localRotation = Quaternion.Euler(new Vector3(rotation.eulerAngles.x, 0, 0));
		cannon.transform.rotation = rotation;
	}

	Quaternion LimitVerticalRotation(Quaternion rot, Quaternion oldRot)
	{
		Vector3 components = rot.eulerAngles;
		Vector3 componentsOld = oldRot.eulerAngles;
		Vector3 vComponentFwd = Quaternion.LookRotation(transform.up).eulerAngles;

		float angleV = Vector3.Angle(transform.up, rot * Vector3.forward);

		// Vertical extremes
		// Don't have to do anything besides ignoring bad rotations because units will never rotate on their Z or X axes
		if (angleV > maxV)
		{
			components.x = componentsOld.x;
		}
		else if (angleV < minV)
		{
			components.x = componentsOld.x;
		}

		return Quaternion.Euler(components);
	}

	public void GiveStack(Unit bounty)
	{
		if (!claimedBounties.Contains(bounty))
		{
			claimedBounties.Add(bounty);
			stacks++;
			Display();
		}
	}

	void Reset()
	{
		state = 0; // Back to standby state

		targetUnit = null;
		checkIfDead = false;

		parentUnit.ClearAbilityGoal();

		countdownCoroutine = null;

		Destroy(countdownStart); // TODO: ???
		ClearEffects();
	}

	public override void Suspend()
	{
		base.Suspend();

		if (state == 1)
		{
			Reset();
			SetCooldown(gameRules.ABLY_superlaserCancelCDMult); // Reduced cooldown
		}
		else if (state == 2)
		{
			SelfFire();
		}
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
}
