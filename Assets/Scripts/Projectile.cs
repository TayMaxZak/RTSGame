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
	public ProjectileType type = ProjectileType.Bullet;
	public float speed = 0;
	public float damage = 0;

	private Vector3 startPosition = Vector3.zero;
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

	public float Range()
	{
		return (startPosition - position).magnitude;
	}
}
