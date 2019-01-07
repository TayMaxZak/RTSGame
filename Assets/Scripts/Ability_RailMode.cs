using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_RailMode : Ability
{
	[SerializeField]
	private Transform shieldStart;
	[SerializeField]
	private Effect_Mesh targetLoopEffectPrefab;
	private Effect_Mesh targetLoopEffect;

	[SerializeField]
	private GameObject returnEffectPrefab;
	[SerializeField]
	private GameObject breakEffectPrefab;

	//private UI_AbilBar_ShieldProject abilityBar;
	private bool isActive = false;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.RailMode;
		InitCooldown();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		//abilityBar = parentUnit.hpBar.GetComponent<UI_AbilBar_ShieldProject>();
		//abilityBar.SetShield(shieldMod.shieldPercent, shieldMod.shieldPercent < 0);
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
			parentUnit.AddStatus(new Status(gameObject, StatusType.ModeSpeedNerf));
		}
		else
		{
			parentUnit.RemoveStatus(new Status(gameObject, StatusType.ModeSpeedNerf));
		}
	}

	public override void End()
	{
		//targetLoopEffect.End();
	}

	new void Update()
	{
		base.Update();

		if (isActive)
		{

		}

		//UpdateActiveEffects();
	}

	void UpdateActiveEffects()
	{
		//lineEffect.SetEffectActive(1, shieldStart.position, targetUnit.transform.position);
		targetLoopEffect.transform.rotation = transform.rotation;
		targetLoopEffect.transform.position = transform.position;
	}

	protected override void UpdateAbilityBar()
	{
		//abilityBar.SetShield(Mathf.Clamp(shieldMod.shieldPercent, -1, 1), shieldMod.shieldPercent < 0);
	}

	public override void Suspend()
	{
		base.Suspend();

		// Force the shield to return
		//RemoveShield();
	}
}
