using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProjectileType
{
	Bullet
}

public class Projectile
{
	public ProjectileType type = ProjectileType.Bullet;
	public Vector3 position = Vector3.zero;
	public Vector3 velocity = Vector3.zero;
	public float damage = 0;
}
