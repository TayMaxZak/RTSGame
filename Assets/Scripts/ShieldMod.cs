using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ShieldModType
{
	ShieldProject,
	Flagship
}

[System.Serializable]
public class ShieldMod
{
	public Unit from;
	public float shieldPercent;
	public ShieldModType shieldModType;

	public ShieldMod(Unit u, float p, ShieldModType t)
	{
		from = u;
		shieldPercent = p;
		shieldModType = t;
	}
}