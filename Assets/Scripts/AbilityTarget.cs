using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AbilityTarget
{
	public Unit unit;
	public Vector3 position;

	public AbilityTarget(Unit u)
	{
		unit = u;
	}

	public AbilityTarget(Vector3 pos)
	{
		position = pos;
	}

	public override string ToString()
	{
		return EntityUtils.GetDisplayName(unit.Type) + " " + position;
	}

	//public bool UnitOrPos()
	//{
	//	if (unit)
	//		return true;
	//	else
	//		return false;
	//}
}
