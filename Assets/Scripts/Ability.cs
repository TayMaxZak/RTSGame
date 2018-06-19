using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class Ability : MonoBehaviour {
	protected AbilityType abilityType; // Used to determine how to interact with this Ability
	protected int team; // Doesn't need to be public

	// Common primtive data
	protected int abilityIndex = -1;
	protected float cooldownDelta = 1;
	protected float cooldownTimer = 0; // Tracks when ability can be used again next
	protected bool offCooldown = true;
	protected int stacks = 0; // Counter used for tracking ability level, uses left, etc.

	// Common display data
	protected AbilityDisplayInfo displayInfo;

	// Common object data
	//protected AbilityTarget target;
	protected GameRules gameRules;
	protected Unit parentUnit;

	// Use this for initialization
	protected void Start ()
	{
		parentUnit = GetComponent<Unit>();
		team = parentUnit.team;
		abilityIndex = parentUnit.abilities.IndexOf(this);

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;

		displayInfo = new AbilityDisplayInfo();
	}

	protected void Update()
	{
		if (!offCooldown)
		{
			if (!gameRules.useTestValues)
				cooldownTimer -= cooldownDelta * Time.deltaTime;
			else
				cooldownTimer -= cooldownDelta * (1f / gameRules.TESTtimeMult) * Time.deltaTime;

			if (cooldownTimer <= 0)
				offCooldown = true;
			Display();
		}
	}

	void Display()
	{
		if (displayInfo.displayFill)
			return;

		displayInfo.cooldown = cooldownTimer;
		UpdateDisplay(abilityIndex, false);
	}
	
	public virtual void UseAbility(AbilityTarget targ)
	{
		//target = targ;
		StartCooldown();
	}

	// For abilities that want customized ability cast logic
	protected void StartCooldown()
	{
		cooldownTimer = 1;
		Display();
		offCooldown = false;
	}

	// For abilities that want customized ability cast logic
	protected void ResetCooldown()
	{
		cooldownTimer = 0;
		Display();
		offCooldown = true;
	}

	public virtual void End()
	{
	}

	protected virtual void UpdateAbilityBar()
	{

	}

	protected virtual void UpdateDisplay(int index, bool updateStacks)
	{
		parentUnit.UpdateAbilityDisplay(index, updateStacks);
	}

	protected virtual void InitCooldown()
	{
		cooldownDelta = AbilityUtils.GetDeltaDurations(abilityType).x;
	}

	/*public AbilityTarget GetTarget()
	{
		return target;
	}*/

	public AbilityType GetAbilityType()
	{
		return abilityType;
	}

	public AbilityDisplayInfo GetDisplayInfo()
	{
		return displayInfo;
	}
}

public class AbilityDisplayInfo
{
	public bool displayInactive = false;
	public bool displayStacks = false;
	public float cooldown = 0;
	public bool displayFill = false;
	public int stacks = 0;
	public float fill = 0;
}
