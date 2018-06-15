using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Chain : Ability
{
	[SerializeField]
	private Transform chainStart;
	[SerializeField]
	private Effect_Line lineEffectPrefab;
	private Effect_Line lineEffect;
	[SerializeField]
	private Effect_Point pointEffectPrefab;
	private Effect_Point pointEffect;
	[SerializeField]
	private GameObject pointEffectBreakPrefab;
	private GameObject pointEffectBreak;

	private Unit targetUnit;

	void Awake()
	{
		abilityType = AbilityType.Chain;
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		lineEffect = Instantiate(lineEffectPrefab, transform.position, Quaternion.identity);
		lineEffect.SetEffectActive(0);

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(false);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (targetUnit)
			ClearTarget();
		if (target.unit != parentUnit && InRange(target.unit.transform))
			targetUnit = target.unit;
	}

	public override void End()
	{
		lineEffect.End();
		pointEffect.End();
	}

	void Update()
	{
		if (targetUnit)
		{
			if (InRange(targetUnit.transform))
			{
				targetUnit.AddVelocityMod(new VelocityMod(parentUnit, parentUnit.GetVelocity(), VelocityModType.Chain));

				lineEffect.SetEffectActive(1, chainStart.position, targetUnit.transform.position);
				pointEffect.SetEffectActive(true);
				pointEffect.transform.position = (chainStart.position + targetUnit.transform.position) * 0.5f;
			}
			else
			{
				Instantiate(pointEffectBreakPrefab, (chainStart.position + targetUnit.transform.position) * 0.5f, Quaternion.LookRotation(chainStart.position - targetUnit.transform.position));
				ClearTarget();
			}
		}
	}

	void ClearTarget()
	{
		targetUnit.RemoveVelocityMod(new VelocityMod(parentUnit, parentUnit.GetVelocity(), VelocityModType.Chain));
		targetUnit = null;
		lineEffect.SetEffectActive(0);
		pointEffect.SetEffectActive(false);
	}

	bool InRange(Transform tran)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < gameRules.ABLYchainRange * gameRules.ABLYchainRange)
			return true;
		else
			return false;
	}


}
