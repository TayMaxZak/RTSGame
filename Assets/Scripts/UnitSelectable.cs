using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSelectable
{
	public Unit unit;
	public int groupId;

	public UnitSelectable(Unit u, int squad)
	{
		unit = u;
		groupId = squad;
	}
}
