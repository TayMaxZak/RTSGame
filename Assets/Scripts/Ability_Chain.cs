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
	private Effect_Point chainEndsEffectPrefab;
	private Effect_Point[] chainEndsEffect;
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

		chainEndsEffect = new Effect_Point[2];
		chainEndsEffect[0] = Instantiate(chainEndsEffectPrefab, transform.position, Quaternion.identity);
		chainEndsEffect[1] = Instantiate(chainEndsEffectPrefab, transform.position, Quaternion.identity);
		chainEndsEffect[0].SetEffectActive(false);
		chainEndsEffect[1].SetEffectActive(false);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (targetUnit)
			ClearTarget();
		if (target.unit != parentUnit && InRange(target.unit.transform))
		{
			targetUnit = target.unit;

			chainEndsEffect[0].SetEffectActive(true);
			chainEndsEffect[1].SetEffectActive(true);
		}
	}

	public override void End()
	{
		lineEffect.End();
		chainEndsEffect[0].End();
		chainEndsEffect[1].End();
	}

	void Update()
	{
		if (targetUnit)
		{
			if (InRange(targetUnit.transform))
			{
				targetUnit.AddVelocityMod(new VelocityMod(parentUnit, parentUnit.GetVelocity(), VelocityModType.Chain));

				lineEffect.SetEffectActive(1, chainStart.position, targetUnit.transform.position);

				chainEndsEffect[0].transform.position = chainStart.position;
				chainEndsEffect[0].transform.rotation = Quaternion.LookRotation(targetUnit.transform.position - chainStart.position);
				chainEndsEffect[1].transform.position = targetUnit.transform.position;
				chainEndsEffect[1].transform.rotation = Quaternion.LookRotation(chainStart.position - targetUnit.transform.position);
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
		chainEndsEffect[0].SetEffectActive(false);
		chainEndsEffect[1].SetEffectActive(false);
	}

	bool InRange(Transform tran)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < gameRules.ABLYchainRange * gameRules.ABLYchainRange)
			return true;
		else
			return false;
	}


}
