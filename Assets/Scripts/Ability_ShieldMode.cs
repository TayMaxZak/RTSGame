using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_ShieldMode : Ability
{
	private ShieldMod shieldMod;
	private float activeRegenTimer = 0;

	[SerializeField]
	private Transform shieldStart;
	[SerializeField]
	private Effect_Mesh targetLoopEffectPrefab;
	private Effect_Mesh targetLoopEffect;
	private bool reEnabledLoop = true;

	[SerializeField]
	private GameObject targetProjectEffectPrefab;
	[SerializeField]
	private GameObject returnEffectPrefab;
	[SerializeField]
	private GameObject breakEffectPrefab;

	//private UI_AbilBar_ShieldProject abilityBar;
	private bool isActive = false;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.ShieldMode;
		InitCooldown();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		shieldMod = new ShieldMod(parentUnit, 1, ShieldModType.ShieldMode);
		//abilityBar = parentUnit.hpBar.GetComponent<UI_AbilBar_ShieldProject>();
		//abilityBar.SetShield(shieldMod.shieldPercent, shieldMod.shieldPercent < 0);

		targetLoopEffect = Instantiate(targetLoopEffectPrefab, transform.position, Quaternion.identity);
		targetLoopEffect.SetEffectActive(false);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (suspended)
			return;

		if (!offCooldown)
			return;

		base.UseAbility(target);

		SetActive(!isActive);
	}

	public void SetActive(bool newActive)
	{
		isActive = newActive;
		if (isActive)
		{
			ProjectShield();
			parentUnit.AddStatus(new Status(gameObject, StatusType.ModeSpeedNerf));
		}
		else
		{
			ReturnShield();
			parentUnit.RemoveStatus(new Status(gameObject, StatusType.ModeSpeedNerf));
		}
	}

	void ProjectShield()
	{
		// If we successfully added a shield
		if (parentUnit.AddShieldMod(shieldMod))
		{
			// Update effects accordingly
			Instantiate(targetProjectEffectPrefab, shieldStart.position, transform.rotation);

			targetLoopEffect.SetEffectActive(true);

			UpdateActiveEffects();
		}
	}

	public void BreakShield()
	{
		// Indicate shield broke, return shield, and put on cooldown
		Instantiate(breakEffectPrefab, transform.position, transform.rotation);
		//StartCooldown();
		//RemoveShield();
	}

	void ReturnShield()
	{
		// Indicate shield broke, return shield, and put on cooldown
		Instantiate(returnEffectPrefab, transform.position, transform.rotation);
		StartCooldown();
		RemoveShield();
	}

	void RemoveShield()
	{
		parentUnit.RemoveShieldMod(shieldMod);
		targetLoopEffect.SetEffectActive(false);
	}

	public override void End()
	{
		targetLoopEffect.End();
	}

	new void Update()
	{
		base.Update();

		if (isActive)
		{
			activeRegenTimer -= Time.deltaTime;
			if (activeRegenTimer <= 0)
			{
				if (isActive && !reEnabledLoop)
				{
					targetLoopEffect.SetEffectActive(true);
					reEnabledLoop = true;
				}

				if (shieldMod.shieldPercent < 1)
				{
					// Regen even if inactive
					int regenIndex = Mathf.Clamp(Mathf.CeilToInt(5 * (1 - shieldMod.shieldPercent)) - 1, 0, 5);
					float increment = (gameRules.ABLY_shieldModeRegenGPS[regenIndex] / gameRules.ABLY_shieldModeMaxPool) * Time.deltaTime;
					//if (shieldMod.shieldPercent >= 0)
					shieldMod.shieldPercent = Mathf.Min(shieldMod.shieldPercent + increment, 1);
					//UpdateAbilityBar();
					if (isActive)
						parentUnit.OnShieldChange(); // Update parent unit's HP bar if shield is active
				}
			}
		}

		UpdateActiveEffects();
	}

	void UpdateActiveEffects()
	{
		//lineEffect.SetEffectActive(1, shieldStart.position, targetUnit.transform.position);
		targetLoopEffect.transform.rotation = transform.rotation;
		targetLoopEffect.transform.position = transform.position;
	}

	public void OnDamage()
	{
		activeRegenTimer = gameRules.ABLY_shieldModeRegenDelay;
		//UpdateAbilityBar();

		if (shieldMod.shieldPercent <= 0.01f)
		{
			targetLoopEffect.SetEffectActive(false);
			reEnabledLoop = false;
		}
	}

	protected override void UpdateAbilityBar()
	{
		//abilityBar.SetShield(Mathf.Clamp(shieldMod.shieldPercent, -1, 1), shieldMod.shieldPercent < 0);
	}



	public override void Suspend()
	{
		base.Suspend();

		// Force the shield to return
		SetActive(false);
	}

	bool InRange(Transform tran, float range)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < range * range)
			return true;
		else
			return false;
	}


}
