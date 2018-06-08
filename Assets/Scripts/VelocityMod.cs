using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum VelocityModType
{
	Chain
}

[System.Serializable]
public class VelocityMod
{
	public Unit from;
	public Vector3 vel;
	public VelocityModType velModType;

	public VelocityMod(Unit u, Vector3 v, VelocityModType t)
	{
		from = u;
		vel = v;
		velModType = t;
	}
}