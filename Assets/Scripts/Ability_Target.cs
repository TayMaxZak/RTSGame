using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Target
{
	[SerializeField]
	public Unit unit;
	[SerializeField]
	public Vector3 position;

	public Ability_Target(Unit u)
	{
		unit = u;
	}

	public Ability_Target(Vector3 pos)
	{
		position = pos;
	}

	public bool UnitOrPos()
	{
		if (unit)
			return true;
		else
			return false;
	}
}
