using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_MoveSwarm : Ability
{
	[SerializeField]
	private Ability_SpawnSwarm swarmController;

	private Unit targetUnit;

	new void Start()
	{
		base.Start();
		displayInfo.displayInactive = true;
	}

	void Awake()
	{
		abilityType = AbilityType.MoveSwarm;
		InitCooldown();
	}

	public void DisplayInactive(bool state)
	{
		if (displayInfo.displayInactive != state)
		{
			displayInfo.displayInactive = state;
			UpdateDisplay(abilityIndex, false);
		}
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (!offCooldown)
			return;

		base.UseAbility(target);

		if (swarmController.HasUsed())
		{
			if (target.unit != targetUnit && target.unit != swarmController.GetTargetUnit())
			{
				//targetUnit = target.unit; // swarmController will set this property for us
				swarmController.MoveSwarm(target.unit);
			}
			else // Unit is already targeted
				ResetCooldown();
		}
		else // No swarms to control
			ResetCooldown();
	}

	public void SetTargetUnit(Unit unit)
	{
		targetUnit = unit;
	}
}
