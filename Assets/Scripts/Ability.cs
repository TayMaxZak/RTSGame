using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
	EMP,
	ArmorDrain,
	Swarm
}

public class Ability
{
	[SerializeField]
	private string displayName = "Default Name";

	public string DisplayName
	{
		get
		{
			return displayName;
		}
	}

	void OnUse(Unit user, Unit receiver)
	{

	}
}
