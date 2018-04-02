using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProjectileType
{
	Bullet
}

[System.Serializable]
public class Projectile
{
	private ProjectileType type = ProjectileType.Bullet;
	[SerializeField]
	private float speed = 0;
	[SerializeField]
	private float damage = 0;

	private Vector3 startPosition = Vector3.zero;
	private float timeAlive = 0;


	public Vector3 position = Vector3.zero;
	public Vector3 direction = Vector3.forward;


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

	public float CalcRange()
	{
		return (startPosition - position).magnitude;
	}
}
