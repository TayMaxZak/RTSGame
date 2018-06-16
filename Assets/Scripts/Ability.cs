using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class Ability : MonoBehaviour {
	protected AbilityType abilityType; // Used to determine how to interact with this Ability
	protected int team; // Doesn't need to be public

	// Common primtive data
	protected int abilityIndex = -1;
	protected float cooldownTimer = 0; // Tracks when ability can be used again next
	protected int stacks = 0; // Counter used for tracking ability level, uses left, etc.

	// Common display data
	protected AbilityDisplayInfo displayInfo;

	// Common object data
	protected AbilityTarget target;
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
	
	public virtual void UseAbility(AbilityTarget targ)
	{
		target = targ;
	}

	public virtual void End()
	{
	}

	public virtual void UpdateAbilityBar()
	{

	}

	protected virtual void UpdateDisplay(int index, bool updateStacks)
	{
		parentUnit.UpdateAbilityDisplay(index, updateStacks);
	}

	public AbilityTarget GetTarget()
	{
		return target;
	}

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
	public int stacks = 0;
	public float fill = 0;
}
