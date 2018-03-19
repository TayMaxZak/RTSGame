using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : Entity
{

	private Unit target;

	[Header("Health Pool")]
	[SerializeField]
	private float curArmor = 100;
	[SerializeField]
	private float maxArmor = 100;
	[SerializeField]
	private float curHealth = 100;
	[SerializeField]
	private float maxHealth = 100;
	[SerializeField]
	private GameObject deathClone; // Object to spawn on death
	private float curBurnCounter;

	[Header("Combat")]
	[SerializeField]
	private GameObject tempExplosion;
	private int tempCounter = 0;
	private bool isAttacking;

	[Header("Abilities")]
	[SerializeField]
	private List<Ability> abilities;

	[Header("Moving")]
	[SerializeField]
	private float MS = 7;
	private float curMSRatio = 0;
	[SerializeField]
	private float MSverticalMod = 2;
	private float curYMSRatio = 0;
	//private float targetMSRatio = 0;
	[SerializeField]
	private float MSAccel = 1; // Time in seconds to reach full speed
	[SerializeField]
	private float MSDeccel = 2; // Time in seconds to reach full stop

	[Header("Turning")]
	[SerializeField]
	private float RS = 90;
	[SerializeField]
	private float RSAccel = 1;
	private float curRSRatio = 0;
	//private float targetRSRatio = 0;
	[SerializeField]
	private float bankAngle = 30;
	[SerializeField]
	private Transform model;

	[Header("Pathing")]
	[SerializeField]
	private float reachGoalThresh = 1;
	[SerializeField]
	private float YMSResetThresh = 1;
	private bool isPathing;
	private Vector3 goal;

	private Quaternion lookRotation;
	private Vector3 direction;
	[SerializeField]
	private float allowMoveThresh = 0.1f;

	private GameRules gameRules;

	// Use this for initialization
	new void Start()
	{
		base.Start(); // Init Entity base class

		goal = transform.position; // Path towards current location (ie nowhere)

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules

		curHealth = maxHealth; // Reset HP values
		curArmor = maxArmor;


	}

	// Banking
	//float bank = bankAngle * -Vector3.Dot(transform.right, direction);
	//banker.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.forward);

	// Update is called once per frame
	void Update ()
	{
		Vector3 dif = goal - transform.position;
		
		if (dif.magnitude <= reachGoalThresh)
			isPathing = false;
		else
			isPathing = true;

		// Health
		bool isBurning = curHealth / maxHealth <= gameRules.HLTHthreshBurn;
		
		if (isBurning)
		{
			curBurnCounter += Time.deltaTime;
			if (curBurnCounter >= 1)
			{
				curBurnCounter = 0;
				TrueDamage(Mathf.RoundToInt(Random.Range(gameRules.HLTHburnMin, gameRules.HLTHburnMax)));
			}
			
		}

		// Rotation
		float targetRSRatio = isPathing ? 1 : 0;

		float RSdelta = Mathf.Sign(targetRSRatio - curRSRatio) * (1f / RSAccel) * Time.deltaTime;
		curRSRatio = Mathf.Clamp01(curRSRatio + RSdelta);

		direction = dif.normalized;
		lookRotation = Quaternion.LookRotation(direction);
		float oldRotY = transform.eulerAngles.y;
		transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * RS * curRSRatio);
		float difRot = transform.eulerAngles.y - oldRotY;

		float oldBank = model.localEulerAngles.z;
		float newBank = bankAngle * (difRot / (Time.deltaTime * RS)) * curRSRatio;
		float diffBank = ((oldBank - newBank + 180 + 360) % 360) - 180;
		float targetBank = (360 + newBank + (diffBank / 2)) % 360;

		//model.rotation = Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, targetBank));
		model.rotation = Quaternion.Euler(new Vector3(0, transform.eulerAngles.y, 0));
		


		// Horizontal Movement
		float dot = Mathf.Max((Vector3.Dot(direction, transform.forward) - (1 - allowMoveThresh)) / (allowMoveThresh), 0);
		if (dot >= 0.9999f)
			dot = 1;

		float targetMSRatio = dot * (isPathing ? 1 : 0);

		float MSccel = (Mathf.Sign(targetMSRatio - curMSRatio) > 0) ? MSAccel : MSDeccel;
		float MSdelta = Mathf.Sign(targetMSRatio - curMSRatio) * (1f / MSccel) * Time.deltaTime;
		curMSRatio = Mathf.Clamp01(curMSRatio + MSdelta);

		transform.position += MS * curMSRatio * new Vector3(transform.forward.x, 0, transform.forward.z) * Time.deltaTime;

		// Vertical Movement
		float targetYMSRatio = (isPathing ? 1 : 0);

		float YMSdelta = Mathf.Sign(targetYMSRatio - curYMSRatio) * (1f / MSAccel) * Time.deltaTime;
		curYMSRatio = Mathf.Clamp01(curYMSRatio + YMSdelta);

		transform.position += MS * curYMSRatio * Vector3.up * Mathf.Clamp(direction.y * 2, -1, 1) * MSverticalMod * Time.deltaTime;


		// Combat
		if (target)
			isAttacking = true;
		else
			isAttacking = false;

		if (isAttacking)
		{
			tempCounter++;
			if (tempCounter == 50)
			{
				Instantiate(tempExplosion, target.transform.position, Quaternion.identity);
				tempCounter = 0;
			}
		}
	}

	public void OrderMove(Vector3 newGoal)
	{
		Vector3 prevGoal = goal;
		goal = newGoal;
		float yDif = goal.y - prevGoal.y;

		if (Mathf.Abs(yDif) >= YMSResetThresh)
		{
			curYMSRatio = 0;
		}
	}

	public void OrderAttack(Unit newTarg)
	{
		target = newTarg;
		Debug.Log("I, " + DisplayName + ", am going after " + target.DisplayName);
	}

	public bool Damage(float damageBase, float range)
	{
		float dmg = damageBase;

		if (dmg - gameRules.ARMrangeResist * range <= curArmor) // Range resist condition: if this shot wont break the armor, it will be range resisted
			dmg = Mathf.Max(0, dmg - gameRules.ARMrangeResist * range); //Damage lost to falloff, incentivising shooting armor from up close
		else
			dmg = Mathf.Max(0, dmg); // Not enough armor left, no longer grants range resist

		if (dmg <= 0)
			return false;

		float absorbLim = Mathf.Min(curArmor, (curArmor / maxArmor) * gameRules.ARMabsorbMax + gameRules.ARMabsorbFlat); // Absorbtion limit formula

		float dmgToArmor = Mathf.Min(absorbLim, dmg); // How much damage armor takes

		float overflowDmg = Mathf.Max(0, dmg - absorbLim); // How much damage health takes (aka by how much damage exceeds absorbtion limit)

		//float critflowDmg = 0;
		float critflowDmg = Mathf.Max(0, overflowDmg - absorbLim); // By how much *overflow damage* exceeds absorbtion limit. 
		overflowDmg -= critflowDmg;

		// Setting cur values
		curArmor += -dmgToArmor;
		curHealth += Mathf.Min(curArmor /*ie armor is negative*/, 0) - overflowDmg - critflowDmg;
		curArmor += -critflowDmg; // Don't want critflow damage to wrap around and damage health twice, so we put this step after health step

		curArmor = Mathf.Max(curArmor, 0);
		HPLog();
		if (curHealth <= 0)
		{
			Die();
			return true;
		}

		return true;
	}

	public void TrueDamage(float damageBase) // Ignores armor
	{
		curHealth -= damageBase;
		HPLog();
		if (curHealth <= 0)
		{
			Die();
		}
		
	}

	public void Die()
	{
		Instantiate(deathClone, model.transform.position, model.rotation);
		Destroy(gameObject);
	}

	private void HPLog()
	{
		Debug.Log((int)(curHealth * 10) + " " + (int)(maxHealth * 10) + " :: " + (int)(curArmor * 10) + " " + (int)(maxArmor * 10));
	}
}
