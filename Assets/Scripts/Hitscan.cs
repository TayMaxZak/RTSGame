﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Hitscan
{
	[SerializeField]
	private DamageType dmgType = DamageType.Normal;
	[SerializeField]
	private float range = 0;
	[SerializeField]
	private float damage = 0;
	[SerializeField]
	private float lifetime = 0;

	[HideInInspector]
	public Vector3 startPosition = Vector3.zero;
	[HideInInspector]
	public Vector3 direction = Vector3.forward;

	private Unit from;
	private Status status;

	public Hitscan(Hitscan copy)
	{
		dmgType = copy.dmgType;
		range = copy.range;
		damage = copy.damage;
		lifetime = copy.lifetime;
		startPosition = copy.startPosition;
		direction = copy.direction;
	}

	public DamageType GetDamageType()
	{
		return dmgType;
	}

	public float GetRange()
	{
		return range;
	}

	public float GetDamage()
	{
		return damage;
	}

	public void SetDamage(float dmg)
	{
		damage = dmg;
	}

	public float GetLifetime()
	{
		return lifetime;
	}

	public void SetLifetime(float time)
	{
		lifetime = time;
	}

	public Unit GetFrom()
	{
		return from;
	}

	public void SetFrom(Unit newFrom)
	{
		from = newFrom;
	}
	public Status GetStatus()
	{
		return status;
	}

	public void SetStatus(Status newStatus)
	{
		status = newStatus;
	}
}
