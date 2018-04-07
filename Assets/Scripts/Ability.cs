using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
	ArmorDrain,
	Swarm
}

[System.Serializable]
public class Ability
{
	[SerializeField]
	private string displayName = "Default Name";
	private AbilityType type;

	public string DisplayName
	{
		get
		{
			return displayName;
		}
	}

	public void Use(Unit user, Ability_Target target)
	{
		if (AbilityUtils.InstantOrToggle(type))
			Debug.Log("Instant");
		else
			Debug.Log("Toggle");
	}

	public AbilityType GetAbilityType()
	{
		return type;
	}
}

public static class AbilityUtils
{
	public static bool InstantOrToggle(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return false;
			case AbilityType.Swarm:
				return true;
			default:
				return true;
		}
	}

	// 0 = no, 1 = unit, 2 = position
	public static int RequiresTarget(AbilityType type)
	{
		switch (type)
		{
			case AbilityType.ArmorDrain:
				return 0;
			case AbilityType.Swarm:
				return 1;
			default:
				return 0;
		}
	}
}
