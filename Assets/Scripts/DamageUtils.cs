﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageType
{
	Normal,
	Wreck,
	Swarm,
	Superlaser,
	Chemical,
	Internal,
	IonMissile,
	Bomb
}

public struct DamageResult
{
	public bool lastHit;

	public DamageResult(bool lastHit)
	{
		this.lastHit = lastHit;
	}
}

public static class DamageUtils
{
	public static bool IgnoresRangeResist(DamageType dmgType)
	{
		switch (dmgType)
		{
			case DamageType.Superlaser:
				return true;
			case DamageType.IonMissile:
				return true;
			case DamageType.Bomb:
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
			case DamageType.Bomb:
				return true;
			default:
				return false;
		}
	}

	public static bool CannotOverflowArmor(DamageType dmgType)
	{
		switch (dmgType)
		{
			case DamageType.Wreck:
				return true;
			case DamageType.Chemical:
				return true;
			case DamageType.Bomb:
				return true;
			default:
				return false;
		}
	}
}