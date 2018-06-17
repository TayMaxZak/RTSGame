using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
	Default,
	ArmorDrain,
	ArmorRegen,
	SpawnSwarm,
	MoveSwarm,
	ShieldProject,
	HealField,
	Chain
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
				return GetDeltaOf(new Vector3(2, 10, 20));
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
				return "Disintegrate";
			case AbilityType.SpawnSwarm:
				return "Deploy Swarm";
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
	public static Sprite GetDisplayIconSecondary(AbilityType type, int stacks)
	{
		bool loadSprite = true;

		switch (type)
		{
			case AbilityType.HealField:
				{
					if (stacks == 0) // Not borrowing, don't show secondary icon
						loadSprite = false;
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
