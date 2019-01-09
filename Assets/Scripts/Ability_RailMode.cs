using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_RailMode : Ability
{
	//[SerializeField]
	//private Transform shieldStart;
	//[SerializeField]
	//private Effect_Mesh targetLoopEffectPrefab;
	//private Effect_Mesh targetLoopEffect;

	//[SerializeField]
	//private GameObject returnEffectPrefab;
	//[SerializeField]
	//private GameObject breakEffectPrefab;

	[SerializeField]
	private GameObject[] offTurrets;
	[SerializeField]
	private GameObject[] onTurrets;

	[SerializeField]
	private ParticleSystem ampPrefab;
	[SerializeField]
	private Transform ampPos;
	private Transform[] ampPositions;

	private List<ParticleSystem> ampPSystems;

	private bool ended = false;

	[SerializeField]
	private Ability_ShieldMode otherMode;

	//private UI_AbilBar_ShieldProject abilityBar;
	private bool isActive = false;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.RailMode;
		InitCooldown();

		ampPSystems = new List<ParticleSystem>();

		if (ampPos)
		{
			Transform[] ePos = ampPos.GetComponentsInChildren<Transform>();
			ampPositions = new Transform[ePos.Length - 1];
			for (int i = 1; i < ePos.Length; i++)
			{
				ampPositions[i - 1] = ePos[i];
			}
		}
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
		otherMode._StartCooldown();

		SetActive(!isActive);
	}

	// TODO: Sometimes turrets get swapped mid-reload, and never finish reloading!
	public void SetActive(bool newActive)
	{
		isActive = newActive;
		if (isActive)
		{
			// Has to happen first so we don't remove the speed nerf
			if (otherMode.GetIsActive())
				otherMode.SetActive(false);

			foreach (GameObject g in offTurrets)
			{
				g.SetActive(false);
			}
			foreach (GameObject g in onTurrets)
			{
				g.SetActive(true);
			}

			SetAmpActive(true);

			parentUnit.AddStatus(new Status(gameObject, StatusType.ModeSpeedNerf));
		}
		else
		{
			foreach (GameObject g in offTurrets)
			{
				g.SetActive(true);
			}
			foreach (GameObject g in onTurrets)
			{
				g.SetActive(false);
			}

			SetAmpActive(false);

			parentUnit.RemoveStatus(new Status(gameObject, StatusType.ModeSpeedNerf));
		}
	}

	public override void End()
	{
		ampPos.SetParent(null);
		SetAmpActive(false);
		ended = true;

		float duration = ampPrefab.main.duration;

		Destroy(ampPos.gameObject, duration);
		Destroy(gameObject, duration);
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
		//targetLoopEffect.transform.rotation = transform.rotation;
		//targetLoopEffect.transform.position = transform.position;
	}

	public void SetAmpActive(bool isActive)
	{
		if (ended)
			return;

		if (ampPSystems.Count == 0)
		{
			InitAmp();
		}

		foreach (ParticleSystem ampPS in ampPSystems)
		{
			if (isActive)
			{
				if (!ampPS.isPlaying)
					ampPS.Play();
			}
			else
			{
				if (ampPS.isPlaying)
					ampPS.Stop();
			}
		}
	}

	void InitAmp()
	{
		foreach (Transform pos in ampPositions)
		{
			GameObject go = Instantiate(ampPrefab.gameObject, pos.position, pos.rotation);
			go.transform.SetParent(pos);
			ampPSystems.Add(go.GetComponent<ParticleSystem>());
		}
	}

	protected override void UpdateAbilityBar()
	{
		//abilityBar.SetShield(Mathf.Clamp(shieldMod.shieldPercent, -1, 1), shieldMod.shieldPercent < 0);
	}

	public override void Suspend()
	{
		base.Suspend();

		// Disable high velocity turrets
		SetActive(false);
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
