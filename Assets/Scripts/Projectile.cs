using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Projectile
{
	[SerializeField]
	private DamageType dmgType = DamageType.Normal;
	[SerializeField]
	private float speed = 0;
	[SerializeField]
	private float damage = 0;

	private Vector3 startPosition = Vector3.zero;
	private float timeAlive = 0;

	[HideInInspector]
	public Vector3 position = Vector3.zero;
	[HideInInspector]
	public Vector3 direction = Vector3.forward;

	private Unit from;
	private Status status;

	public Projectile(Projectile copy)
	{
		dmgType = copy.dmgType;
		speed = copy.speed;
		position = copy.position;
		direction = copy.direction;
		damage = copy.damage;
	}

	public void SetStartPosition(Vector3 startPos)
	{
		startPosition = startPos;
	}

	public DamageType GetDamageType()
	{
		return dmgType;
	}

	public float GetSpeed()
	{
		return speed;
	}

	public float GetDamage()
	{
		return damage;
	}

	public float GetTimeAlive()
	{
		return timeAlive;
	}

	public void UpdateTimeAlive(float deltaTime)
	{
		timeAlive += deltaTime;
	}

	public void SetTimeAlive(float time)
	{
		timeAlive = time;
	}

	public float CalcRange()
	{
		return (startPosition - position).magnitude;
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
