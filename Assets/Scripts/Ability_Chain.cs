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

	[Header("Audio")]
	[SerializeField]
	private AudioEffect_Loop audioLoopPrefab;
	private AudioEffect_Loop audioLoop;

	[SerializeField]
	private GameObject pointEffectBreakPrefab;
	private GameObject pointEffectBreak;

	private Unit targetUnit;
	private bool checkingForDead = false;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.Chain;
		InitCooldown();
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

		audioLoop = Instantiate(audioLoopPrefab, transform.position, Quaternion.identity);
		audioLoop.SetEffectActive(false);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (!offCooldown)
			return;

		base.UseAbility(target);

		ApplyChain(target.unit);
	}

	void ApplyChain(Unit unit)
	{
		if (unit != parentUnit)
		{
			if (InRange(unit.transform))
			{
				if (!targetUnit || unit != targetUnit)
				{
					if (targetUnit)
						ClearTarget(false);
					targetUnit = unit;
					checkingForDead = true;

					targetUnit.recievingAbilities.Add(this);

					chainEndsEffect[0].SetEffectActive(true);
					chainEndsEffect[1].SetEffectActive(true);
					audioLoop.SetEffectActive(true);
				}
				else
					ResetCooldown();
			} // InRange
			else
				ResetCooldown();
		} // not parentUnit
		else
		{
			if (targetUnit)
				ClearTarget(true);

			// If target is parentUnit, don't put ability on cooldown
			ResetCooldown();
		}
	}

	public override void End()
	{
		lineEffect.End();
		chainEndsEffect[0].End();
		chainEndsEffect[1].End();
		audioLoop.End();

		if (targetUnit)
			ClearTarget(true);
	}

	new void Update()
	{
		base.Update();

		if (targetUnit)
		{
			if (InRange(targetUnit.transform))
			{
				targetUnit.AddVelocityMod(new VelocityMod(parentUnit, parentUnit.GetVelocity(), VelocityModType.Chain));

				// Position and enable line effect
				lineEffect.SetEffectActive(1, chainStart.position, targetUnit.transform.position);

				// Particle effects placed at each end of the chain
				chainEndsEffect[0].transform.position = chainStart.position;
				chainEndsEffect[0].transform.rotation = Quaternion.LookRotation(targetUnit.transform.position - chainStart.position);
				chainEndsEffect[1].transform.position = targetUnit.transform.position;
				chainEndsEffect[1].transform.rotation = Quaternion.LookRotation(chainStart.position - targetUnit.transform.position);

				audioLoop.transform.position = (chainStart.position + targetUnit.transform.position) * 0.5f;
			}
			else // Chain breaks
			{
				Instantiate(pointEffectBreakPrefab, (chainStart.position + targetUnit.transform.position) * 0.5f, Quaternion.LookRotation(chainStart.position - targetUnit.transform.position));
				ClearTarget(true);
				StartCooldown(); // TODO: Maybe not
			}
		}
		else
		{
			if (checkingForDead)
			{
				ClearTarget(true);
				StartCooldown(); // TODO: Maybe not
			}
		}
	}

	void ClearTarget(bool clearEffects)
	{
		targetUnit.RemoveVelocityMod(new VelocityMod(parentUnit, parentUnit.GetVelocity(), VelocityModType.Chain));
		targetUnit.recievingAbilities.Remove(this);
		targetUnit = null;
		checkingForDead = false;
		if (clearEffects)
			ClearEffects();
	}

	public override void Suspend()
	{
		base.Suspend();

		if (targetUnit)
		{
			ClearTarget(true);
			StartCooldown();
		}
	}

	void ClearEffects()
	{
		lineEffect.SetEffectActive(0);
		chainEndsEffect[0].SetEffectActive(false);
		chainEndsEffect[1].SetEffectActive(false);
		audioLoop.SetEffectActive(false);
	}

	public override void SetEffectsVisible(bool visible)
	{
		lineEffect.SetVisible(visible);
		chainEndsEffect[0].SetVisible(visible);
		//chainEndsEffect[1].SetVisible(visible);
		audioLoop.SetVisible(visible);
	}

	public override void SetRecievingEffectsVisible(bool visible)
	{
		//lineEffect.SetVisible(visible);
		//chainEndsEffect[0].SetVisible(visible);
		chainEndsEffect[1].SetVisible(visible);
		//audioLoop.SetVisible(visible);
	}

	bool InRange(Transform tran)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < gameRules.ABLY_chainRange * gameRules.ABLY_chainRange)
			return true;
		else
			return false;
	}
}
