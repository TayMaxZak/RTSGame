using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Superlaser : Ability
{
	[SerializeField]
	private Transform[] laserPositions;
	[SerializeField]
	private Effect_Line laserEffectPrefab;
	private Effect_Line[] laserEffects;
	[SerializeField]
	private Effect_Point laserStartPrefab;
	private Effect_Point[] laserStartEffects;
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
	private bool checkingForDead = false;
	private float initDistance = 0;
	private Vector3 initPosition;

	private Coroutine countdownCoroutine;

	void Awake()
	{
		abilityType = AbilityType.Superlaser;
		InitCooldown();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();
		stacks = gameRules.ABLYsuperlaserInitStacks;
		displayInfo.displayStacks = true;
		Display();

		laserEffects = new Effect_Line[laserPositions.Length];
		laserStartEffects = new Effect_Point[laserPositions.Length];
		for (int i = 0; i< laserPositions.Length; i++)
		{
			laserEffects[i] = Instantiate(laserEffectPrefab, laserPositions[i].position, Quaternion.identity);
			laserEffects[i].SetEffectActive(0);
			laserStartEffects[i] = Instantiate(laserStartPrefab, laserPositions[i].position, Quaternion.identity);
			laserStartEffects[i].SetEffectActive(false);
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
		if (countdownCoroutine == null)
		{
			if (stacks > 0) // At least one stack is required to activate this ability
			{
				//if (unit.team != team)
				if (true) // Cannot target allies
				{
					if (InRange(unit.transform, gameRules.ABLYsuperlaserRangeUse))
					{
						if (!targetUnit || unit != targetUnit)
						{
							if (targetUnit)
								ClearTarget(false);
							targetUnit = unit;
							checkingForDead = true;
							initDistance = Vector3.Magnitude(targetUnit.transform.position - transform.position);
							initPosition = transform.position;

							countdownCoroutine = StartCoroutine(CountdownCoroutine());

							for (int i = 0; i < laserPositions.Length; i++)
							{
								laserStartEffects[i].SetEffectActive(true);
							}
						}
					} // InRange
				} // notally
			} // stacks
		} // coroutine not currently going
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
		markStatus.SetTimeLeft(GetDamage());
		targetUnit.AddStatus(markStatus);

		targetUnit.Damage(GetDamage(), 0, DamageType.Superlaser);

		Instantiate(countdownFinishExplosion, targetUnit.transform.position, Quaternion.identity);
		Instantiate(countdownFinishPrefab, transform.position, Quaternion.identity);

		Destroy(countdownStart);
		ClearTarget(true);
		StartCooldown(); // Now start cooldown
	}

	float GetDamage()
	{
		return gameRules.ABLYsuperlaserDmgByStacks[Mathf.Clamp(stacks, 0, gameRules.ABLYsuperlaserDmgByStacks.Length - 1)];
	}

	public override void End()
	{
		for (int i = 0; i < laserPositions.Length; i++)
		{
			laserEffects[i].End();
			laserStartEffects[i].End();
		}
	}

	new void Update()
	{
		base.Update();

		if (targetUnit)
		{
			if (InRangeTolerance(targetUnit.transform, gameRules.ABLYsuperlaserRangeUse))
			{
				if (!countdownStart)
				{
					countdownStart = Instantiate(countdownStartPrefab, transform.position, transform.rotation);
					//countdownStartRange = Instantiate(countdownStartRangePrefab, transform.position, transform.rotation);
				}
				else
					countdownStart.transform.position = transform.position;

				for (int i = 0; i < laserPositions.Length; i++)
				{
					laserEffects[i].SetEffectActive(1, laserPositions[i].position, targetUnit.transform.position);
					laserStartEffects[i].transform.position = laserPositions[i].position;
					laserStartEffects[i].transform.rotation = Quaternion.LookRotation(targetUnit.transform.position - laserPositions[i].position);
				}
			}
			else
			{
				Instantiate(pointEffectBreakPrefab, (laserPositions[1].position + targetUnit.transform.position) * 0.5f, Quaternion.LookRotation(laserPositions[1].position - targetUnit.transform.position));

				//countdownStart.SetEffectActive(false); TODO: BUGGED
				ClearTarget(true);
				StartCooldown(); // Now start cooldown
			}
		}
		else
		{
			if (checkingForDead)
			{
				//countdownStart.SetEffectActive(false); TODO: BUGGED
				ClearTarget(true);
			}
		}
	}

	public void GiveStack()
	{
		stacks++;
		Display();
	}

	void ClearTarget(bool clearEffects)
	{
		targetUnit.RemoveVelocityMod(new VelocityMod(parentUnit, parentUnit.GetVelocity(), VelocityModType.Chain));
		targetUnit = null;
		checkingForDead = false;

		StopCoroutine(countdownCoroutine);
		countdownCoroutine = null;

		if (clearEffects)
		{
			Destroy(countdownStart);
			//Destroy(countdownStartRange);
			ClearEffects();
		}
	}

	void ClearEffects()
	{
		for (int i = 0; i < laserPositions.Length; i++)
		{
			laserEffects[i].SetEffectActive(0);
			laserStartEffects[i].SetEffectActive(false);
		}
	}

	bool InRange(Transform tran, float range)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < range * range)
			return true;
		else
			return false;
	}

	bool InRangeTolerance(Transform tran, float range)
	{
		float ourDistanceSqr = Vector3.SqrMagnitude(tran.position - initPosition);
		
		if (ourDistanceSqr <= (initDistance - gameRules.ABLYsuperlaserRangeTolerance) * (initDistance - gameRules.ABLYsuperlaserRangeTolerance)
			|| ourDistanceSqr >= (initDistance + gameRules.ABLYsuperlaserRangeTolerance) * (initDistance + gameRules.ABLYsuperlaserRangeTolerance))
			return false;
		else
			return true;
	}
}
