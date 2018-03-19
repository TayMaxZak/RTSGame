using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager_Projectiles : MonoBehaviour
{
	[SerializeField]
	private int maxProjectiles = 99; // TODO: Change
	[SerializeField]
	public Projectile[] projectiles;

	void Start()
	{
		projectiles = new Projectile[maxProjectiles];
	}

	public void FixedUpdate()
	{


		for (int i = 0; i < projectiles.Length; i++)
		{
			Projectile proj = projectiles[i];
			//projectiles[i].position += projectiles[i].velocity;
		}
	}
}
