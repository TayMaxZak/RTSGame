using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
	[Header("Targeting")]
	private Unit target;
	[SerializeField]
	private float range = 25;

	[Header("Sound")]
	[SerializeField]
	private AudioClip soundShoot;

	[SerializeField]
	private Projectile projTemplate;

	[Header("Shooting")]
	[SerializeField]
	private Transform firePos;
	private int curAmmo = 4;
	[SerializeField]
	private int maxAmmo = 4;
	[SerializeField]
	private float reloadCooldown = 4;
	private float reloadTimer;

	[SerializeField]
	private float rateOfFire = 120; // In RPM
	private float shootCooldown;
	private float shootTimer = 0;

	[Header("Turning")]
	[SerializeField]
	private float RS = 90;
	[SerializeField]
	private float RSAccel = 1;
	private float curRSRatio = 0;
	/*
	[SerializeField]
	private float bankAngle = 30;
	*/
	private Quaternion rotation;
	[SerializeField]
	private bool baseRotatesOnY = true;
	[SerializeField]
	private Transform pivotY;
	[SerializeField]
	private Transform pivotX;


	private bool isTargeting;
	private Quaternion lookRotation;
	private Vector3 direction;
	[SerializeField]
	private float allowShootThresh = 0.1f;

	private GameRules gameRules;
	private Manager_Projectiles projs;

	private AudioSource audioSource;

	// Use this for initialization
	void Start()
	{
		curAmmo = maxAmmo;
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules
		projs = GameObject.FindGameObjectWithTag("ProjsManager").GetComponent<Manager_Projectiles>(); // Grab copy of Projectiles Manager
		shootCooldown = 1f / (rateOfFire / 60f);

		audioSource = GetComponent<AudioSource>();
	}

	// Banking
	//float bank = bankAngle * -Vector3.Dot(transform.right, direction);
	//banker.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.forward);

	// Update is called once per frame
	void Update()
	{
		Vector3 dif = target ? target.transform.position - transform.position : transform.forward;

		if (target)
		{


			if (dif.sqrMagnitude <= range * range)
				isTargeting = true;
			else
				isTargeting = false;


			// Intercept code TODO: Limit how far ahead it can aim by rotation speed
			// How far to aim ahead given how long it would take to reach current position
			Vector3 offsetTarget = target.transform.position + target.GetVelocity() * (dif.magnitude / projTemplate.speed);
			// How far to aim ahead given how long it would take to reach predicted position
			Vector3 offsetTargetAdj = target.transform.position + target.GetVelocity() * ((offsetTarget - transform.position).magnitude / projTemplate.speed);
			dif = offsetTargetAdj - transform.position;
		}
		else
			isTargeting = false;

		// Rotation
		float targetRSRatio = isTargeting ? 1 : 1;

		float RSdelta = Mathf.Sign(targetRSRatio - curRSRatio) * (1f / RSAccel) * Time.deltaTime;
		curRSRatio = Mathf.Clamp01(curRSRatio + RSdelta);

		direction = dif.normalized;
		lookRotation = Quaternion.LookRotation(direction, Vector3.up);
		rotation = Quaternion.RotateTowards(rotation, lookRotation, Time.deltaTime * RS * curRSRatio);

		//model.rotation = Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, targetBank));
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

		// Ammo
		bool outOfAmmo = maxAmmo > 0 ? curAmmo <= 0 : false;

		shootTimer += Time.deltaTime;
		if (outOfAmmo)
		{

			reloadTimer += Time.deltaTime;
			if (reloadTimer >= reloadCooldown)
			{
				reloadTimer = 0;
				curAmmo = maxAmmo;
			}
		}
		else if (isTargeting) // Has ammo, should be shooting
		{
			// Aimed at target?
			Vector3 forward = baseRotatesOnY ? pivotX.forward : pivotY.forward;
			float dot = Mathf.Max(Vector3.Dot(direction, forward), 0);
			Debug.DrawRay(transform.position, forward, Color.white);


			if (dot >= 0.999f)
			{
				dot = 1;


				if (shootTimer >= shootCooldown)
				{
					projs.SpawnProjectile(projTemplate, firePos.position, forward);

					AudioUtils.PlayClipAt(soundShoot, transform.position, audioSource, gameRules.AUDpitchVariance);

					shootTimer = 0;
					curAmmo--;
				}

			} //dot
		} //isTargeting
	}

	public void SetTarget(Unit newTarg)
	{
		target = newTarg;
		Debug.Log("Turret aiming at " + target.DisplayName);
	}
}
