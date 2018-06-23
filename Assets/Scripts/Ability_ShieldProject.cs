using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_ShieldProject : Ability
{
	private ShieldMod shieldMod;
	private float activeRegenTimer = 0;

	[SerializeField]
	private Transform shieldStart;
	[SerializeField]
	private Effect_Line lineEffectPrefab;
	private Effect_Line lineEffect;
	[SerializeField]
	private Effect_Point targetLoopEffectPrefab;
	private Effect_Point targetLoopEffect;

	[SerializeField]
	private GameObject targetProjectEffectPrefab;
	[SerializeField]
	private GameObject returnEffectPrefab;
	[SerializeField]
	private GameObject breakEffectPrefab;

	private Unit targetUnit;
	private bool checkIfDead = false;
	private UI_AbilBar_ShieldProject abilityBar;

	void Awake()
	{
		abilityType = AbilityType.ShieldProject;
		InitCooldown();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		shieldMod = new ShieldMod(parentUnit, 1, ShieldModType.ShieldProject);

		abilityBar = parentUnit.hpBar.GetComponent<UI_AbilBar_ShieldProject>();
		abilityBar.SetShield(shieldMod.shieldPercent, shieldMod.shieldPercent < 0);

		lineEffect = Instantiate(lineEffectPrefab, transform.position, Quaternion.identity);
		lineEffect.SetEffectActive(0);

		targetLoopEffect = Instantiate(targetLoopEffectPrefab, transform.position, Quaternion.identity);
		targetLoopEffect.SetEffectActive(false);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (!offCooldown)
			return;

		base.UseAbility(target);

		ProjectShield(target.unit);
	}

	void ProjectShield(Unit target)
	{
		if (target != null) // Normal use case
		{
			if (target.GetType() != typeof(Unit_Flagship)) // Can't drain Flagships
			{
				// If we are targeting ourselves, clear everything
				if (target == parentUnit)
				{
					if (targetUnit)
					{
						Instantiate(returnEffectPrefab, shieldStart.position, shieldStart.rotation);

						ClearTarget();
					}
					else
						ResetCooldown();
				}
				else if (InRange(target.transform, gameRules.ABLYshieldProjectRangeUse)) // Make sure target is in casting range
				{
					// If we successfully added a shield,
					if (target.AddShieldMod(shieldMod))
					{
						// forget about previous targetUnit
						if (targetUnit)
							ClearTarget();

						// and set new targetUnit and update effects accordingly
						targetUnit = target;
						checkIfDead = true;

						Instantiate(targetProjectEffectPrefab, targetUnit.transform.position, targetUnit.transform.rotation);

						targetLoopEffect.SetEffectActive(true);

						UpdateActiveEffects();
					}
					else // Failed to cast, don't punish player with a cooldown
						ResetCooldown();
				}
				else // Failed to cast, don't punish player with a cooldown
					ResetCooldown();
			}
			else
				ResetCooldown();
		}
		else // Shield broken
		{
			if (targetUnit)
			{
				Instantiate(breakEffectPrefab, targetUnit.transform.position, targetUnit.transform.rotation);

				ClearTarget();
			}
		}
	}

	public void BreakShield()
	{
		// Indicate shield broke, return shield, and put on cooldown
		StartCooldown();
		ProjectShield(null);
	}

	public override void End()
	{
		if (targetUnit)
			ClearTarget();
		lineEffect.End();
		targetLoopEffect.End();
	}

	new void Update()
	{
		base.Update();

		if (targetUnit)
		{
			if (!targetUnit.IsDead() && InRange(targetUnit.transform, gameRules.ABLYshieldProjectRange))
			{
				activeRegenTimer -= Time.deltaTime;
				if (activeRegenTimer <= 0)
				{
					if (shieldMod.shieldPercent < 1)
					{
						float increment = (gameRules.ABLYshieldProjectOnGPS / gameRules.ABLYshieldProjectMaxPool) * Time.deltaTime;
						if (shieldMod.shieldPercent >= 0)
							shieldMod.shieldPercent = Mathf.Min(shieldMod.shieldPercent + increment, 1);
						UpdateAbilityBar();
						targetUnit.OnShieldChange(); // Update target unit's HP bar
					}
				}

				UpdateActiveEffects();
			}
			else // Left range
			{
				// Return shield and put on cooldown
				StartCooldown();
				ProjectShield(parentUnit);
			}
		}
		else
		{
			if (checkIfDead)
			{
				lineEffect.SetEffectActive(0);
				targetLoopEffect.SetEffectActive(false);
				checkIfDead = false;
			}

			// While inactive, constantly regenerate shieldPercent
			if (shieldMod.shieldPercent < 1)
			{
				float increment = (gameRules.ABLYshieldProjectOffGPS / gameRules.ABLYshieldProjectMaxPool) * Time.deltaTime;
				if (shieldMod.shieldPercent >= 0)
					shieldMod.shieldPercent = Mathf.Min(shieldMod.shieldPercent + increment, 1);
				else // Accelerated regeneration if in negative pool
					shieldMod.shieldPercent = Mathf.Min(shieldMod.shieldPercent + increment * gameRules.ABLYshieldProjectOffGPSNegMult, 1);
				UpdateAbilityBar();
			}
		}
	}

	void UpdateActiveEffects()
	{
		//lineEffect.SetEffectActive(1, shieldStart.position, targetUnit.transform.position);
		lineEffect.SetEffectActive(1, transform.position, transform.position + (targetUnit.transform.position - shieldStart.position).normalized * 3);
		targetLoopEffect.transform.rotation = targetUnit.transform.rotation;
		targetLoopEffect.transform.position = targetUnit.transform.position;
	}

	public void OnDamage()
	{
		activeRegenTimer = gameRules.ABLYshieldProjectOnGPSDelay;
		UpdateAbilityBar();
	}

	protected override void UpdateAbilityBar()
	{
		abilityBar.SetShield(Mathf.Clamp(shieldMod.shieldPercent, -1, 1), shieldMod.shieldPercent < 0);
	}

	void ClearTarget()
	{
		targetUnit.RemoveShieldMod(shieldMod);
		targetUnit = null;

		lineEffect.SetEffectActive(0);
		targetLoopEffect.SetEffectActive(false);
	}

	bool InRange(Transform tran, float range)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < range * range)
			return true;
		else
			return false;
	}


}
