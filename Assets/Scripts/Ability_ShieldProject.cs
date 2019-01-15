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
	private Effect_Point pointEffectPrefab;
	private Effect_Point pointEffect;

	[Header("Audio")]
	[SerializeField]
	private AudioEffect_Loop audioLoopPrefab;
	private AudioEffect_Loop audioLoop;

	[SerializeField]
	private GameObject targetProjectEffectPrefab;
	[SerializeField]
	private GameObject returnEffectPrefab;
	[SerializeField]
	private GameObject breakEffectPrefab;

	private Unit targetUnit;
	private bool checkIfDead = false;

	private UI_AbilBar_ShieldProject abilityBar;

	new void Awake()
	{
		base.Awake();

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

		pointEffect = Instantiate(pointEffectPrefab, transform.position, Quaternion.identity);
		pointEffect.SetEffectActive(false);

		audioLoop = Instantiate(audioLoopPrefab, transform.position, Quaternion.identity);
		audioLoop.SetEffectActive(false);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (suspended)
			return;

		if (!offCooldown)
			return;

		base.UseAbility(target);

		ProjectShield(target.unit);
	}

	// TODO: CLEAN UP
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
				else if (target.team == team)
				{
					if (InRange(target.transform, gameRules.ABLY_shieldProjectRangeUse)) // Make sure target is in casting range
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

							targetUnit.recievingAbilities.Add(this);

							Instantiate(targetProjectEffectPrefab, targetUnit.transform.position, targetUnit.transform.rotation);

							pointEffect.SetEffectActive(true, false);
							audioLoop.SetEffectActive(true);

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
		pointEffect.End();
		audioLoop.End();
	}

	new void Update()
	{
		base.Update();

		if (targetUnit)
		{
			if (!targetUnit.IsDead() && InRange(targetUnit.transform, gameRules.ABLY_shieldProjectRange))
			{
				activeRegenTimer -= Time.deltaTime;
				if (activeRegenTimer <= 0)
				{
					if (shieldMod.shieldPercent < 1)
					{
						float increment = (gameRules.ABLY_shieldProjectOnGPS / gameRules.ABLY_shieldProjectMaxPool) * Time.deltaTime;
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
				pointEffect.SetEffectActive(false);
				audioLoop.SetEffectActive(false);
				checkIfDead = false;
			}

			// While inactive, constantly regenerate shieldPercent
			if (shieldMod.shieldPercent < 1)
			{
				float increment = (gameRules.ABLY_shieldProjectOffGPS / gameRules.ABLY_shieldProjectMaxPool) * Time.deltaTime;
				if (shieldMod.shieldPercent >= 0)
					shieldMod.shieldPercent = Mathf.Min(shieldMod.shieldPercent + increment, 1);
				else // Accelerated regeneration if in negative pool
					shieldMod.shieldPercent = Mathf.Min(shieldMod.shieldPercent + increment * gameRules.ABLY_shieldProjectOffGPSNegMult, 1);
				UpdateAbilityBar();
			}
		}
	}

	void UpdateActiveEffects()
	{
		//lineEffect.SetEffectActive(1, shieldStart.position, targetUnit.transform.position);
		lineEffect.SetEffectActive(1, transform.position, transform.position + (targetUnit.transform.position - shieldStart.position).normalized * 3);
		pointEffect.transform.rotation = targetUnit.transform.rotation;
		pointEffect.transform.position = targetUnit.transform.position;
		audioLoop.transform.position = targetUnit.transform.position;
	}

	// TODO: Limit just how negative the shield pool can go? Can get ridiculous and unintuitive if the negative pool exceeds what's displayed in the ability bar
	public void OnDamage()
	{
		shieldMod.shieldPercent = Mathf.Clamp(shieldMod.shieldPercent, -1, 1);
		if (targetUnit)
			activeRegenTimer = gameRules.ABLY_shieldProjectOnGPSDelay;
		UpdateAbilityBar();
	}

	protected override void UpdateAbilityBar()
	{
		abilityBar.SetShield(Mathf.Clamp(shieldMod.shieldPercent, -1, 1), shieldMod.shieldPercent < 0);
	}

	void ClearTarget()
	{
		targetUnit.RemoveShieldMod(shieldMod);
		targetUnit.recievingAbilities.Remove(this);
		targetUnit = null;

		lineEffect.SetEffectActive(0);
		pointEffect.SetEffectActive(false);
		audioLoop.SetEffectActive(false);
	}

	public override void Suspend()
	{
		base.Suspend();

		// Force the shield to return
		if (targetUnit)
		{
			Instantiate(returnEffectPrefab, shieldStart.position, shieldStart.rotation);

			ClearTarget();

			StartCooldown();
		}
	}

	public override void SetEffectsVisible(bool visible)
	{
		lineEffect.SetVisible(visible);
		//pointEffect.SetVisible(visible);
		//audioLoop.SetVisible(visible);
	}

	public override void SetRecievingEffectsVisible(bool visible)
	{
		//lineEffect.SetVisible(visible);
		pointEffect.SetVisible(visible);
		audioLoop.SetVisible(visible);
	}

	bool InRange(Transform tran, float range)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < range * range)
			return true;
		else
			return false;
	}


}
