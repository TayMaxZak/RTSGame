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

			Status stat = onHitStatus == null ? null : new Status(onHitStatus.from, onHitStatus.statusType);

			// Either the target requires a raycast to hit, or it became null during a coroutine delay. In this case, we want to shoot anyway
			if (IsNull(target) || target.HasCollision())
				hitscans.SpawnHitscan(scanTemplate, firePos.position, errForward, parentUnit, stat); // TODO: Do we need to make a new status each time?
			else // Explicitly define visuals
			{
				float distance = (firePos.position - target.GetPosition()).magnitude;
				// TODO: Should raycast anyway to check for cover. Currently, we will NOT shoot through allies bc we check for FF, but we CAN ignore enemies and terrain, shooting fighters through them
				hitscans.SpawnHitscan(scanTemplate, firePos.position, errForward, parentUnit, stat, target); // TODO: Do we need to make a new status each time?
			}
		}

		// Sound
		PlayShootSound();
	}

}
