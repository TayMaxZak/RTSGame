using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_ShieldMode : Ability
{
	private ShieldMod shieldMod;
	private float activeRegenTimer = 0;

	[Header("Effects")]
	[SerializeField]
	private Transform shieldStart;
	[SerializeField]
	private Effect_Mesh meshEffectPrefab;
	private Effect_Mesh meshEffect;
	[SerializeField]
	private GameObject targetProjectEffectPrefab;
	[SerializeField]
	private GameObject returnEffectPrefab;
	[SerializeField]
	private GameObject breakEffectPrefab;

	[Header("Audio")]
	[SerializeField]
	private AudioEffect_Loop audioLoopPrefab;
	private AudioEffect_Loop audioLoop;

	private bool reEnabledLoop = true; // Used to optimize activation/deactivation of looping effect/audio in Update

	[SerializeField]
	private Ability_RailMode otherMode; // Only one mode can be active at once

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

		meshEffect = Instantiate(meshEffectPrefab, transform.position, Quaternion.identity);
		meshEffect.SetEffectActive(false);

		audioLoop = Instantiate(audioLoopPrefab, transform.position, Quaternion.identity);
		audioLoop.transform.parent = transform;
		audioLoop.SetEffectActive(false);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (suspended)
			return;

		if (!offCooldown)
			return;

		base.UseAbility(target);
		otherMode._StartCooldown();

		SetActive(!isActive);
	}

	public void SetActive(bool newActive)
	{
		isActive = newActive;
		if (isActive)
		{
			// Has to happen first so we don't remove the speed nerf
			if (otherMode.GetIsActive())
				otherMode.SetActive(false);

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

			meshEffect.SetEffectActive(true);
			audioLoop.SetEffectActive(true);

			UpdateActiveEffects();
		}
	}

	public void BreakShield()
	{
		// Indicate shield broke and return shield
		Instantiate(breakEffectPrefab, transform.position, transform.rotation);
		RemoveShield();
	}

	void ReturnShield()
	{
		// Indicate shield returned and return shield
		Instantiate(returnEffectPrefab, transform.position, transform.rotation);
		RemoveShield();
	}

	void RemoveShield()
	{
		parentUnit.RemoveShieldMod(shieldMod);

		meshEffect.SetEffectActive(false);
		audioLoop.SetEffectActive(false);
	}

	public override void End()
	{
		meshEffect.End();
		audioLoop.End();
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
					meshEffect.SetEffectActive(true);
					audioLoop.SetEffectActive(true);
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
		meshEffect.transform.rotation = transform.rotation;
		meshEffect.transform.position = transform.position;
	}

	public void OnDamage()
	{
		activeRegenTimer = gameRules.ABLY_shieldModeRegenDelay;
		//UpdateAbilityBar();

		if (shieldMod.shieldPercent <= 0.01f)
		{
			meshEffect.SetEffectActive(false);
			audioLoop.SetEffectActive(false);
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

	public override void SetEffectsVisible(bool visible)
	{
		meshEffect.SetVisible(visible);
		audioLoop.SetVisible(visible);
	}

	bool InRange(Transform tran, float range)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < range * range)
			return true;
		else
			return false;
	}

	public void _StartCooldown()
	{
		StartCooldown();
	}

	public bool GetIsActive()
	{
		return isActive;
	}
}
