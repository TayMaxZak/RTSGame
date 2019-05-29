using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Bombs : Ability
{
	private float reloadTimer;
	private Vector3 deltaDurations;

	[SerializeField]
	private Transform lockRayPosition;

	[SerializeField]
	private float lockRayDistance = 35;
	[SerializeField]
	private float lockRayAngle = 30;

	[SerializeField]
	private Transform[] startPosition;
	private int startIndexCur = 0;
	//[SerializeField]
	//private float initAngle = 45;

	[SerializeField]
	private GameObject bombPrefab;
	[SerializeField]
	private GameObject explosionPrefab;

	[Header("Launcher")]
	[SerializeField]
	private GameObject launcher;
	private Quaternion rotation;
	[SerializeField]
	private float verticalRS = 5;
	[SerializeField]
	private float minV = 50;
	[SerializeField]
	private float maxV = 170;
	private float aimThreshold = 0.001f;

	[SerializeField]
	private Effect_Point missilePrefab;
	private Effect_Point missile;
	private float missileLifetime;
	private bool missileActive = false;

	//[SerializeField]
	//private Effect_Point explosionPrefab;
	//private Effect_Point explosion;

	private int state = 0; // 0 = standby, 1 = targeting (has a target, is turning towards it), 2 = countdown (will shoot after a fixed time delay)
	private float startTargetTime;
	private int attemptShotWhen;
	private bool checkIfDead = false;

	private Vector3 targetPosition;
	private bool checkingForDead = false;


	private float lockOnRemaining;
	private float keepTargetRemaining;
	private Unit targetUnit;

	[SerializeField]
	private float leanMult = 0.5f;


	private float parentUnitMS;

	[SerializeField]
	private AudioSource lockOnSound;

	private LayerMask mask;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.Bombs;
		InitCooldown();

		stacks = gameRules.ABLY_bombsStacks;
		deltaDurations = AbilityUtils.GetDeltaDurations(abilityType);

		lockOnRemaining = gameRules.ABLY_bombsLockOnTime;

		mask = gameRules.collisionLayerMask;

		displayInfo.stacks = stacks;
		displayInfo.displayStacks = true;
		//displayInfo.displayFill = true;
	}

	void DisplayStacks()
	{
		displayInfo.stacks = stacks;
		//if (stacks <= 0)
		//	displayInfo.displayInactive = true;
		//else
		//	displayInfo.displayInactive = false;
		UpdateDisplay(abilityIndex, true);
	}

	void DisplayFill(float fill)
	{
		//displayInfo.fill = 1 - fill;
		UpdateDisplay(abilityIndex, false);
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		parentUnitMS = parentUnit.GetMovement().GetMovementSpeed();
		//startPosition.localRotation = Quaternion.Euler(0, 0, 0);

		//missile = Instantiate(missilePrefab);
		//missile.SetEffectActive(false);

		//explosion = Instantiate(explosionPrefab);
		//explosion.SetEffectActive(false);
	}

	// Aim for one second ahead of the target
	void CalculateTargetPosition()
	{
		targetPosition = targetUnit.transform.position + targetUnit.GetVelocity();
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (suspended)
			return;

		if (keepTargetRemaining > 0) // We have a target
		{
			if (!offCooldown)
				return;

			if (stacks < 1)
				return;

			//if (InRange(targetUnit.transform))
			//{
			base.UseAbility(target);

			targetUnit.Damage(gameRules.ABLY_bombsCount * (gameRules.ABLY_bombsDamage + gameRules.ABLY_bombsDamageBonusMult * targetUnit.GetHP().y), 0, DamageType.Bomb);
			

			Instantiate(bombPrefab, startPosition[startIndexCur].position, lockRayPosition.rotation);
			Instantiate(explosionPrefab, targetUnit.transform.position + Vector3.up * 0.5f, Quaternion.identity);

			ResetKeepTarget();
			ResetLockOn();

			stacks--;
			DisplayStacks();
			//}

		}

		// While a missile is already active, a new target for the current missile can be selected
	}

	void BeginTargeting(Unit unit)
	{
		if (unit.team == team) // Cannot target allies
			return;

		state = 1; // Targeting state

		targetUnit = unit; // Set new target
		checkIfDead = true; // Our target may become null
	}

	void SwitchTargets(Unit unit)
	{
		if (unit.team == team) // Cannot target allies
			return;

		targetUnit = unit; // Set new target
		checkIfDead = true; // Our target may become null
	}

	//void SpawnMissile()
	//{
	//	state = 2; // Missile in the air state

	//	missile.transform.position = startPosition[startIndexCur].position;
	//	missile.transform.rotation = startPosition[startIndexCur].rotation;

	//	// Lifetime cannot exceed cooldown time or time to cross ability cast range
	//	missileLifetime = Mathf.Min(gameRules.ABLY_ionMissileMaxLifetime, 1 / cooldownDelta);

	//	missile.SetEffectActive(true);
	//	missileActive = true;

	//	// Rotate at a speed to just barely reach the correct orientation above the target position
	//	//RS = Mathf.Min(2 * (90 - initAngle) / ((targetPosition - targetUnit.transform.position).magnitude / MS), maxRS);
	//	//RS = maxRS;

	//	// Change firing position
	//	startIndexCur++;
	//	if (startIndexCur > startPosition.Length - 1)
	//		startIndexCur = 0;
	//}

	new void Update()
	{
		base.Update();

		// Lose lock
		if (keepTargetRemaining > 0 && lockOnRemaining > 0)
		{
			keepTargetRemaining -= Time.deltaTime;
			if (keepTargetRemaining <= 0)
			{
				ResetKeepTarget();
				ResetLockOn();
			}
		}

		lockRayPosition.transform.forward = Vector3.down + leanMult * parentUnit.transform.forward * parentUnit.GetMovement().GetHVelocity().magnitude / parentUnitMS;

		// Lock on behaviour
		int raysConnected = 0;
		if (offCooldown && lockOnRemaining > 0)
		{
			Unit newTargetUnit = null;

			for (int i = 0; i < 5; i++)
			{
				// Raycast connects, too far from parent unit, or out of lifetime

				Vector3 noAngle = lockRayPosition.forward;
				Vector3 newVector = new Vector3();
				if (i == 0)
					newVector = noAngle;
				else if(i == 1)
					newVector = Quaternion.AngleAxis(0 + 45, -lockRayPosition.forward) * Quaternion.AngleAxis(lockRayAngle, lockRayPosition.up) * noAngle;
				else if (i == 2)
					newVector = Quaternion.AngleAxis(90 + 45, -lockRayPosition.forward) * Quaternion.AngleAxis(lockRayAngle, lockRayPosition.up) * noAngle;
				else if (i == 3)
					newVector = Quaternion.AngleAxis(180 + 45, -lockRayPosition.forward) * Quaternion.AngleAxis(lockRayAngle, lockRayPosition.up) * noAngle;
				else if (i == 4)
					newVector = Quaternion.AngleAxis(270 + 45, -lockRayPosition.forward) * Quaternion.AngleAxis(lockRayAngle, lockRayPosition.up) * noAngle;

				RaycastHit hit;
				Ray rayCharles = new Ray(lockRayPosition.position, newVector);

				

				if (parentUnit.printInfo)
					Debug.DrawLine(rayCharles.origin, rayCharles.origin + rayCharles.direction * lockRayDistance, Color.red);
				//bool hitSelf = false;
				if (Physics.Raycast(rayCharles, out hit, lockRayDistance, mask))
				{
					Unit unit = null;
					if (hit.collider.transform.parent) // Is this a unit?
					{
						unit = hit.collider.transform.parent.GetComponent<Unit>();
						if (unit) // Is this a unit?
						{
							if (unit != parentUnit && unit.team != team) // If we hit a unit and its not us, damage it
							{
								if (newTargetUnit == null)
									newTargetUnit = unit;
								else
								{
									if (unit == newTargetUnit)
									{
										if (i == 4)
											raysConnected += 10;
										else
											raysConnected++;
									}
									else if (parentUnit.printInfo)
										Debug.Log(i + " not target unit");
									// else doesn't matter
								}
							}
							else if (parentUnit.printInfo)
								Debug.Log(i + " its us or its an ally");
						} // if unit
						else if (parentUnit.printInfo)
							Debug.Log(i + " not a unit");
					} // if has parent
					else if (parentUnit.printInfo)
						Debug.Log(i + " no parent");
				} // if raycast
				else if (parentUnit.printInfo)
					Debug.Log(i + " missed ray");
			} // for

			targetUnit = newTargetUnit;

			if (parentUnit.printInfo)
				Debug.Log(raysConnected);

			// At least 1 ray have to connect for lock-on to continue
			if (raysConnected >= 1)
			{
				lockOnRemaining -= Time.deltaTime;
			}
			else
			{
				ResetLockOn();
			}

			// Locking on
			if (lockOnRemaining < gameRules.ABLY_bombsLockOnTime)
			{
				Debug.Log("AAAAAAAA " + lockOnRemaining);
				if (!lockOnSound.isPlaying)
				{
					lockOnSound.Play();
				}

				if (lockOnRemaining < 0)
				{
					SucceedLockOn();
				}
			}

			/*
			if (!hit.collider || hitSelf)
			{
				if ((startPosition[startIndexCur].position - missile.transform.position).sqrMagnitude > gameRules.ABLY_ionMissileRangeMissile * gameRules.ABLY_ionMissileRangeMissile || missileLifetime <= 0)
				{
					Explode(false);
				}
				else
				{
					missileLifetime -= Time.deltaTime;

					missile.transform.rotation = Quaternion.RotateTowards(missile.transform.rotation, Quaternion.LookRotation(targetPosition - missile.transform.position), RS * Time.deltaTime);
					missile.transform.position += missile.transform.forward * MS * Time.deltaTime;
				}
			} // if failed raycast
			*/
		} // if missileActive













		/*
		// Targeting
		if (state == 1)
		{
			if (targetUnit) // We have something to aim at
			{
				CalculateTargetPosition();
				if (InRange(targetUnit.transform)) // In range
				{
					if (targetUnit.VisibleBy(team))
					{
						if (Time.frameCount > attemptShotWhen) // Should be checking for aim
						{
							if (Vector3.Dot(launcher.transform.forward, (targetPosition - launcher.transform.position).normalized) > 1 - aimThreshold) // Aimed close enoughs
							{
								stacks--;
								DisplayStacks();

								// Start countdown once we are properly aimed
								SpawnMissile();
								StartCoroutine(ClearAbilityGoalCoroutine());

								StartCooldown();
							}
						}
					}
					else
					{
						Reset();
						// No cooldown
					} // visible
				}
				else
				{
					Reset();
					// No cooldown
				} // in range
			}
			else // Target died early
			{
				if (checkIfDead)
				{
					Reset();
					// No cooldown
				}
			} // target alive

		} // if state 1
		else if (state == 2) // missile in the air
		{
			if (targetUnit)
			{
				CalculateTargetPosition();
			}
			else
			{
				if (checkingForDead)
				{
					// Clear target but don't reset
					targetUnit = null;
					checkingForDead = false;
				}
			}
		}

		if (state == 1 && targetUnit)
		{
			// Do rotation stuff
			// Construct an imaginary target that only differs from the launcher's current orientation by the Y-axis
			float dist = new Vector2(launcher.transform.position.x - targetUnit.transform.position.x, launcher.transform.position.z - targetUnit.transform.position.z).magnitude;
			Vector3 pos = launcher.transform.position + transform.forward * dist;
			pos.y = targetUnit.transform.position.y;
			// Rotate launcher vertically
			Rotate((pos - launcher.transform.position).normalized);
		}
		else
		{
			Rotate(transform.forward);
		}
		*/











		/*
		// Missile behavior
		if (missileActive)
		{
			// Raycast connects, too far from parent unit, or out of lifetime
			RaycastHit hit;
			bool hitSelf = false;
			if (Physics.Raycast(missile.transform.position, missile.transform.forward, out hit, MS * Time.deltaTime, mask))
			{
				Unit unit = null;
				if (hit.collider.transform.parent) // Is this a unit?
				{
					unit = hit.collider.transform.parent.GetComponent<Unit>();
					if (unit) // Is this a unit?
					{
						if (unit != parentUnit) // If we hit a unit and its not us, damage it
						{
							//DamageResult result = unit.Damage(gameRules.ABLY_ionMissileDamage + gameRules.ABLY_ionMissileDamageBonusMult * curShields + gameRules.ABLY_ionMissileDamageBonusMult * maxShields, (startPosition[startIndexCur].position - hit.point).magnitude, DamageType.IonMissile);
							DamageResult result = unit.Damage(gameRules.ABLY_ionMissileDamage, (startPosition[startIndexCur].position - hit.point).magnitude, DamageType.IonMissile);

							// Is the unit still alive?
							if (!result.lastHit)
							{
								// Don't do anything else if shields are still up
								if (unit.GetShields().x <= 0)
								{
									unit.AddStatus(new Status(parentUnit.gameObject, StatusType.IonSuppressed));

									if (unit.GetIons() <= gameRules.ABLY_ionMissileDecayCutoff)
										unit.AddIons(gameRules.ABLY_ionMissileIonsFirst, true);
									else
										unit.AddIons(gameRules.ABLY_ionMissileIonsNext, true);
								}
								else
								{
									// TODO: Add ions to shield source? Damage shield source?
								}
							}
						}
						else
						{
							// Ignore this collision
							hitSelf = true;
						}
					}

					if (!hitSelf)
						Explode(true, hit);
				} // if unit
				else
					Explode(true, hit);
			} // if raycast

			if (!hit.collider || hitSelf)
			{
				if ((startPosition[startIndexCur].position - missile.transform.position).sqrMagnitude > gameRules.ABLY_ionMissileRangeMissile * gameRules.ABLY_ionMissileRangeMissile || missileLifetime <= 0)
				{
					Explode(false);
				}
				else
				{
					missileLifetime -= Time.deltaTime;

					missile.transform.rotation = Quaternion.RotateTowards(missile.transform.rotation, Quaternion.LookRotation(targetPosition - missile.transform.position), RS * Time.deltaTime);
					missile.transform.position += missile.transform.forward * MS * Time.deltaTime;
				}
			} // if failed raycast
		} // if missileActive
		*/
		// Reload
		if (stacks < gameRules.ABLY_ionMissileMaxAmmo)
		{
			reloadTimer += deltaDurations.y * Time.deltaTime;

			DisplayFill(reloadTimer);

			if (reloadTimer >= 1)
			{
				stacks++;
				DisplayStacks();
				reloadTimer = 0;
			}
		}
	}

	// Reset targetUnit
	void ResetKeepTarget()
	{
		//targetUnit = null; // Always called with reset lock on
		keepTargetRemaining = 0;
	}

	void ResetLockOn()
	{
		lockOnRemaining = gameRules.ABLY_bombsLockOnTime;
		lockOnSound.Stop();

		targetUnit = null;
	}

	void SucceedLockOn()
	{
		keepTargetRemaining = gameRules.ABLY_bombsKeepTargetTime;
	}

	void Rotate(Vector3 direction)
	{
		// Fixes strange RotateTowards bug
		Quaternion resetRot = Quaternion.identity;

		// Rotate towards the desired look rotation
		Quaternion newRotation = Quaternion.RotateTowards(launcher.transform.rotation, Quaternion.LookRotation(direction, Vector3.up), Time.deltaTime * verticalRS);

		// Limit rotation
		newRotation = LimitVerticalRotation(newRotation, rotation);
		rotation = newRotation;

		// Fixes strange RotateTowards bug
		if (Time.frameCount == attemptShotWhen + 1)
			newRotation = resetRot;

		// Apply to cannon
		//cannon.transform.localRotation = Quaternion.Euler(new Vector3(rotation.eulerAngles.x, 0, 0));
		launcher.transform.rotation = rotation;
	}

	Quaternion LimitVerticalRotation(Quaternion rot, Quaternion oldRot)
	{
		Vector3 components = rot.eulerAngles;
		Vector3 componentsOld = oldRot.eulerAngles;
		Vector3 vComponentFwd = Quaternion.LookRotation(transform.up).eulerAngles;

		float angleV = Vector3.Angle(transform.up, rot * Vector3.forward);

		// Vertical extremes
		// Don't have to do anything besides ignoring bad rotations because units will never rotate on their Z or X axes
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

	//void Explode(bool intentional)
	//{
	//	Explode(intentional, new RaycastHit());
	//}

	//void Explode(bool intentional, RaycastHit hit)
	//{
	//	missile.SetEffectActive(false);
	//	missileActive = false;

	//	//Ability_ionMissile_Cloud cloud = Instantiate(damageCloud, missile.transform.position, Quaternion.identity);
	//	//cloud.SetParentUnit(parentUnit);

	//	explosion.transform.position = missile.transform.position;
	//	explosion.transform.rotation = hit.collider ? Quaternion.LookRotation((missile.transform.forward + hit.normal * -1) / 2) : missile.transform.rotation;
	//	explosion.SetEffectActive(true);

	//	Reset();
	//}

	void Reset()
	{
		state = 0; // Back to standby state

		targetUnit = null;
		checkingForDead = false;

		parentUnit.ClearAbilityGoal();
	}

	IEnumerator ClearAbilityGoalCoroutine()
	{
		yield return new WaitForSeconds(0.2f);
		parentUnit.ClearAbilityGoal();
	}

	public override void End()
	{
		//if (missileActive)
		//	Explode(false); // TODO: Should missile continue flying after its parent unit dies?
		//missile.End();
	}

	bool InRange(Transform tran)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < gameRules.ABLY_ionMissileRangeUse * gameRules.ABLY_ionMissileRangeUse)
			return true;
		else
			return false;
	}
}
