using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_IonMissile : Ability
{
	private float reloadTimer;
	private Vector3 deltaDurations;

	[SerializeField]
	private Transform startPosition;
	//[SerializeField]
	//private float initAngle = 45;

	[SerializeField]
	private float maxRS = 45; // Turning speed of missile
	private float RS;
	[SerializeField]
	private float MS = 8; // Movement speed of missile
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

		startPosition.localRotation = Quaternion.Euler(0, 0, 0);

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

		missile.transform.position = startPosition.position;
		missile.transform.rotation = startPosition.rotation;

		// Lifetime cannot exceed cooldown time or time to cross ability cast range
		missileLifetime = Mathf.Min(gameRules.ABLY_ionMissileMaxLifetime, 1 / cooldownDelta);

		missile.SetEffectActive(true);
		missileActive = true;

		// Rotate at a speed to just barely reach the correct orientation above the target position
		//RS = Mathf.Min(2 * (90 - initAngle) / ((targetPosition - targetUnit.transform.position).magnitude / MS), maxRS);
		RS = maxRS;
	}

	new void Update()
	{
		base.Update();

		// Targeting
		if (state == 1)
		{
			if (Time.frameCount > attemptShotWhen) // Should be checking for aim
			{
				if (targetUnit) // We have something to aim at
				{
					CalculateTargetPosition();
					if (InRange(targetUnit.transform)) // In range
					{
						if (Vector3.Dot(transform.forward, (targetPosition - transform.position).normalized) > 1 - aimThreshold) // Aimed close enoughs
						{
							stacks--;
							DisplayStacks();

							// Start countdown once we are properly aimed
							SpawnMissile();
							StartCoroutine(ClearAbilityGoalCoroutine());

							StartCooldown();
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
			} // state
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
							unit.AddStatus(new Status(parentUnit.gameObject, StatusType.IonSuppressed));

							float curShields = unit.GetShields().x;
							float maxShields = unit.GetShields().y;
							DamageResult result = unit.Damage(gameRules.ABLY_ionMissileDamage + gameRules.ABLY_ionMissileDamageBonusMult * curShields + gameRules.ABLY_ionMissileDamageBonusMult * maxShields, (startPosition.position - hit.point).magnitude, DamageType.IonMissile);

							// Is the unit still alive?
							if (!result.lastHit)
							{
								// Don't add ions if shields are up
								if (curShields <= 0)
								{
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
				if ((startPosition.position - missile.transform.position).sqrMagnitude > gameRules.ABLY_ionMissileRangeMissile * gameRules.ABLY_ionMissileRangeMissile || missileLifetime <= 0)
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
