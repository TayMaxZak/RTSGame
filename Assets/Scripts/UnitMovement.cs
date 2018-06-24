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
	[SerializeField]
	private float stopRotateThresh = 0.3f; // How early during a turn can we start moving forward

	//private float targetRSRatio = 0;
	//[SerializeField]
	//private float bankAngle = 30;

	[Header("Pathing")]
	
	[SerializeField]
	private float reachGoalThresh = 1; // How close to the goal position is close enough?

	private float deltaBias = 99999;
	private Vector3 goal;
	private bool reachedHGoal = false;
	private bool reachedVGoal = false;



	//protected Manager_Game gameManager;
	protected GameRules gameRules;

	// Use this for initialization
	public void Init(Unit parent)
	{
		parentUnit = parent;
		transform = parentUnit.transform;

		goal = parentUnit.transform.position; // Path towards current location (i.e. nowhere)

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules

		curRSRatio = 0;
	}

	// Banking
	//float bank = bankAngle * -Vector3.Dot(transform.right, direction);
	//banker.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.forward);

	// Update is called once per frame
	public void Tick()
	{
		/*
		transform.Rotate(0, 180 * Time.deltaTime, 0);
		*/

		UpdateMovement();
	}

	void UpdateMovement()
	{
		Vector3 dif = goal - transform.position;

		// Rotate
		float leftOrRight  = UpdateRotation(dif.normalized);
		// If we are facing the goal after rotating, "move" towards it, saving velocity for actual position change later
		Vector3 hVel = UpdatePositionH(leftOrRight);
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

	// TODO: Use Quaternion.Angle to measure angle differences
	float UpdateRotation(Vector3 dir)
	{
		float RdirectionOrg = AngleDir(transform.forward, dir, Vector3.up);
		float Rdirection = Mathf.Clamp(RdirectionOrg * deltaBias, -1, 1);

		if (!reachedHGoal)
		{
			if (Rdirection > 0)
				CurRS(1);
			else if (Rdirection < 0)
				CurRS(-1);
			else
				CurRS(0);
		}
		else
			CurRS(0);

		//Quaternion origRotate = transform.rotation;

		transform.Rotate(0, RS * curRSRatio * Time.deltaTime, 0);

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

	void CurRS(int targetRatio)
	{
		float deltaMult = 1 / RSAccel;

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

	Vector3 UpdatePositionH(float leftOrRight)
	{
		if (Vector3.SqrMagnitude(transform.position - new Vector3(goal.x, transform.position.y, goal.z)) < reachGoalThresh * reachGoalThresh)
			reachedHGoal = true;

		if (!reachedHGoal && Mathf.Abs(leftOrRight) < allowMoveThresh)
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
		float deltaMult = 1 / MSAccel;

		if (targetRatio > 0)
			curHMSRatio = Mathf.Clamp(curHMSRatio + Time.deltaTime * deltaMult, 0, 1);
		else
			curHMSRatio = Mathf.Clamp(curHMSRatio - Time.deltaTime * deltaMult, 0, 1);
	}

	Vector3 UpdatePositionV()
	{
		if (Mathf.Abs(goal.y - transform.position.y) < reachGoalThresh * MSVMult)
			reachedVGoal = true;

		float aboveOrBelow = goal.y - transform.position.y; // > 0 if goal is above, < 0 if goal is below

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
			if (velocityMods[i].from.team == parentUnit.team)
			{
				total += velocityMods[i].vel * gameRules.ABLYchainAllyMult * dot;
			}
			else
			{
				total += velocityMods[i].vel * gameRules.ABLYchainEnemyMult * dot;
			}
		}

		if (velocityMods.Count > 0)
		{
			Debug.Log("yoink");
			reachedHGoal = false;
			reachedVGoal = false;
		}

		return new Vector4(total.x, total.y, total.z, maxMagnitude);
	}

	float CalcStatusSpeedMult()
	{
		float statusSpeedMult = 1;
		foreach (Status s in parentUnit.GetStatuses()) // TODO: Optimize
			if (s.statusType == StatusType.SpawnSwarmSpeedNerf)
				statusSpeedMult = gameRules.ABLYswarmFirstUseSpeedMult;
		return statusSpeedMult;
	}


	public void OrderMove(Vector3 newGoal)
	{
		goal = newGoal;
		reachedHGoal = false;
		reachedVGoal = false;
	}
	
	public Vector3 GetVelocity()
	{
		return velocity;
	}

	/*
	void UpdateMovementOld()
	{
		
		Vector3 dif = goal - transform.position;

		if (dif.magnitude <= reachGoalThresh)
			isPathing = false;
		else
			isPathing = true;

		// Rotation
		float targetRSRatio = isPathing ? 1 : 0;

		//float RSdelta = Mathf.Sign(targetRSRatio - curRSRatio) * (1f / RSAccel) * Time.deltaTime;

		float RSdelta = Mathf.Clamp((targetRSRatio - curRSRatio) * deltaBias, -1, 1) * (1f / RSAccel) * Time.deltaTime;
		curRSRatio = Mathf.Clamp01(curRSRatio + RSdelta);

		direction = dif.normalized;
		lookRotation = Quaternion.LookRotation(direction == Vector3.zero ? transform.forward : direction);
		//float oldRotY = transform.eulerAngles.y;
		//transform.Rotate(new Vector3(0, RS * Time.deltaTime, 0));
		transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * RS * curRSRatio);
		//float difRot = transform.eulerAngles.y - oldRotY;

		//float oldBank = model.localEulerAngles.z;
		//float newBank = bankAngle * (difRot / (Time.deltaTime * RS)) * curRSRatio;
		//float diffBank = ((oldBank - newBank + 180 + 360) % 360) - 180;
		//float targetBank = (360 + newBank + (diffBank / 2)) % 360;

		//model.rotation = Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, targetBank));
		model.rotation = Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, 0));



		// Horizontal Movement
		float dot = Mathf.Max((Vector3.Dot(direction, transform.forward) - (1 - allowMoveThresh)) / (allowMoveThresh), 0);
		if (dot >= 0.9999f)
			dot = 1;

		float statusSpeedMult = 1;
		foreach (Status s in parentUnit.GetStatuses()) // TODO: Optimize
			if (s.statusType == StatusType.SpawnSwarmSpeedNerf)
				statusSpeedMult = gameRules.ABLYswarmFirstUseSpeedMult;

		float targetMSRatio = dot * (isPathing ? statusSpeedMult : 0);

		//float MSccel = (Mathf.Sign(targetMSRatio - curMSRatio) > 0) ? MSAccel : MSDeccel;
		//float MSdelta = Mathf.Sign(targetMSRatio - curMSRatio) * (1f / MSccel) * Time.deltaTime;

		float MSccel = (Mathf.Sign(targetMSRatio - curMSRatio) > 0) ? MSAccel : MSDeccel; // MSccel is not a typo
		float MSdelta = Mathf.Clamp((targetMSRatio - curMSRatio) * deltaBias, -1, 1) * (1f / MSccel) * Time.deltaTime;
		curMSRatio = Mathf.Clamp01(curMSRatio + MSdelta);

		Vector3 Hvel = MS * curMSRatio * new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
		//transform.position += Hvel * Time.deltaTime;

		// Vertical Movement
		float targetYMSRatio = (isPathing ? 1 : 0);

		//float YMSdelta = Mathf.Sign(targetYMSRatio - curYMSRatio) * (1f / MSAccel) * Time.deltaTime;
		float YMSdelta = Mathf.Clamp((targetYMSRatio - curVMSRatio) * deltaBias, -1, 1) * (1f / MSAccel) * Time.deltaTime;
		curVMSRatio = Mathf.Clamp01(curVMSRatio + YMSdelta);

		Vector3 Yvel = MS * curVMSRatio * Vector3.up * Mathf.Clamp(direction.y * 2, -1, 1) * MSVMult;
		Vector4 chainVel = CalcChainVel((Hvel + Yvel).magnitude);

		// Final velocity is combination of independent movement and velocity mods
		velocity = Vector3.ClampMagnitude((Yvel + Hvel) + new Vector3(chainVel.x, chainVel.y, chainVel.z), chainVel.w);
		transform.position += velocity * Time.deltaTime;
		
	}
	*/
}
