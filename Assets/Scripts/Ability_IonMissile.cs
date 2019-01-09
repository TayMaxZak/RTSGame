using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_IonMissile : Ability
{
	private float reloadTimer;
	private Vector3 deltaDurations;

	[SerializeField]
	private Transform[] startPosition;
	private int startIndexCur = 0;
	//[SerializeField]
	//private float initAngle = 45;

	[SerializeField]
	private float maxRS = 45; // Turning speed of missile
	private float RS;
	[SerializeField]
	private float MS = 8; // Movement speed of missile

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

	[SerializeField]
	private Effect_Point explosionPrefab;
	private Effect_Point explosion;

	private int state = 0; // 0 = standby, 1 = targeting (has a target, is turning towards it), 2 = countdown (will shoot after a fixed time delay)
	private float startTargetTime;
	private int attemptShotWhen;
	private bool checkIfDead = false;

	private Vector3 targetPosition;
	private Unit targetUnit;
	private bool checkingForDead = false;


	private LayerMask mask;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.IonMissile;
		InitCooldown();

		stacks = gameRules.ABLY_ionMissileMaxAmmo;
		deltaDurations = AbilityUtils.GetDeltaDurations(abilityType);

		mask = gameRules.collisionLayerMask;

		displayInfo.stacks = stacks;
		displayInfo.displayStacks = true;
		displayInfo.displayFill = true;
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
		displayInfo.fill = 1 - fill;
		UpdateDisplay(abilityIndex, false);
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		//startPosition.localRotation = Quaternion.Euler(0, 0, 0);

		missile = Instantiate(missilePrefab);
		missile.SetEffectActive(false);

		explosion = Instantiate(explosionPrefab);
		explosion.SetEffectActive(false);
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

		if (state == 0) // Standby
		{
			if (!offCooldown)
				return;

			if (stacks < 1)
				return;

			if (InRange(target.unit.transform))
			{
				base.UseAbility(target);
				ResetCooldown(); // Cooldown is applied later

				startTargetTime = Time.time;
				BeginTargeting(target.unit);
				attemptShotWhen = Time.frameCount + 1; // We should start aim checking in 1 frame from now
			}
		}
		else if (state == 1) // Targeting
		{
			if (Time.time + gameRules.ABLY_ionMissileCancelTime > startTargetTime)
			{
				base.UseAbility(target);
				ResetCooldown();
				Reset();
			}
		}
		else if (state == 2)
		{
			if (InRange(target.unit.transform))
			{
				SwitchTargets(target.unit);
			}
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

		parentUnit.SetAbilityGoal(new AbilityTarget(targetUnit)); // Turn towards it
		
	}

	void SwitchTargets(Unit unit)
	{
		if (unit.team == team) // Cannot target allies
			return;

		targetUnit = unit; // Set new target
		checkIfDead = true; // Our target may become null
	}

	void SpawnMissile()
	{
		state = 2; // Missile in the air state

		missile.transform.position = startPosition[startIndexCur].position;
		missile.transform.rotation = startPosition[startIndexCur].rotation;

		// Lifetime cannot exceed cooldown time or time to cross ability cast range
		missileLifetime = Mathf.Min(gameRules.ABLY_ionMissileMaxLifetime, 1 / cooldownDelta);

		missile.SetEffectActive(true);
		missileActive = true;

		// Rotate at a speed to just barely reach the correct orientation above the target position
		//RS = Mathf.Min(2 * (90 - initAngle) / ((targetPosition - targetUnit.transform.position).magnitude / MS), maxRS);
		RS = maxRS;

		// Change firing position
		startIndexCur++;
		if (startIndexCur > startPosition.Length - 1)
			startIndexCur = 0;
	}

	new void Update()
	{
		base.Update();

		// Targeting
		if (state == 1)
		{
			if (targetUnit) // We have something to aim at
			{
				CalculateTargetPosition();
				if (InRange(targetUnit.transform)) // In range
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
			Debug.DrawLine(launcher.transform.position, pos, Color.blue);
			Rotate((pos - launcher.transform.position).normalized);
		}
		else
		{
			Rotate(transform.forward);
		}

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

	void Rotate(Vector3 direction)
	{
		// Fixes strange RotateTowards bug
		Quaternion resetRot = Quaternion.identity;

		// Rotate towards the desired look rotation
		// TODO: Sometimes super slow
		Debug.DrawRay(launcher.transform.position, direction, Color.red);
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

	void Explode(bool intentional)
	{
		Explode(intentional, new RaycastHit());
	}

	void Explode(bool intentional, RaycastHit hit)
	{
		missile.SetEffectActive(false);
		missileActive = false;

		//Ability_ionMissile_Cloud cloud = Instantiate(damageCloud, missile.transform.position, Quaternion.identity);
		//cloud.SetParentUnit(parentUnit);

		explosion.transform.position = missile.transform.position;
		explosion.transform.rotation = hit.collider ? Quaternion.LookRotation((missile.transform.forward + hit.normal * -1) / 2) : missile.transform.rotation;
		explosion.SetEffectActive(true);

		Reset();
	}

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
		if (missileActive)
			Explode(false); // TODO: Should missile continue flying after its parent unit dies?
		missile.End();
	}

	bool InRange(Transform tran)
	{
		if (Vector3.SqrMagnitude(tran.position - transform.position) < gameRules.ABLY_ionMissileRangeUse * gameRules.ABLY_ionMissileRangeUse)
			return true;
		else
			return false;
	}
}
