using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DamageType
{
	Normal,
	Wreck
}

[System.Serializable]
public class Projectile
{
	[SerializeField]
	private DamageType type = DamageType.Normal;
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

	private int team = 0;

	public Projectile(Projectile copy)
	{
		type = copy.type;
		speed = copy.speed;
		position = copy.position;
		direction = copy.direction;
		damage = copy.damage;
	}

	public void SetStartPosition(Vector3 startPos)
	{
		startPosition = startPos;
	}

	public float GetTeam()
	{
		return team;
	}

	public void SetTeam(int newTeam)
	{
		team = newTeam;
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
}
