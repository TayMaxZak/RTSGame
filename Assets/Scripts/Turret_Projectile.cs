using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret_Projectile : Turret
{
	[Header("Projectile Turret")]
	[SerializeField]
	private Projectile projTemplate;
	[SerializeField]
	private int pelletCount = 1;
	[SerializeField]
	private float accuracy = 0;

	private Manager_Projectiles projs;

	// Use this for initialization
	new void Awake()
	{
		base.Awake();
		projs = GameObject.FindGameObjectWithTag("ProjsManager").GetComponent<Manager_Projectiles>(); // Grab reference to Projectiles Manager);
	}

	protected override Vector3 FindAdjDifference()
	{
		// How far to aim ahead given how long it would take to reach current position
		// current target position + target velocity * time for projectile to reach current target position
		Vector3 offsetTarget = target.transform.position + target.GetVelocity() * ((target.transform.position - transform.position).magnitude / projTemplate.GetSpeed());

		// How far to aim ahead given how long it would take to reach predicted position
		// current target position + target velocity * time for projectile to reach predicted target position
		Vector3 offsetTargetAdj = target.transform.position + target.GetVelocity() * ((offsetTarget - transform.position).magnitude / projTemplate.GetSpeed());

		Vector3 difference = offsetTargetAdj - transform.position;

		// Visuals
		if (parentUnit.printInfo)
		{
			Debug.DrawLine(target.transform.position, transform.position, Color.red);
			Debug.DrawLine(offsetTarget, transform.position, Color.green);
			Debug.DrawLine(offsetTargetAdj, transform.position, Color.blue);
		}

		return difference;
	}

	protected override void Fire()
	{
		curAmmo--;

		Vector3 forward = GetForward();
		// Projectile
		for (int i = 0; i < pelletCount; i++)
		{
			Vector2 error = Random.insideUnitCircle * (accuracy / 10f);
			Vector3 errForward = (forward + (GetRight() * error.x) + (GetUp() * error.y)).normalized;

			if (onHitStatus == null)
				projs.SpawnProjectile(projTemplate, firePos.position, errForward, parentUnit, null); // TODO: Do we need to make a new status each time?
			else
				projs.SpawnProjectile(projTemplate, firePos.position, errForward, parentUnit, new Status(onHitStatus.from, onHitStatus.statusType)); // TODO: Do we need to make a new status each time?
		}

		// Sound
		PlayShootSound();
	}

}
