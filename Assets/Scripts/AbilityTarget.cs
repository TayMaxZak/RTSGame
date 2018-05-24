using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityTarget
{
	[SerializeField]
	public Unit unit;
	[SerializeField]
	public Vector3 position;

	public AbilityTarget(Unit u)
	{
		unit = u;
	}

	public AbilityTarget(Vector3 pos)
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
