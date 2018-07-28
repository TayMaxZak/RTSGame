using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageType
{
	Normal,
	Wreck,
	Swarm,
	Superlaser
}

public static class DamageUtils
{
	public static bool IgnoresRangeResist(DamageType dmgType)
	{
		switch (dmgType)
		{
			case DamageType.Superlaser:
				return true;
			default:
				return false;
		}
	}

	public static bool IgnoresFriendlyFire(DamageType dmgType)
	{
		switch (dmgType)
		{
			case DamageType.Superlaser:
				return true;
			default:
				return false;
		}
	}
}