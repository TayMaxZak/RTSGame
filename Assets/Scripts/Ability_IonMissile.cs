using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_IonMissile : Ability
{
	[SerializeField]
	private Transform startPosition;
	//[SerializeField]
	//private float initAngle = 45;

	[SerializeField]
	private float maxRS = 45; // Turning speed of missile
	private float RS;
	[SerializeField]
	private float MS = 8; // Movement speed of missile

	[SerializeField]
	private Effect_Point missilePrefab;
	private Effect_Point missile;
	private float missileLifetime;
	private bool missileActive = false;

	[SerializeField]
	private Effect_Point explosionPrefab;
	private Effect_Point explosion;

	private Vector3 targetPosition;
	private Unit targetUnit;
	private bool checkingForDead = false;

	private LayerMask mask;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.IonMissile;
		InitCooldown();

		mask = gameRules.collisionLayerMask;
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

	void CalculateTargetPosition()
	{
		targetPosition = targetUnit.transform.position + targetUnit.GetVelocity();
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (target.unit.team == team)
		{
			ResetCooldown();
			return;
		}

		// While a missile is already active, a new target for the current missile can be selected
		if (missileActive)
		{
			//if (InRange(target.unit.transform))
			//{
				//targetUnit = target.unit;
			//}
		}
		else // New missile
		{
			if (!offCooldown)
				return;

			base.UseAbility(target);

			if (InRange(target.unit.transform))
			{
				targetUnit = target.unit;
				SpawnMissile();
			}
			else
				ResetCooldown();
		}
	}

	void SpawnMissile()
	{
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

		if (targetUnit)
		{
			CalculateTargetPosition();
		}
		else
		{
			if (checkingForDead)
			{
				ClearTarget();
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
							Status status = new Status(parentUnit.gameObject, StatusType.CriticalBurnImmune);
							if (status != null)
							{
								unit.AddStatus(status);
							}

							float curShields = unit.GetShields().x;
							DamageResult result = unit.Damage(gameRules.ABLY_ionMissileDamage + gameRules.ABLY_ionMissileDamageBonusMult * curShields, (startPosition.position - hit.point).magnitude, DamageType.Ion);

							if (!result.lastHit)
							{
								if (unit.GetIons() <= gameRules.ABLY_ionMissileDecayCutoff)
									unit.AddIons(gameRules.ABLY_ionMissileIonsFirst, true);
								else
									unit.AddIons(gameRules.ABLY_ionMissileIonsNext, true);
							}
						}
						else
						{
							// Ignore this collision
							hitSelf = true;
						}
					}

					if (!hitSelf)
						Explode(true);
				}
				else
					Explode(true);
			}

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
	}

	void ClearTarget()
	{
		targetUnit = null;
		checkingForDead = false;
	}

	void Explode(bool intentional)
	{
		missile.SetEffectActive(false);
		missileActive = false;

		//Ability_ionMissile_Cloud cloud = Instantiate(damageCloud, missile.transform.position, Quaternion.identity);
		//cloud.SetParentUnit(parentUnit);

		explosion.transform.position = missile.transform.position;
		explosion.SetEffectActive(true);

		ClearTarget();
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
