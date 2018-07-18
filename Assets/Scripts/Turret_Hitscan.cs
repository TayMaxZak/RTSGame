using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret_Hitscan : Turret
{
	[Header("Hitscan Turret")]
	[SerializeField]
	private Hitscan scanTemplate;
	[SerializeField]
	private int pelletCount = 1;
	[SerializeField]
	private float accuracy = 0;

	private Manager_Hitscan hitscans;

	// Use this for initialization
	new void Awake()
	{
		base.Awake();
		hitscans = GameObject.FindGameObjectWithTag("HitscanManager").GetComponent<Manager_Hitscan>(); // Grab reference to Hitscan Manager);
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
				hitscans.SpawnHitscan(scanTemplate, firePos.position, errForward, parentUnit, null); // TODO: Do we need to make a new status each time?
			else
				hitscans.SpawnHitscan(scanTemplate, firePos.position, errForward, parentUnit, new Status(onHitStatus.from, onHitStatus.statusType)); // TODO: Do we need to make a new status each time?
		}

		// Sound
		PlayShootSound();
	}

}
