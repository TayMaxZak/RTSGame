using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Turret : MonoBehaviour
{
	//public int team = 0;
	protected Unit parentUnit;
	private int turretId = -1;

	protected Status onHitStatus;

	// TODO: Let user-specified target override preferences
	[Header("Targeting")]
	[SerializeField]
	protected TargetType preferredTargetType = TargetType.Default;
	protected ITargetable target;
	private bool hasManualTarget = false;
	[SerializeField]
	private float range = 25;
	[SerializeField]
	private bool riskFFAgainstFighters = false;
	[SerializeField]
	private bool ignoreNonPreferrential = false;

	[Header("Sound")]
	[SerializeField]
	protected AudioClip soundShoot; // Played on each shot or, if there is an audio loop, when the loop ends

	[Header("Shooting")]
	[SerializeField]
	protected Transform firePos;
	protected int curAmmo = 4;
	[SerializeField]
	private int maxAmmo = 4;
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
	private bool isReloading;
	private bool isShooting;

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
	private int resetRotFrame;
	private Quaternion lookRotation;
	//private Quaternion forwardRotation;
	private Vector3 direction;

	[Header("Rotation Limits")]
	[SerializeField]
	private float maxH = 180;
	[SerializeField]
	private float minH = 180;
	[SerializeField]
	private float minV = 0;
	[SerializeField]
	private float maxV = 180;

	//[Header("State")]
	//[SerializeField]
	private bool infiniteAmmo = false;
	private bool suspended = false;

	private AudioSource audioSource;
	private GameRules gameRules;

	private Multiplayer_Manager multManager;
	//private NetworkIdentity ourId;

	// Use this for initialization
	protected void Awake()
	{
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules

		audioSource = GetComponent<AudioSource>();

		curAmmo = maxAmmo;

		shootCooldown = 1f / (rateOfFire / 60f);
		shootOffset = shootCooldown * shootOffsetRatio;

		rotation = Quaternion.LookRotation(transform.forward, Vector3.up);

		multManager = GameObject.FindGameObjectWithTag("MultiplayerManager").GetComponent<Multiplayer_Manager>(); // For multiplayer
	}

	public void SetParentUnit(Unit unit, int id)
	{
		parentUnit = unit;
		//team = parentUnit.team;
		turretId = id;
	}

	public void SetOnHitStatus(Status status)
	{
		onHitStatus = status;
	}

	// Update is called once per frame
	void Update()
	{
		// Only the server looks for targets every frame
		if (parentUnit.isServer)
		{
			UpdateTargeting();
		}
		else
		{ // Rotate appropriately based on target set by server
			if (!IsNull(target))
			{
				CalcTargetLookRotation();
				Rotate();
			}
			else
			{
				direction = transform.forward;
				lookRotation = CalcLookRotation(direction);
				Rotate();
			}
		}
	}

	void UpdateTargeting()
	{
		if (!suspended)
		{
			// Have target and it's valid
			if (!IsNull(target) && IsValid(target))
			{
				// If we are targeting a non-preferred target type, we want to constantly search for a better target
				if (!hasManualTarget && target.GetTargetType() != preferredTargetType)
				{
					// Collect a list of all valid targets, ignoring non-preferred targets
					List<ITargetable> autoTargets = ScanForTargets(true);

					// Pick best one (if any were found)
					if (autoTargets.Count > 0)
					{
						// This new target will ALWAYS be different from the previous target because it was filtered differently
						target = autoTargets[0];

						// Update resetRotFrame
						UpdateTarget(false);
					}
				}

				// Rotate towards the target
				CalcTargetLookRotation();
				Rotate();
				// Shoot if possible
				if (parentUnit.isServer) // Only server actually shoots, it's faked on clients
				{
					AttemptStartShooting();
				}
			}
			else // We haven't assigned one yet, it died, or it's become invalid
			{
				// Collect a list of all valid targets
				List<ITargetable> autoTargets = ScanForTargets(ignoreNonPreferrential);
				// Pick best one
				target = autoTargets.Count > 0 ? autoTargets[0] : null;

				// Found a valid target
				if (!IsNull(target))
				{
					Debug.Log("my parent team is " + parentUnit.GetTeam() + " targets team is " + target.GetTeam());
					// Update resetRotFrame
					UpdateTarget(false);

					// Rotate towards the target
					CalcTargetLookRotation();
					Rotate();
					// Shoot if possible
					if (parentUnit.isServer) // Only server actually shoots, it's faked on clients
					{
						AttemptStartShooting();
					}
				}
				else // Failed to find a valid target
				{
					// Rotate to standby position
					direction = transform.forward;
					lookRotation = CalcLookRotation(direction);
					Rotate();
				}
			} // if have target and it's valid
		} // if not suspended
		else
		{
			direction = transform.forward;
			lookRotation = CalcLookRotation(direction);
			Rotate();
		}
	}
	// ignoreNonPreferrential
	// In a sphere with the radius of range, find all enemy units and pick one to target
	List<ITargetable> ScanForTargets(bool ignoreNonPreferred)
	{
		Collider[] cols = Physics.OverlapSphere(transform.position, range, gameRules.targetLayerMask);
		List<ITargetable> targs = new List<ITargetable>();

		for (int i = 0; i < cols.Length; i++)
		{
			ITargetable targ = GetITargetableFromCol(cols[i]);

			if (IsNull(targ)) // No targetable found
				continue;

			if (targs.Contains(targ)) // Ignore multiple colliders for one targetable
				continue;

			if (targ.GetTeam() == parentUnit.GetTeam()) // Can't target allies
				continue;

			if (!targ.GetVisibleTo(parentUnit.GetTeam())) // Must be visible to this team
				continue;

			// Can ignore non-preferred targets altogether (used when searching for a better target)
			if (ignoreNonPreferred && targ.GetTargetType() != preferredTargetType)
				continue;

			// The list of colliders is created by intersection with a range-radius sphere, but the center of this unit can still actually be out of range, leading to a target which cannot be shot at
			if (!IsValid(targ))
				continue;

			targs.Add(targ);
		}

		// Sort by distance and target type
		targs.Sort(delegate (ITargetable a, ITargetable b)
		{
			return ComparisonWeight(a)
			.CompareTo(
			  ComparisonWeight(b));
		});

		return targs;
	}

	float ComparisonWeight(ITargetable x)
	{
		float distanceWeight = Vector3.Distance(transform.position, x.GetPosition()) / range; // distanceWeight is always 0 to 1
		int typeWeight = x.GetTargetType() == preferredTargetType ? 0 : 1; // Move weight out of 0 to 1 range if it is not our preferred type
		return distanceWeight + typeWeight; // Always targets the closest preferred target. Only targets a non-preferred target if a preferred target is not present.
	}

	bool IsValid(ITargetable potentialTarget)
	{
		// Valid distance
		float sqrDistance = !IsNull(potentialTarget) ? (potentialTarget.GetPosition() - parentUnit.transform.position).sqrMagnitude : 0; // Check distance between us and the target // TODO: Maybe check from the parent unit's position?
		// Valid direction
		Vector3 dir = !IsNull(potentialTarget) ? CalcAdjDirection(potentialTarget) : transform.forward; // direction used elsewhere to check if aimed at target or not

		bool valid = false;
		// Have target, its in range, the look rotation is within limits, it is visible to our team
		if (!IsNull(potentialTarget) && sqrDistance <= range * range && ValidRotationHorizontal(CalcLookRotation(dir)) && potentialTarget.GetVisibleTo(parentUnit.GetTeam()))
		{
			valid = true;
		}
		else
		{
			valid = false;
		}

		return valid;
	}

	bool ValidRotationHorizontal(Quaternion rot)
	{
		Vector3 components = rot.eulerAngles;
		Vector3 hComponentFwd = Quaternion.LookRotation(transform.forward).eulerAngles;
		Vector3 vComponentFwd = Quaternion.LookRotation(transform.up).eulerAngles;

		float angleH = Vector3.SignedAngle(transform.forward, rot * Vector3.forward, Vector3.up);

		// Horizontal extremes
		if (angleH > maxH)
		{
			return false;
			//components.y = hComponentFwd.y + maxH;
		}
		else if (angleH < -minH)
		{
			return false;
			//components.y = hComponentFwd.y - minH;
		}

		return true;
	}

	void CalcTargetLookRotation()
	{
		direction = !IsNull(target) ? CalcAdjDirection(target) : transform.forward; // direction used elsewhere to check if aimed at target or not
		lookRotation = CalcLookRotation(direction);
	}

	Quaternion CalcLookRotation(Vector3 dir)
	{
		return Quaternion.LookRotation(dir, Vector3.up);
	}

	void Rotate()
	{
		// Fixes strange RotateTowards bug
		Quaternion resetRot = rotation;

		// Rotate towards our target
		Quaternion newRotation = Quaternion.RotateTowards(rotation, lookRotation, Time.deltaTime * RS);

		// Fixes strange RotateTowards bug
		if (Time.frameCount == resetRotFrame)
			newRotation = resetRot;

		Quaternion limitRot = LimitVerticalRotation(newRotation, rotation);

		rotation = limitRot;

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

	bool CheckFriendlyFire()
	{
		if (IsNull(target))
			return false;

		// We know how many potential collisions we can have with the parent unit's colliders, so we will cast multiple rays in succession to skip past the collisions that we want to ignore
		// RaycastAll will not work in this case because it will pass through everything, rather than only passing through the parent unit's colliders and stopping on the first collision after them
		Collider[] cols = parentUnit.GetComponentsInChildren<Collider>();

		// First, try to raycast and hope we don't hit ourselves
		Vector3 forward = GetForward();
		RaycastHit hit;
		// If we are targeting a fighter and willing to aim at an ally unit hoping to hit an enemy fighter, we will check for FF in a shorter distance
		float checkDistance = (riskFFAgainstFighters && !target.HasCollision()) ? Vector3.Distance(firePos.position, target.GetPosition()) : range * gameRules.PRJ_friendlyFireCheckRangeMult;
		float offset = 0.02f; // How much we move in towards our first raycast hit location to make sure the next raycast is technically inside the collider we hit the first time around
		if (Physics.Raycast(firePos.position, forward, out hit, checkDistance, gameRules.collisionLayerMask))
		{
			if (parentUnit.printInfo)
				Debug.DrawLine(firePos.position, firePos.position + forward * checkDistance, Color.magenta, 2);
			// Is it a unit? This could be either self-detection or hitting a different unit.
			Transform parent = hit.collider.transform.parent;
			Unit unit = parent ? parent.GetComponent<Unit>() : null;
			if (unit)
			{
				if (unit.team == parentUnit.GetTeam())
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
							// Start where the last raycast left off, plus moved in a little bit to make sure we dont hit the same collider again
							if (Physics.Raycast(hit.point + forward * offset, forward, out hit, checkDistance, gameRules.collisionLayerMask))
							{
								if (parentUnit.printInfo)
									Debug.DrawLine(firePos.position, firePos.position + forward * checkDistance, Color.magenta, 2);

								parent = hit.collider.transform.parent;
								unit = parent ? parent.GetComponent<Unit>() : null;
								if (unit)
								{
									if (unit.team == parentUnit.GetTeam())
									{
										// If we hit a non-parent teammate, immediately return false.
										if (unit != parentUnit)
										{
											return false;
										}
										// If we hit the parent unit, the hit.point from this raycast will be used by the next raycast as a starting point
									} // teammate 2
									else // enemy
									{ // Hit an enemy before an ally unit, no reason to continue checking
										return true;
									}
								} // unit 2
								else
								{
									return false;
								}
							} // second raycast
						}
					} // parent
				} // teammate
				else // enemy
				{ // Hit an enemy before an ally unit, no reason to continue checking
					return true;
				}
			} // unit
			else
			{
				return false;
			}
		} // first raycast
		return true;
	}

	// TODO: Consolidate conditions which appear in both attempt shot and attempt start shooting e.g. aimed at target
	void AttemptStartShooting()
	{
		if (isShooting) // Don't start shooting if we are already shooting
			return;

		if (suspended)
			return;

		if (isReloading) // Don't start shooting if we are reloading
		{
			if (!isReloadCancellable)
				return;
			else
			{
				// Cancel reload
				CancelReload();
			}
		}

		// Are we pointed at the target?
		// TODO: Make threshold distance-based and not angle-based
		Vector3 forward = GetForward();
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

	// TODO: Looping fire audio does not work on clients!
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

	// Whenever you return from this method, first make sure isShooting is set to false so that next Update() the process can restart
	void AttemptShot(float delay)
	{
		if (suspended)
		{
			isShooting = false;
			ToggleFiringAudio(false); // Stop firing audio loop
			return;
		}

		if (curAmmo <= 0 && maxAmmo > 0 && !infiniteAmmo) // If we run out of ammo mid-shooting, start reload
		{
			isShooting = false;
			ToggleFiringAudio(false); // Stop firing audio loop

			if (!isReloading) // Reload
				Reload();
			return;
		}

		if (isReloading) // If we have ammo left and are in range, cancel the current reload
		{
			if (!isReloadCancellable)
			{
				// Don't need to set isShooting to false because we are reloading anyway
				// TODO: Do we need to toggle firing audio off here?
				ToggleFiringAudio(false); // Stop firing audio loop
				return;
			}
			else // Shot after manually clearing target
			{
				CancelReload();
			}
		}

		// Are we pointed at the target?
		// TODO: Make threshold distance-based and not angle-based
		Vector3 forward = GetForward();
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

	// TODO: Is this even a mechanic anymore?
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

	/// <summary>
	/// Takes into account how this turret type deals damage to optimize aiming.
	/// </summary>
	/// <returns></returns>
	protected virtual Vector3 CalcAdjDirection(ITargetable targ)
	{
		return (targ.GetPosition() - transform.position).normalized;
	}

	protected void PlayShootSound()
	{
		if (!audioSource.clip)
			AudioUtils.PlayClipAt(soundShoot, transform.position, audioSource);
	}

	protected Vector3 GetForward()
	{
		return baseRotatesOnY ? pivotX.forward : pivotY.forward;
	}

	protected Vector3 GetRight()
	{
		return baseRotatesOnY ? pivotX.right : pivotY.right;
	}

	protected Vector3 GetUp()
	{
		return baseRotatesOnY ? pivotX.up : pivotY.up;
	}

	protected virtual void Fire()
	{
		if (parentUnit.isServer)
		{
			curAmmo--;

			multManager.RpcFireTurret(parentUnit.GetComponent<NetworkIdentity>(), turretId);
		}
	}

	public void ClientFire()
	{
		if (parentUnit.isServer) // This is for clients only
			return;
		Fire(); // Pretend to fire turret, not worrying about reloading, ammo, or actually dealing damage
	}

	// Called by parentUnit to give this turret an idea of what to shoot at
	public void SetManualTarget(Unit newTarg)
	{
		if (!parentUnit.isServer) // Target can only be set on server
			return;
		target = newTarg;
		UpdateTarget(true);
	}

	public void Suspend()
	{
		suspended = true;
	}

	// TODO: Verify that the delay you see after unsuspending before the turret starts shooting is a reload
	public void UnSuspend()
	{
		suspended = false;
	}

	private void UpdateTarget(bool manual)
	{
		// TODO: What if target is intentionally null?
		if (parentUnit.isServer)
		{
			multManager.CmdSyncTarget(parentUnit.GetComponent<NetworkIdentity>(), turretId, target.GetGameObject().GetComponent<NetworkIdentity>(), manual);
		}
		resetRotFrame = Time.frameCount;

		if (target.GetGameObject() == parentUnit.GetGameObject())
		{
			Debug.Log("Reset target");
			manual = false;
			target = null;
		}
		hasManualTarget = manual;
		//Debug.Log("Turret aiming at " + (!IsNull(target) ? target.GetTargetType().ToString() : "null"));
	}

	public void ClientUpdateTarget(NetworkIdentity targetIdentity, bool manual)
	{
		if (!parentUnit)
		{
			Debug.LogWarning("Can't find parent unit " + Time.frameCount);
			return;
		}
		if (parentUnit.isServer) // This is for clients only
			return;
		ITargetable potentialTarget = GetITargetableFromNetworkIdentity(targetIdentity);
		target = potentialTarget;
		Debug.Log("Client turret aiming at " + (!IsNull(target) ? target.GetTargetType().ToString() : "null"));
		UpdateTarget(manual);
		
	}

	ITargetable GetITargetableFromCol(Collider col)
	{
		Entity ent = col.GetComponentInParent<Entity>();
		if (ent)
		{
			if (ent.GetType() == typeof(Unit) || ent.GetType().IsSubclassOf(typeof(Unit)))
			{
				return (Unit)ent;
			}
			else
				return null;
		}
		else
		{
			ITargetable it = col.GetComponent<FighterGroup>(); // TODO: Generalize for all non-unit targetables
			if (!IsNull(it))
				return it;
			else
				return null;
		}
	}

	ITargetable GetITargetableFromNetworkIdentity(NetworkIdentity identity)
	{
		Entity ent = identity.GetComponent<Entity>();
		if (ent)
		{
			if (ent.GetType() == typeof(Unit) || ent.GetType().IsSubclassOf(typeof(Unit)))
			{
				return (Unit)ent;
			}
			else
				return null;
		}
		else
		{
			ITargetable it = identity.GetComponent<FighterGroup>(); // TODO: Generalize for all non-unit targetables
			if (!IsNull(it))
				return it;
			else
				return null;
		}
	}

	void CancelReload()
	{
		StopCoroutine(reloadCoroutine);
		isReloadCancellable = false;
		isReloading = false;
	}

	protected bool IsNull(ITargetable t)
	{
		if ((MonoBehaviour)t == null)
			return true;
		else
			return false;
	}

	public void SetInfiniteAmmo(bool state)
	{
		infiniteAmmo = state; // Don't need to reload
		if (isReloading)
			CancelReload(); // Cancel current reload
	}

	// Visualize range of turrets in editor
	/*
	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.white;
		Gizmos.DrawWireSphere(transform.position, range);
	}
	*/
}


public interface ITargetable
{
	Vector3 GetPosition(); // Where to aim turret at
	int GetTeam(); // What team does this belong to
	bool HasCollision(); // Is a proper raycast check required to hit this target?
	TargetType GetTargetType();
	DamageResult Damage(float damageBase, float range, DamageType dmgType);
	bool GetVisibleTo(int team); // Can the given team see this target
	GameObject GetGameObject();
}

public enum TargetType
{
	Default,
	Fighter,
	Unit // TODO: Unit categories?
}