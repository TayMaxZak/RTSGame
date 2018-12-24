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
	protected void Awake()
	{
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;

		displayInfo = new AbilityDisplayInfo();
	}

	protected void Start()
	{
		parentUnit = GetComponent<Unit>();
		team = parentUnit.team;
		abilityIndex = parentUnit.abilities.IndexOf(this);
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
	protected void SetCooldown(float amount)
	{
		cooldownTimer = amount;
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

	// Used by subclasses
	protected virtual void UpdateAbilityBar()
	{

	}

	protected virtual void UpdateDisplay(int index, bool updateStacks, bool updateIconB)
	{
		parentUnit.UpdateAbilityDisplay(index, updateStacks, updateIconB);
	}

	protected virtual void UpdateDisplay(int index, bool updateStacks)
	{
		UpdateDisplay(index, updateStacks, false);
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
	public int stacks = 0;

	public bool displayFill = false;
	public float fill = 0;
	public float cooldown = 0;

	public bool displayIconB = false;
	public int iconBState = 0;
}

public enum AbilityType
{
	Default,
	ArmorDrain,
	ArmorRegen,
	SpawnSwarm,
	MoveSwarm,
	ShieldProject,
	HealField,
	Chain,
	Superlaser,
	StatusMissile,
	NoReload,
	SelfDestruct,
	IonMissile
}

public static class AbilityUtils
{
	// All default to 1 second
	/// <summary>
	/// X = Cooldown, Y = Active Duration, Z = Reset Duration
	/// </summary>
	public static Vector3 GetDeltaDurations(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return GetDeltaOf(new Vector3(2, 15, 30));
			case AbilityType.SpawnSwarm:
				return GetDeltaOf(new Vector3(10, 0, 0));
			case AbilityType.MoveSwarm:
				return GetDeltaOf(new Vector3(0.5f, 0, 0));
			case AbilityType.ShieldProject:
				return GetDeltaOf(new Vector3(10, 0, 0));
			case AbilityType.HealField:
				return GetDeltaOf(new Vector3(2, 0, 0));
			case AbilityType.Chain:
				return GetDeltaOf(new Vector3(20, 0, 0));
			case AbilityType.Superlaser:
				return GetDeltaOf(new Vector3(40, 0, 0));
			case AbilityType.StatusMissile:
				return GetDeltaOf(new Vector3(40, 0, 0));
			case AbilityType.NoReload:
				return GetDeltaOf(new Vector3(2, 10, 20));
			case AbilityType.SelfDestruct:
				return GetDeltaOf(new Vector3(2, 5, 20));
			case AbilityType.IonMissile:
				return GetDeltaOf(new Vector3(40, 0, 0));
			default:
				return GetDeltaOf(new Vector3());
		}
	}

	// Converts durations to multipliers per second
	static Vector3 GetDeltaOf(Vector3 vector)
	{
		// Avoid division by zero
		if (vector.x == 0)
			vector.x = 1;
		if (vector.y == 0)
			vector.y = 1;
		if (vector.z == 0)
			vector.z = 1;

		return new Vector3(1.0f / vector.x, 1.0f / vector.y, 1.0f / vector.z);
	}

	// 0 = no, 1 = unit, 2 = position
	public static int GetTargetRequirement(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.SpawnSwarm:
				return 1;
			case AbilityType.MoveSwarm:
				return 1;
			case AbilityType.ShieldProject:
				return 1;
			case AbilityType.Chain:
				return 1;
			case AbilityType.Superlaser:
				return 1;
			case AbilityType.StatusMissile:
				return 1;
			case AbilityType.IonMissile:
				return 1;
			default:
				return 0;
		}
	}

	// Display name of ability
	public static string GetDisplayName(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return "Armor Well";
			case AbilityType.SpawnSwarm:
				return "Launch Fighters";
			case AbilityType.MoveSwarm:
				return "Move Fighters";
			case AbilityType.ShieldProject:
				return "Project Shield";
			case AbilityType.HealField:
				return "Metasteel Pool";
			case AbilityType.Chain:
				return "Gravity Chain";
			case AbilityType.Superlaser:
				return "Hellrazor Superlaser";
			case AbilityType.StatusMissile:
				return "Disintegrate";
			case AbilityType.NoReload:
				return "Rapid Refire";
			case AbilityType.SelfDestruct:
				return "Overload Reactor";
			default:
				return "default";
		}
	}

	// Display name of ability
	public static string GetDisplayDesc(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return "Gradually drains armor from nearby enemy and allied units.";
			case AbilityType.SpawnSwarm:
				return "Deploys new fighters and moves them to the target. Limited uses.";
			case AbilityType.MoveSwarm:
				return "Fighters will follow the target, either protecting or attacking it.";
			case AbilityType.ShieldProject:
				return "Covers the target allied unit in a destructible shield. Cannot shield itself.";
			case AbilityType.HealField:
				return "Temporarily borrows resources to create a health repair field around this unit.";
			case AbilityType.Chain:
				return "Attaches to the target, pulling it wherever this unit goes.";
			case AbilityType.Superlaser:
				return "Collects charges for every kill. Firing requires at least 1 charge, dealing massive damage to the target.";
			case AbilityType.StatusMissile:
				return "Launches a chemical missile at the target which weakens armor and deals damage over time.";
			case AbilityType.NoReload:
				return "Allows all turrets to fire continuously by temporarily suspending their cooling cycles.";
			case AbilityType.SelfDestruct:
				return "Disables reactor cooling, causing a massive explosion after several seconds.";
			default:
				return "default";
		}
	}

	// Display icon of ability
	public static Sprite GetDisplayIcon(AbilityType type)
	{
		Sprite sprite = Resources.Load<Sprite>("IconAbility_" + type);
		if (sprite)
			return sprite;
		else
			return Resources.Load<Sprite>("IconEmpty");
	}

	// Secondary display icon of ability
	public static Sprite GetDisplayIconB(AbilityType type)
	{
		bool loadSprite = false;

		switch (type)
		{
			case AbilityType.HealField:
				{
					loadSprite = true;
				}
				break;
		}

		Sprite sprite = null;
		if (loadSprite)
			sprite = Resources.Load<Sprite>("IconAbility_" + type + "_B");

		if (sprite)
			return sprite;
		else
			return Resources.Load<Sprite>("IconEmpty");
	}
}