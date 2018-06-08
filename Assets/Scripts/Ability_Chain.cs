using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Chain : MonoBehaviour {
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

	//private Vector3 velocity; // Doesn't need to be public

	//private bool isActive = false;

	//private Manager_Game gameManager;
	private GameRules gameRules;

	private Unit parentUnit;
	private Unit targetUnit;

	// Use this for initialization
	void Start()
	{
		parentUnit = GetComponent<Unit>();
		//velocity = parentUnit.GetVelocity();

		//gameManager = ;
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;

		lineEffect = Instantiate(lineEffectPrefab, transform.position, Quaternion.identity);
		lineEffect.SetEffectActive(0);

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(false);
	}

	public void SetTarget(AbilityTarget target)
	{
		if (targetUnit)
			ClearTarget();
		if (target.unit != parentUnit && InRange(target.unit.transform))
			targetUnit = target.unit;
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
		else
		{
			lineEffect.SetEffectActive(0);
			pointEffect.SetEffectActive(false);
		}
	}

	void ClearTarget()
	{
		targetUnit.RemoveVelocityMod(new VelocityMod(parentUnit, parentUnit.GetVelocity(), VelocityModType.Chain));
		targetUnit = null;
		lineEffect.SetEffectActive(0);
		
	}

	bool InRange(Transform tran)
	{
		if (Vector3.SqrMagnitude(tran.position - chainStart.position) < gameRules.ABLYchainRange * gameRules.ABLYchainRange)
			return true;
		else
			return false;
	}

	public void End()
	{
		lineEffect.End();
		pointEffect.End();
	}
}
