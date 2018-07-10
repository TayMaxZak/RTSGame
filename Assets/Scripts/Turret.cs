using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
	public int team = 0;
	private Unit parentUnit;
	private int state = 0; // 0 = standby, 1 = shooting

	[SerializeField]
	private Projectile projTemplate;
	private Status onHitStatus;

	[Header("Targeting")]
	[SerializeField]
	private Unit target;
	[SerializeField]
	private float range = 25;

	[Header("Sound")]
	[SerializeField]
	private AudioClip soundShoot; // Played on each shot or, if there is an audio loop, when the loop ends

	[Header("Shooting")]
	[SerializeField]
	private Transform firePos;
	private int curAmmo = 4;
	[SerializeField]
	private int maxAmmo = 4;
	[SerializeField]
	private int pelletCount = 1;
	[SerializeField]
	private float accuracy = 1;
	private float allowShootThressh = 0.0001f; // 0.001f

	[SerializeField]
	private float reloadCooldown = 4;
	// If we manually clear target, this should immediately trigger an early reload
	// If the target goes out of range, this should immediately trigger a early reload
	// This coroutine references the specific reload we want to cancel
	private Coroutine reloadCoroutine;
	private bool isReloadCancellable;

	[SerializeField]
	[Tooltip("Rate of fire measured in rounds per minute")]
	private float rateOfFire = 120; // In RPM
	private float shootCooldown;
	[SerializeField]
	private float shootOffsetRatio = 0.0f;
	private float shootOffset;

	[Header("Rotating")]
	[SerializeField]
	private float RS = 90;
	private Quaternion rotation;
	[SerializeField]
	private bool baseRotatesOnY = true;
	[SerializeField]
	private Transform pivotX;
	[SerializeField]
	private Transform pivotY;


	[Header("Rotation Limits")]
	[SerializeField]
	private float maxH = 180;
	[SerializeField]
	private float minH = 180;
	[SerializeField]
	private float minV = 0;
	[SerializeField]
	private float maxV = 180;

	private bool targetInRange;
	private bool isReloading;
	private bool isShooting;

	private Quaternion lookRotation;
	//private Quaternion forwardRotation;
	private Vector3 direction;

	private int resetRotFrame;

	private GameRules gameRules;
	private Manager_Projectiles projs;
	private AudioSource audioSource;

	// Use this for initialization
	void Start()
	{
		curAmmo = maxAmmo;

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules
		projs = GameObject.FindGameObjectWithTag("ProjsManager").GetComponent<Manager_Projectiles>(); // Grab copy of Projectiles Manager
		audioSource = GetComponent<AudioSource>();

		shootCooldown = 1f / (rateOfFire / 60f);
		shootOffset = shootCooldown * shootOffsetRatio;

		rotation = Quaternion.LookRotation(transform.forward, Vector3.up);
	}

	public void SetParentUnit(Unit unit)
	{
		parentUnit = unit;
		team = parentUnit.team;
	}

	public void SetOnHitStatus(Status status)
	{
		onHitStatus = status;
	}

	// Update is called once per frame
	void Update()
	{
		if (state == 1 && !target)
			UpdateTarget();

		// 0 = standby, 1 = shooting
		if (state == 0)
		{
			targetInRange = false;
			AttemptEarlyReload();

			Rotate(transform.forward); // Default aim
		}
		else
		{
			Aim(); // Rotate towards target if possible
			StartShooting();
		}
	}

	void Aim()
	{
		Vector3 difference = target ? target.transform.position - transform.position : transform.forward * 0.25f; // To make sure its always in range

		if (difference.sqrMagnitude <= range * range)
		{
			targetInRange = true;
		}
		else
		{
			targetInRange = false;
			AttemptEarlyReload();
		}

		Rotate(difference.normalized);
	}

	bool CheckFriendlyFire()
	{
		// We know how many potential collisions we can have with the parent unit's colliders, so we will cast multiple rays in succession to skip past the collisions that we want to ignore
		// RaycastAll will not work in this case because it will pass through everything, rather than only passing through the parent unit's colliders and stopping on the first collision after them
		Collider[] cols = parentUnit.GetComponentsInChildren<Collider>();

		// First, try to raycast and hope we don't hit ourselves
		Vector3 forward = baseRotatesOnY ? pivotX.forward : pivotY.forward;
		RaycastHit hit;
		float offset = 0.02f; // How much we move in towards our first raycast hit location to make sure the next raycast is technically inside the collider we hit the first time around
		if (Physics.Raycast(firePos.position, forward, out hit, range * gameRules.PRJfriendlyFireRangeMult, gameRules.entityLayerMask))
		{
			// Is it a unit? This could be either self-detection or hitting a different unit.
			Transform parent = hit.collider.transform.parent;
			Unit unit = parent ? parent.GetComponent<Unit>() : null;
			if (unit)
			{
				if (unit.team == team)
				{
					// If we hit a non-parent teammate, immediately return false.
					if (unit != parentUnit)
					{
						return false;
					}
					else
					{
						// Here's the fun part. We have to try to brute-force through the parent unit's colliders
						for (int i = 0; i < cols.Length; i++)
						{
							// TODO: Reuse hit
							// Start where the last raycast left off, plus moved in a little bit to make sure we dont hit the same collider again
							if (Physics.Raycast(hit.point + forward * offset, forward, out hit, range * gameRules.PRJfriendlyFireRangeMult, gameRules.entityLayerMask))
							{
								parent = hit.collider.transform.parent;
								unit = parent ? parent.GetComponent<Unit>() : null;
								if (unit)
								{
									if (unit.team == team)
									{
										// If we hit a non-parent teammate, immediately return false.
										if (unit != parentUnit)
										{
											return false;
										}
										// If we hit the parent unit, the hit.point from this raycast will be used by the next raycast as a starting point
									} // teammate 2
								} // unit 2
							} // second raycast
						}
					} // parent
				} // teammate
			} // unit
		} // first raycast
		return true;
	}

	// TODO: Consolidate conditions which appear in both attempt shot and start shooting
	void StartShooting()
	{
		if (isShooting) // Don't start shooting if we are already shooting
		{
			return;
		}

		if (!targetInRange) // Don't start shooting while out of range
		{
			return;
		}

		if (isReloading) // Don't start shooting if we are reloading
		{
			if (!isReloadCancellable)
				return;
			else
			{
				StopCoroutine(reloadCoroutine);
				isReloadCancellable = false;
				isReloading = false;
			}
		}

		// Are we pointed at the target?
		Vector3 forward = baseRotatesOnY ? pivotX.forward : pivotY.forward;
		float dot = Mathf.Max(Vector3.Dot(direction, forward), 0);

		if (dot < 1 - allowShootThressh)
		{
			return;
		}

		// Is our line of fire blocked by an allied unit?
		if (!CheckFriendlyFire())
			return;

		isShooting = true;
		StartCoroutine(CoroutineToggleFiringAudio(true, shootOffset)); // Start firing audio loop
		AttemptShot(shootOffset); // Start first shot, which then calls the next shot recursively
	}

	IEnumerator CoroutineToggleFiringAudio(bool play, float delay)
	{
		yield return new WaitForSeconds(delay);
		ToggleFiringAudio(play);
	}

	void ToggleFiringAudio(bool play)
	{
		if (!audioSource.clip)
			return;

		if (play)
		{
			if (!audioSource.isPlaying)
			{
				audioSource.Play();
			}
		}
		else
		{
			if (audioSource.isPlaying)
			{
				audioSource.Stop();
				
				AudioUtils.PlayClipAt(soundShoot, transform.position, audioSource);
			}
		}
	}

	void AttemptShot(float delay)
	{
		if (curAmmo <= 0 && maxAmmo > 0) // If we run out of ammo mid-shooting, start reload
		{
			isShooting = false;
			ToggleFiringAudio(false); // Stop firing audio loop

			if (!isReloading) // Reload
				Reload();
			return;
		}

		if (!targetInRange) // If we end up out of range mid-shooting, stop
		{
			isShooting = false;
			ToggleFiringAudio(false); // Stop firing audio loop
			return;
		}

		if (isReloading) // If we have ammo left and are in range, cancel the current reload
		{
			if (!isReloadCancellable)
			{
				ToggleFiringAudio(false); // Stop firing audio loop
				return;
			}
			else // Shot after manually clearing target
			{
				StopCoroutine(reloadCoroutine);
				isReloadCancellable = false;
				isReloading = false;
			}
		}

		// Are we pointed at the target?
		Vector3 forward = baseRotatesOnY ? pivotX.forward : pivotY.forward;
		float dot = Mathf.Max(Vector3.Dot(direction, forward), 0);

		if (dot < 1 - allowShootThressh)
		{
			isShooting = false;
			ToggleFiringAudio(false); // Stop firing audio loop
			return;
		}

		// Is our line of fire blocked by an allied unit?
		if (!CheckFriendlyFire())
		{
			isShooting = false;
			ToggleFiringAudio(false); // Stop firing audio loop
			return;
		}

		StartCoroutine(CoroutineShoot(delay));
	}

	void AttemptEarlyReload()
	{
		if (isReloading)
			return;

		if (curAmmo >= maxAmmo)
			return;

		Reload();
		isReloadCancellable = true;
	}

	void Reload()
	{
		isReloading = true;
		reloadCoroutine = StartCoroutine(CoroutineReload());
	}

	IEnumerator CoroutineShoot(float delay)
	{
		yield return new WaitForSeconds(delay);
		Fire();
		yield return new WaitForSeconds(shootCooldown);
		AttemptShot(0);
	}

	IEnumerator CoroutineReload()
	{
		yield return new WaitForSeconds(reloadCooldown);

		isReloading = false;
		isReloadCancellable = false; // Cancellable status should not carry over to the next reload
		curAmmo = maxAmmo;
	}

	void Fire()
	{
		curAmmo--;

		Vector3 forward = baseRotatesOnY ? pivotX.forward : pivotY.forward;
		// Projectile
		for (int i = 0; i < pelletCount; i++)
		{
			Vector2 error = Random.insideUnitCircle * (accuracy / 10f);
			Vector3 errForward = (forward + ((baseRotatesOnY ? pivotX.right : pivotY.right) * error.x) + ((baseRotatesOnY ? pivotX.up : pivotY.up) * error.y)).normalized;

			if (onHitStatus == null)
				projs.SpawnProjectile(projTemplate, firePos.position, errForward, parentUnit, null); // TODO: Do we need to make a new status each time?
			else
				projs.SpawnProjectile(projTemplate, firePos.position, errForward, parentUnit, new Status(onHitStatus.from, onHitStatus.statusType)); // TODO: Do we need to make a new status each time?
		}

		// Sound
		if (!audioSource.clip)
			AudioUtils.PlayClipAt(soundShoot, transform.position, audioSource);
	}

	void Rotate(Vector3 difference)
	{
		// What do we rotate towards?
		if (targetInRange)
		{
			// How far to aim ahead given how long it would take to reach current position
			Vector3 offsetTarget = target.transform.position + target.GetVelocity() * (difference.magnitude / projTemplate.GetSpeed());
			// How far to aim ahead given how long it would take to reach predicted position
			Vector3 offsetTargetAdj = target.transform.position + target.GetVelocity() * ((offsetTarget - transform.position).magnitude / projTemplate.GetSpeed());
			difference = offsetTargetAdj - transform.position;
		}
		else
			difference = transform.forward;

		// Fixes strange RotateTowards bug
		Quaternion resetRot = rotation;

		// Rotate towards our target
		direction = difference.normalized;
		lookRotation = Quaternion.LookRotation(direction, Vector3.up);


		Quaternion newRotation = Quaternion.RotateTowards(rotation, lookRotation, Time.deltaTime * RS);

		// Fixes strange RotateTowards bug
		if (Time.frameCount == resetRotFrame)
			newRotation = resetRot;

		Quaternion limitRot = LimitRotation(newRotation, rotation);

		// Limit
		rotation = limitRot;

		if (parentUnit.printInfo)
		{
			if (newRotation != limitRot)
				Debug.Log("false");
			else
				Debug.Log("true");
		}

		if (baseRotatesOnY)
		{
			if (pivotY)
				pivotY.rotation = Quaternion.Euler(new Vector3(0, rotation.eulerAngles.y, 0));
			if (pivotX)
				pivotX.rotation = Quaternion.Euler(new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y, 0));
		}
		else
		{
			if (pivotY)
				pivotY.rotation = Quaternion.Euler(new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y, 0));
			if (pivotX)
				pivotX.rotation = Quaternion.Euler(new Vector3(0, rotation.eulerAngles.y, 0));
		}
	}

	Quaternion LimitRotation(Quaternion rot, Quaternion oldRot)
	{
		Debug.DrawRay(transform.position, rot * Vector3.forward, Color.white);
		Debug.DrawRay(transform.position, lookRotation * Vector3.forward, Color.red);
		Debug.DrawRay(transform.position, transform.forward, Color.black);
		Debug.DrawRay(transform.position, transform.up, Color.black);

		Vector3 components = rot.eulerAngles;
		Vector3 componentsOld = oldRot.eulerAngles;
		Vector3 hComponentFwd = Quaternion.LookRotation(transform.forward).eulerAngles;
		Vector3 vComponentFwd = Quaternion.LookRotation(transform.up).eulerAngles;

		float angleH = Vector3.SignedAngle(transform.forward, rot * Vector3.forward, Vector3.up);
		float angleV = Vector3.Angle(transform.up, rot * Vector3.forward);

		// Horizontal extremes
		if (angleH > maxH)
		{
			components.y = hComponentFwd.y + maxH;
		}
		else if (angleH < -minH)
		{
			components.y = hComponentFwd.y - minH;
		}
	
		// Vertical extremes
		// Don't have to do anything besides bad rotations because units will never rotate on their Z or X axes
		if (angleV > maxV)
		{
			components.x = componentsOld.x;
		}
		else if (angleV < minV)
		{
			components.x = componentsOld.x;
		}

		return Quaternion.Euler(components);
	}

	float ClampAngle(float angle, float min, float max)
	{
		return angle;
	}

	public void SetTarget(Unit newTarg)
	{
		target = newTarg;

		UpdateTarget();
	}

	private void UpdateTarget()
	{
		resetRotFrame = Time.frameCount;

		if (target)
		{
			if (state == 0)
				state = 1;
		}
		else
		{
			state = 0;
		}

		//Debug.Log("Turret aiming at " + (target ? target.DisplayName : "null"));
	}

	// Visualize range of turrets in editor
	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(transform.position, range);
	}
}
