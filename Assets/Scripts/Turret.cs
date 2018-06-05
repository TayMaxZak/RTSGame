using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
	public int team = 0;
	private int state = 0; // 0 = standby, 1 = shooting

	[SerializeField]
	private Projectile projTemplate;

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
	private float allowShootThressh = 0.001f;

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
	private Transform pivotY;
	[SerializeField]
	private Transform pivotX;

	[Header("Rotation Limits")]
	[SerializeField]
	private float minX;
	[SerializeField]
	private float maxX;


	private bool targetInRange;
	private bool isReloading;
	private bool isShooting;

	private Quaternion lookRotation;
	private Quaternion forwardRotation;
	private Vector3 direction;

	private int resetRotFrame;

	//private GameRules gameRules;
	private Manager_Projectiles projs;
	private AudioSource audioSource;

	// Use this for initialization
	void Start()
	{
		curAmmo = maxAmmo;

		//gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules
		projs = GameObject.FindGameObjectWithTag("ProjsManager").GetComponent<Manager_Projectiles>(); // Grab copy of Projectiles Manager
		audioSource = GetComponent<AudioSource>();

		shootCooldown = 1f / (rateOfFire / 60f);
		shootOffset = shootCooldown * shootOffsetRatio;
	}

	// Update is called once per frame
	void Update()
	{
		if (state == 1 && !target)
			UpdateTarget();

		//Debug.Log("STATE = " + state);

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

	void StartShooting()
	{
		if (!targetInRange) // Don't start shooting while out of range
		{
			return;
		}

		if (isShooting) // Don't start shooting if we are already shooting
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
				//Debug.Log(audioSource.time);
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
				return;
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

			projs.SpawnProjectile(projTemplate, team, firePos.position, errForward);
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
		Quaternion resetRot = Quaternion.Euler(new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y, 0));

		// Rotate towards our target
		direction = difference.normalized;
		lookRotation = Quaternion.LookRotation(direction, Vector3.up);
		
		rotation = Quaternion.RotateTowards(rotation, lookRotation, Time.deltaTime * RS);

		// Fixes strange RotateTowards bug
		if (Time.frameCount == resetRotFrame)
			rotation = resetRot;

		//LimitRotation();

		// Apply rotation to object
		if (baseRotatesOnY)
		{
			pivotY.rotation = Quaternion.Euler(new Vector3(0, rotation.eulerAngles.y, 0));
			pivotX.rotation = Quaternion.Euler(new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y, 0));
		}
		else
		{
			pivotY.rotation = Quaternion.Euler(new Vector3(rotation.eulerAngles.x, rotation.eulerAngles.y, 0));
			pivotX.rotation = Quaternion.Euler(new Vector3(rotation.eulerAngles.x, 0, 0));
		}
	}

	void LimitRotation()
	{
		forwardRotation = Quaternion.LookRotation(transform.forward, Vector3.up);

		Vector3 rotationEuler = new Vector3(0, ClampAngle(rotation.eulerAngles.y, forwardRotation.eulerAngles.y - 90, forwardRotation.eulerAngles.y + 90), 0);
		rotation = Quaternion.Euler(rotationEuler);
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
}
