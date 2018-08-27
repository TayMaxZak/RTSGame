using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnitMovement
{
	private Transform transform;
	private Unit parentUnit;
	private Unit target;

	[Header("Moving")]
	[SerializeField]
	private float MS = 7;
	[SerializeField]
	private float MSVMult = 0.4f;

	//private float targetMSRatio = 0;
	[SerializeField]
	private float MSAccel = 1; // Time in seconds to reach full speed
	[SerializeField]
	private float MSDeccel = 2; // Time in seconds to reach full stop
	private Vector3 velocity;

	private float curHMSRatio = 0;
	private float curVMSRatio = 0;

	[Header("Turning")]
	[SerializeField]
	private Transform model;
	private Quaternion lookRotation;
	private Vector3 direction;
	[SerializeField]
	private float RS = 90;
	[SerializeField]
	private float RSAccel = 1;
	private float curRSRatio = 0;
	[SerializeField]
	private float allowMoveThresh = 0.1f; // How early during a turn can we start moving forward

	//private float targetRSRatio = 0;
	//[SerializeField]
	//private float bankAngle = 30;

	[Header("Pathing")]
	[SerializeField]
	private float reachGoalThresh = 1; // How close to the goal position is close enough?
	private List<Vector3> hGoals;
	private Vector3 hGoalCur;
	private int vGoalCur;


	//private AbilityTarget rotationGoal; // Set by movement inputs. If not null, forces the unit to face towards a different goal than the one it wants to path to
	private AbilityTarget manualRotationGoal; // Set by movement inputs. If not null, forces the unit to face towards a different goal than the one it wants to path to
	private AbilityTarget abilityGoal; // Set by movement inputs. If not null, forces the unit to face towards a different goal than the one it wants to path to
	private bool reachedHGoal = false;
	private bool reachedVGoal = false;

	//protected Manager_Game gameManager;
	protected GameRules gameRules;
	private Manager_Pathfinding pathManager;

	// Use this for initialization
	public void Init(Unit parent)
	{
		hGoals = new List<Vector3>();

		parentUnit = parent;
		transform = parentUnit.transform;

		reachedHGoal = true;
		reachedVGoal = true;

		hGoalCur = transform.position; // Path towards current location (i.e. nowhere)
		vGoalCur = Mathf.RoundToInt(transform.position.y);

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules

		curRSRatio = 0;
	}

	//public void SetVCurrent(int cur)
	//{
	//	vCurrent = cur;
	//}

	// Banking
	//float bank = bankAngle * -Vector3.Dot(transform.right, direction);
	//banker.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.forward);

	// Update is called once per frame
	public void Tick()
	{
		//transform.Rotate(0, 180 * Time.deltaTime, 0);

		UpdateMovement();
	}

	void UpdateMovement()
	{
		Vector3 dif = hGoalCur - transform.position;

		int useRotationGoal = 0;
		if (abilityGoal != null)
			useRotationGoal = 1;
		else if (manualRotationGoal != null)
			useRotationGoal = 2;

		Vector3 dir = dif;
		if (abilityGoal != null)
			dir = abilityGoal.UnitOrPos() ? abilityGoal.unit.transform.position - transform.position : abilityGoal.position - transform.position;
		else if (manualRotationGoal != null)
			dir = manualRotationGoal.position - transform.position;

		// Rotate
		float leftOrRight = UpdateRotation(dir.normalized, useRotationGoal > 0); // Point towards movement goal or a rotation goal
		// If we are facing the goal after rotating, "move" towards it, saving velocity for actual position change later
		Vector3 hVel = UpdatePositionH(leftOrRight, dir.normalized);
		// "Move" vertically, independent from all the other movement so far, saving velocity for actual position change later
		Vector3 vVel = UpdatePositionV();
		// Pass in current speed so CalcChainVel knows if unit's movement speed is less than or greater than the speed from Chain
		Vector3 ourVel = (hVel + vVel) * CalcStatusSpeedMult();
		Vector4 chainVel = CalcChainVel(ourVel.magnitude);
		// Apply the maximum speed, as determined in CalcChainVel, as an upper limit to velocity magnitude
		velocity = Vector3.ClampMagnitude(ourVel + new Vector3(chainVel.x, chainVel.y, chainVel.z), chainVel.w);
		// Finally apply velocity to unit's position
		transform.position += velocity * Time.deltaTime;
	}

	float UpdateRotation(Vector3 dir, bool ignoreHGoal)
	{
		float RdirectionOrg = AngleDir(transform.forward, dir, Vector3.up);
		
		float stopTurningThresh = 0.0001f;

		if (ignoreHGoal || !reachedHGoal)
		{
			if (RdirectionOrg > stopTurningThresh)
			{
				CurRS(1);
			}
			else if (RdirectionOrg < -stopTurningThresh)
			{
				CurRS(-1);
			}
			else
			{
				CurRS(0);
			}
		}
		else
		{
			CurRS(0);
		}

		//Quaternion origRotate = transform.rotation;
		float speed = abilityGoal == null ? RS : RS * gameRules.MOVabilityAimingRSMult;

		transform.Rotate(0, speed * curRSRatio * Time.deltaTime, 0);

		return RdirectionOrg;
	}

	// Credit for JS code to HigherScriptingAuthority and cerebreate on Unity Forums
	// Returns -1 when to the left, 1 to the right, and 0 for forward/backward
	float AngleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
	{
		Vector3 perp = Vector3.Cross(fwd, targetDir);
		float dir = Vector3.Dot(perp, up);

		//dir = Mathf.Clamp(dir * deltaBias, -1, 1);
		return dir;
	}

	bool FrontOrBack(Vector3 fwd, Vector3 targetDir)
	{
		//Vector3 perp = Vector3.Cross(fwd, targetDir);
		float dir = Vector3.Dot(fwd, targetDir);

		//dir = Mathf.Clamp(dir * deltaBias, -1, 1);
		return dir > 0;
	}

	void CurRS(int targetRatio)
	{
		float deltaMult = abilityGoal == null ? 1 / RSAccel : 99;

		if (targetRatio < 0)
			curRSRatio = Mathf.Clamp(curRSRatio - Time.deltaTime * deltaMult, -1, 0);
		else if (targetRatio > 0)
			curRSRatio = Mathf.Clamp(curRSRatio + Time.deltaTime * deltaMult, 0, 1);
		else // Zero
		{
			if (curRSRatio < 0)
				curRSRatio = Mathf.Clamp(curRSRatio + Time.deltaTime * deltaMult, -1, 0);
			else if (curRSRatio > 0)
				curRSRatio = Mathf.Clamp(curRSRatio - Time.deltaTime * deltaMult, 0, 1);
		}
	}

	Vector3 UpdatePositionH(float leftOrRight, Vector3 dir)
	{
		if (Vector3.SqrMagnitude(transform.position - new Vector3(hGoalCur.x, transform.position.y, hGoalCur.z)) < reachGoalThresh * reachGoalThresh)
			ReachHGoal();

		bool inFront = FrontOrBack(transform.forward, dir);

		if (!reachedHGoal && Mathf.Abs(leftOrRight) < allowMoveThresh && inFront)
		{
			CurHMS(1);
		}
		else
		{
			CurHMS(0);
		}

		return MS * curHMSRatio * transform.forward;
	}

	void CurHMS(int targetRatio)
	{
		if (targetRatio > 0)
			curHMSRatio = Mathf.Clamp(curHMSRatio + Time.deltaTime * (1 / MSAccel), 0, 1);
		else
			curHMSRatio = Mathf.Clamp(curHMSRatio - Time.deltaTime * (1 / MSDeccel), 0, 1);
	}

	Vector3 UpdatePositionV()
	{
		if (Mathf.Abs(vGoalCur - transform.position.y) < reachGoalThresh * MSVMult)
		{
			reachedVGoal = true;
		}

		float aboveOrBelow = vGoalCur - transform.position.y; // > 0 if goal is above, < 0 if goal is below

		if (!reachedVGoal)
		{
			if (aboveOrBelow > 0)
				CurVMS(1);
			else
				CurVMS(-1);
		}
		else
		{
			CurVMS(0);
		}

		return MS * MSVMult * curVMSRatio * Vector3.up;
	}

	void CurVMS(int targetRatio)
	{
		float deltaMult = 1 / MSAccel;

		if (targetRatio > 0)
			curVMSRatio = Mathf.Clamp(curVMSRatio + Time.deltaTime * deltaMult, -1, 1);
		else if (targetRatio < 0)
			curVMSRatio = Mathf.Clamp(curVMSRatio - Time.deltaTime * deltaMult, -1, 1);
		else
		{
			if (curVMSRatio < 0)
				curVMSRatio = Mathf.Clamp(curVMSRatio + Time.deltaTime * deltaMult, -1, 0);
			else if (curVMSRatio > 0)
				curVMSRatio = Mathf.Clamp(curVMSRatio - Time.deltaTime * deltaMult, 0, 1);
		}
	}

	Vector4 CalcChainVel(float currentSpeed)
	{
		List<VelocityMod> velocityMods = parentUnit.GetVelocityMods();
		Vector3 total = Vector3.zero;
		float maxMagnitude = currentSpeed;

		for (int i = 0; i < velocityMods.Count; i++)
		{
			if (velocityMods[i].from == null)
			{
				velocityMods.RemoveAt(i);
				i--;
				continue;
			}

			if (velocityMods[i].vel.magnitude > maxMagnitude)
				maxMagnitude = velocityMods[i].vel.magnitude;

			float dot = Vector3.Dot((velocityMods[i].from.transform.position - transform.position).normalized, velocityMods[i].vel.normalized);
			dot = Mathf.Clamp(dot, 0, Mathf.Infinity);

			float curMult = dot;

			if (velocityMods[i].from.team == parentUnit.team)
			{
				curMult *= gameRules.ABLYchainAllyMult;
			}
			else
			{
				curMult *= gameRules.ABLYchainEnemyMult;
			}

			// TODO: Apply this type of logic to all instances checking if something is a flagship
			if (parentUnit.Type == EntityType.Flagship)
				curMult *= gameRules.ABLYchainFlagshipMult;

			total += velocityMods[i].vel * curMult;
		}

		if (velocityMods.Count > 0)
		{
			//reachedHGoal = false;
			//reachedVGoal = false;
		}

		return new Vector4(total.x, total.y, total.z, maxMagnitude);
	}

	float CalcStatusSpeedMult()
	{
		float statusSpeedMult = 1;
		foreach (Status s in parentUnit.GetStatuses()) // TODO: Optimize
		{
			if (s.statusType == StatusType.SpawnSwarmSpeedNerf)
				statusSpeedMult = gameRules.ABLYswarmFirstUseSpeedMult;
			else if (s.statusType == StatusType.SelfDestructSpeedBuff)
				statusSpeedMult = gameRules.ABLY_selfDestructSpeedMult;
		}
		return statusSpeedMult;
	}

	public void OnPathFound(Vector3[] newPath, bool pathFound)
	{
		if (pathFound) // Only reset pathing if a path was found
		{
			hGoals.Clear(); // Replace current waypoints
			foreach (Vector3 point in newPath) // Add new waypoints
				hGoals.Add(point);
			NewHGoal(); // Start pathing
		}
	}

	public void SetHGoal(Vector3 newHGoal, bool group)
	{
		if (abilityGoal != null) // Can't move while an ability is rotating us
			return;

		float selCircleRadius = parentUnit.GetSelCircleSize() * 1.85f; // Approximation of visual area inside selection circle graphic

		if (Vector3.SqrMagnitude(new Vector3(newHGoal.x - transform.position.x, newHGoal.z - transform.position.z)) > selCircleRadius * selCircleRadius)
		{
			PathRequestHandler.RequestPath(new PathRequest(transform.position, newHGoal, OnPathFound));

			manualRotationGoal = null; // Clear any current rotation goal
		}
		else // Not grouped or clicked outside of selection circle
		{
			if (!reachedHGoal) // Only able to rotate while stationary
			{
				Stop();
			}
			if (!group)
				manualRotationGoal = new AbilityTarget(newHGoal);
		}
		// else to do if rotation order but grouped
	}

	void ReachHGoal()
	{
		if (!reachedHGoal) // Not marked as "reached" yet
		{
			reachedHGoal = true;
			hGoals.RemoveAt(0);
			NewHGoal();
		}
	}

	void NewHGoal()
	{
		if (hGoals.Count > 0)
		{
			reachedHGoal = false;
			hGoalCur = hGoals[0];
		}
		else
			reachedHGoal = true;
	}

	public void SetVGoal(int newVGoal)
	{
		vGoalCur = newVGoal;
		reachedVGoal = false;
	}

	public Vector3 GetVelocity()
	{
		return velocity;
	}

	public void SetAbilityGoal(AbilityTarget newGoal)
	{
		abilityGoal = newGoal;
		Stop();
	}

	public void ClearAbilityGoal()
	{
		abilityGoal = null;
	}

	public float GetRotationSpeed()
	{
		return RS;
	}

	public void Stop()
	{
		hGoalCur = transform.position;
		reachedHGoal = true;

		manualRotationGoal = null;
	}

	public void OnDrawGizmos()
	{
		if (hGoals != null && hGoals.Count > 0)
		{
			for (int i = 0; i < hGoals.Count; i++)
			{
				Gizmos.color = Color.white;
				Gizmos.DrawCube(hGoals[i], Vector3.one);

				if (i == 0)
					Gizmos.DrawLine(transform.position, hGoals[i]);
				else
					Gizmos.DrawLine(hGoals[i-1], hGoals[i]);
			}
		}
	}
}
