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
	private bool checkIfDead = false;
	private float initDistance = 0;
	private Vector3 initPosition;

	private Coroutine countdownCoroutine;

	private UI_AbilBar_Superlaser abilityBar;

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
		abilityBar = parentUnit.hpBar.GetComponent<UI_AbilBar_Superlaser>();
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
		UpdateAbilityBar();
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
				if (unit.team != team) // Cannot target allies
				//if (true) // Cannot target allies
				{
					if (InRange(unit.transform, gameRules.ABLYsuperlaserRangeUse))
					{
						if (!targetUnit || unit != targetUnit)
						{
							if (targetUnit)
								ClearTarget(false);
							targetUnit = unit;
							checkIfDead = true;
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
		markStatus.SetTimeLeft(GetDamage() * 2);
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
			if (InRangeTolerance(targetUnit.transform))
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
				//Instantiate(pointEffectBreakPrefab, (laserPositions[1].position + targetUnit.transform.position) * 0.5f, Quaternion.LookRotation(laserPositions[1].position - targetUnit.transform.position));

				//countdownStart.SetEffectActive(false); // TODO: BUGGED
				ClearTarget(true);
				StartCooldown(); // Now start cooldown
			}
		}
		else
		{
			if (checkIfDead)
			{
				//countdownStart.SetEffectActive(false); // TODO: BUGGED
				ClearTarget(true);
			}
		}
	}

	public void GiveStack()
	{
		stacks++;
		Display();
	}

	protected override void UpdateAbilityBar()
	{
		abilityBar.SetStacks(stacks, false);
	}

	void ClearTarget(bool clearEffects)
	{
		targetUnit.RemoveVelocityMod(new VelocityMod(parentUnit, parentUnit.GetVelocity(), VelocityModType.Chain));
		targetUnit = null;
		checkIfDead = false;

		StopCoroutine(countdownCoroutine);
		countdownCoroutine = null;

		if (clearEffects)
		{
			Destroy(countdownStart);
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

	bool InRangeTolerance(Transform tran)
	{
		float ourDistanceSqr = Vector3.SqrMagnitude(tran.position - initPosition);
		
		if (ourDistanceSqr <= Mathf.Max(0, initDistance - gameRules.ABLYsuperlaserRangeTolerance) * Mathf.Max(0, initDistance - gameRules.ABLYsuperlaserRangeTolerance)
			|| ourDistanceSqr >= (initDistance + gameRules.ABLYsuperlaserRangeTolerance) * (initDistance + gameRules.ABLYsuperlaserRangeTolerance))
			return false;
		else
			return true;
	}
}
