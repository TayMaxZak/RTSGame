using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_StatusMissile : Ability
{
	[SerializeField]
	private Transform startPosition;
	[SerializeField]
	private float initAngle = 45;

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

	[SerializeField]
	private Ability_StatusMissile_Cloud damageCloud;

	private Vector3 targetPosition;
	private Unit targetUnit;
	private bool checkingForDead = false;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.StatusMissile;
		InitCooldown();
	}

	// Use this for initialization
	new void Start()
	{
		base.Start();

		startPosition.localRotation = Quaternion.Euler(-initAngle, 0, 0);

		missile = Instantiate(missilePrefab);
		missile.SetEffectActive(false);

		explosion = Instantiate(explosionPrefab);
		explosion.SetEffectActive(false);
	}

	void CalculateTargetPosition()
	{
		targetPosition = targetUnit.GetSwarmTarget().position + targetUnit.GetVelocity() + Vector3.up * gameRules.ABLY_statusMissileVerticalOffset;
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (suspended)
			return;

		if (target.unit.Team == team)
			return;

		// TODO: TEST ALL POSSIBLE COOLDOWN RESETS
		//if (target.unit.team == team)
		//{
		//	ResetCooldown();
		//	return;
		//}

		// While a missile is already active, a new target for the current missile can be selected
		if (missileActive)
		{
			if (InRange(target.unit.transform))
			{
				targetUnit = target.unit;
			}
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
		missileLifetime = Mathf.Min(gameRules.ABLY_statusMissileMaxLifetime, 1 / cooldownDelta);

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
			// Close enough to target position, too far from parent unit, or out of lifetime
			if ((targetPosition - missile.transform.position).sqrMagnitude < gameRules.ABLY_statusMissileExplodeDist * gameRules.ABLY_statusMissileExplodeDist)
			{
				Explode(true);
			}
			else if ((startPosition.position - missile.transform.position).sqrMagnitude > gameRules.ABLY_statusMissileRangeMissile * gameRules.ABLY_statusMissileRangeMissile || missileLifetime <= 0)
			{
				Explode(false);
			}
			else
			{
				missileLifetime -= Time.deltaTime;

				missile.transform.rotation = Quaternion.RotateTowards(missile.transform.rotation, Quaternion.LookRotation(targetPosition - missile.transform.position), RS * Time.deltaTime);
				missile.transform.position += missile.transform.forward * MS * Time.deltaTime;
			}
		}
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

		Ability_StatusMissile_Cloud cloud = Instantiate(damageCloud, missile.transform.position, Quaternion.identity);
		cloud.SetParentUnit(parentUnit);

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
		if (Vector3.SqrMagnitude(tran.position - transform.position) < gameRules.ABLY_statusMissileRangeUse * gameRules.ABLY_statusMissileRangeUse)
			return true;
		else
			return false;
	}
}
