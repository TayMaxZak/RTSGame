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

	private int bombsLeft;
	private float timeUntilNextBomb;


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

			//Boom();
			DropBombs();

			stacks--;
			DisplayStacks();
			//}

		}

		// While a missile is already active, a new target for the current missile can be selected
	}

	void DetonateBomb()
	{
		targetUnit.Damage((gameRules.ABLY_bombsDamage + gameRules.ABLY_bombsDamageBonusMult * targetUnit.GetHP().y), 0, DamageType.Bomb);

		Instantiate(explosionPrefab, targetUnit.transform.position + new Vector3(RandomValue(), 0.5f, RandomValue()), Quaternion.identity);

		bombsLeft--;

		// Last bomb dropped, reset everything
		if (bombsLeft <= 0)
		{
			ResetKeepTarget();
			ResetLockOn();
		}
	}

	void DropBombs()
	{
		bombsLeft = gameRules.ABLY_bombsCount;
		timeUntilNextBomb = gameRules.ABLY_bombsDropTime / gameRules.ABLY_bombsCount;
		Instantiate(bombPrefab, startPosition[startIndexCur].position, lockRayPosition.rotation);
	}

	new void Update()
	{
		base.Update();

		if (checkIfDead && targetUnit == null)
		{
			bombsLeft = 0;

			ResetKeepTarget();
			ResetLockOn();
		}

		if (bombsLeft > 0)
		{
			timeUntilNextBomb -= Time.deltaTime;
			if (timeUntilNextBomb <= 0)
			{
				timeUntilNextBomb = gameRules.ABLY_bombsDropTime / gameRules.ABLY_bombsCount;

				DetonateBomb();
			}
		}
		else
		{
			// Lose lock over time
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
					else if (i == 1)
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
									{
										newTargetUnit = unit;

										if (i == 0)
											raysConnected += 10;
										else
											raysConnected++;
									}
									else
									{
										if (unit == newTargetUnit)
										{
											if (i == 0)
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

				//if (newTargetUnit != null)
				SetTargetUnit(newTargetUnit);

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
		} // bombsLeft <= 0
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

	void SetTargetUnit(Unit newTargetUnit)
	{
		targetUnit = newTargetUnit;
		if (targetUnit != null)
			checkIfDead = true;
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

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
