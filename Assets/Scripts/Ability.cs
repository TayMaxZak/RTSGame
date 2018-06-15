using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class Ability : MonoBehaviour {
	protected AbilityType abilityType; // Used to determine how to interact with this Ability
	protected int team; // Doesn't need to be public

	// Common primtive data
	protected float cooldownTimer = 0; // Tracks when ability can be used again next
	protected int stacks = 0; // Counter used for tracking ability level, uses left, etc.

	// Common display data
	protected AbilityDisplay display;

	// Common object data
	protected AbilityTarget target;
	protected GameRules gameRules;
	protected Unit parentUnit;

	// Use this for initialization
	protected void Start ()
	{
		parentUnit = GetComponent<Unit>();
		team = parentUnit.team;

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
	}
	
	public virtual void UseAbility(AbilityTarget targ)
	{
		target = targ;
	}

	public virtual void End()
	{
	}

	public virtual void UpdateVisuals()
	{

	}

	public AbilityTarget GetTarget()
	{
		return target;
	}

	public AbilityType GetAbilityType()
	{
		return abilityType;
	}


}

public class AbilityDisplay
{
	public bool displayInactive = false;
	public bool displayStacks = false;
	public float displayFill = 0;
}
