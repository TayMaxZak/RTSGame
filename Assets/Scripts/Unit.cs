using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : Entity
{
	public bool printStatus = false;

	private Unit target;
	public int team = 0;
	[HideInInspector]
	public int buildUnitIndex = -1;

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
	protected float curShield = 0;
	protected float maxShield = 0;
	[SerializeField]
	private int shieldTeam = 0;
	[SerializeField]
	private GameObject deathClone; // Object to spawn on death
	private float curBurnCounter;
	private bool dead;

	[Header("Combat")]
	[SerializeField]
	private Turret[] turrets;

	[Header("Abilities")]
	[SerializeField]
	public List<Ability> abilities;

	[Header("Moving")]
	[SerializeField]
	private float MS = 7;
	private float curMSRatio = 0;
	[SerializeField]
	private float MSVerticalMod = 0.4f;
	private float curYMSRatio = 0;
	//private float targetMSRatio = 0;
	[SerializeField]
	private float MSAccel = 1; // Time in seconds to reach full speed
	[SerializeField]
	private float MSDeccel = 2; // Time in seconds to reach full stop
	private Vector3 velocity;

	[Header("Turning")]
	[SerializeField]
	private float RS = 90;
	[SerializeField]
	private float RSAccel = 1;
	private float curRSRatio = 0;
	//private float targetRSRatio = 0;
	//[SerializeField]
	//private float bankAngle = 30;
	[SerializeField]
	private Transform model;

	[Header("Pathing")]
	[SerializeField]
	private float reachGoalThresh = 1; // How close to the goal position is close enough?
	[SerializeField]
	private float YMSResetThresh = 1;
	private float deltaBias = 99999;
	private bool isPathing;
	private Vector3 goal;

	private Quaternion lookRotation;
	private Vector3 direction;
	[SerializeField]
	private float allowMoveThresh = 0.1f; // How early during a turn can we start moving forward

	private bool selected;

	private Manager_Game gameManager;
	protected GameRules gameRules;
	private UI_HPBar hpBar;
	private Vector2 HPBarOffset;

	private List<Status> statuses;
	private List<Unit> notifyOnDeath; // These units will be messaged when we die

	private List<Unit> damageTaken;

	//private List<Unit> killers; // Units that assisted in this unit's death TODO: IMPLEMENT

	// Use this for initialization
	protected new void Start()
	{
		base.Start(); // Init Entity base class

		goal = transform.position; // Path towards current location (i.e. nowhere)

		gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>(); // Find Game Manager
		gameRules = gameManager.GameRules; // Grab copy of Game Rules

		//curHealth = maxHealth; // Reset HP values
		//curArmor = maxArmor;
		maxShield = gameRules.ABLYshieldProjectMaxPool;

		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>(); // Grab copy of UI Manager
		hpBar = Instantiate(uiManager.UnitHPBar);
		hpBar.transform.SetParent(uiManager.Canvas.transform, false);
		HPBarOffset = uiManager.UIRules.HPBoffset;
		UpdateUI(); // Make sure healthbar is hidden until the unit is first selected

		foreach (Ability ab in abilities) // Init abilities
			ab.Init(this, gameRules);

		foreach (Turret tur in turrets) // Init turrets
			tur.team = team;

		statuses = new List<Status>();
		notifyOnDeath = new List<Unit>();
	}

	// Banking
	//float bank = bankAngle * -Vector3.Dot(transform.right, direction);
	//banker.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.forward);

	// Update is called once per frame
	protected new void Update ()
	{
		base.Update(); // Entity base class
		UpdateUI();
		UpdateMovement();
		UpdateAbilities();
		UpdateStatuses();

		// Health
		bool isBurning = curHealth / maxHealth <= gameRules.HLTHthreshBurn;
		
		if (isBurning)
		{
			curBurnCounter += Time.deltaTime;
			if (curBurnCounter >= 1)
			{
				curBurnCounter = 0;
				DamageSimple(Mathf.RoundToInt(Random.Range(gameRules.HLTHburnMin, gameRules.HLTHburnMax)), 0);
			}
		}
	}

	void UpdateUI()
	{
		if (!isSelected)
		{
			if (hpBar.gameObject.activeSelf)
				hpBar.gameObject.SetActive(false);
			return;
		}

		Vector3 barPosition = new Vector3(model.position.x + HPBarOffset.x, swarmTarget.position.y + HPBarOffset.y, model.position.z + HPBarOffset.x);
		Vector3 screenPoint = Camera.main.WorldToScreenPoint(barPosition);

		float dot = Vector3.Dot((barPosition - Camera.main.transform.position).normalized, Camera.main.transform.forward);
		if (dot < 0)
		{
			if (hpBar.gameObject.activeSelf)
				hpBar.gameObject.SetActive(false);
		}
		else
		{
			if (!hpBar.gameObject.activeSelf)
				hpBar.gameObject.SetActive(true);

			RectTransform rect = hpBar.GetComponent<RectTransform>();
			rect.position = new Vector2(screenPoint.x, screenPoint.y);
			//rect.localScale = ;
			UpdateHPBar();
		}
	}

	void UpdateHPBar()
	{
		hpBar.UpdateHPBar(curHealth / maxHealth, curArmor / maxArmor, curShield / maxShield);
	}

	void UpdateMovement()
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

		float targetMSRatio = dot * (isPathing ? 1 : 0);

		//float MSccel = (Mathf.Sign(targetMSRatio - curMSRatio) > 0) ? MSAccel : MSDeccel;
		//float MSdelta = Mathf.Sign(targetMSRatio - curMSRatio) * (1f / MSccel) * Time.deltaTime;

		float MSccel = (Mathf.Sign(targetMSRatio - curMSRatio) > 0) ? MSAccel : MSDeccel; // MSccel is not a typo
		float MSdelta = Mathf.Clamp((targetMSRatio - curMSRatio) * deltaBias, -1, 1) * (1f / MSccel) * Time.deltaTime;
		curMSRatio = Mathf.Clamp01(curMSRatio + MSdelta);

		Vector3 Hvel = MS * curMSRatio * new Vector3(transform.forward.x, 0, transform.forward.z);
		//transform.position += Hvel * Time.deltaTime;

		// Vertical Movement
		float targetYMSRatio = (isPathing ? 1 : 0);

		//float YMSdelta = Mathf.Sign(targetYMSRatio - curYMSRatio) * (1f / MSAccel) * Time.deltaTime;
		float YMSdelta = Mathf.Clamp((targetYMSRatio - curYMSRatio) * deltaBias, -1, 1) * (1f / MSAccel) * Time.deltaTime;
		curYMSRatio = Mathf.Clamp01(curYMSRatio + YMSdelta);

		Vector3 Yvel = MS * curYMSRatio * Vector3.up * Mathf.Clamp(direction.y * 2, -1, 1) * MSVerticalMod;
		velocity = Yvel + Hvel;
		transform.position += velocity * Time.deltaTime;
	}

	void UpdateAbilities()
	{
		for (int i = 0; i < abilities.Count; i++)
		{
			//if (abilities[i].isActive)
			abilities[i].AbilityTick();
		}
	}

	void UpdateStatuses()
	{
		string output = "Current statuses are: ";
		List<Status> toRemove = new List<Status>();

		foreach (Status s in statuses)
		{
			if (!s.UpdateTimeLeft(Time.deltaTime))
				toRemove.Add(s);

			output += s.statusType + " ";
		}

		foreach (Status s in toRemove)
		{
			statuses.Remove(s);
		}

		if (printStatus && statuses.Count > 0)
			Debug.Log(output);
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
		foreach (Turret tur in turrets)
			tur.SetTarget(target);
	}

	public void OrderAbility(int i, AbilityTarget targ)
	{
		abilities[i].Activate(targ);
	}

	public void OrderCommandWheel(int i, AbilityTarget targ)
	{
		if (i == 2)
			foreach (Turret tur in turrets)
				tur.SetTarget(null);
	}

	public float GetShieldPool()
	{
		return curShield;
	}

	public void AddStatus(Status status)
	{
		bool alreadyHave = false;
		foreach (Status s in statuses) // Check all current statuses
		{
			if (s.from != status.from)
				continue;
			if (s.statusType != status.statusType)
				continue;
			alreadyHave = true;
			s.RefreshTimeLeft(); // If we already have this instance of a status effect, refresh its timer
		}

		if (!alreadyHave) // Otherwise add it
			statuses.Add(status);
	}

	public bool RecieveShield(float amount, int projectorTeam)
	{
		if (curShield > 0)
			return false;

		shieldTeam = projectorTeam;
		curShield = amount;
		return true;
	}

	public void UpdateShield(float amount)
	{
		curShield = amount;
	}

	public void RemoveShield()
	{
		curShield = 0;
	}

	public bool Damage(float damageBase, float range) // TODO: How much additional information is necessary (i.e. team, source, projectile type, etc.)
	{
		float dmg = damageBase;

		dmg = StatusDamageMod(dmg); // Apply status modifiers to damage first

		if (dmg <= 0)
			return false;

		dmg = DamageShield(dmg); // Try to damage shield before damaging main health pool

		if (dmg <= 0)
			return false;

		//Damage lost to range resist / damage falloff, incentivising shooting armor from up close
		float rangeRatio = Mathf.Max(0, (range - gameRules.ARMrangeMin) / gameRules.ARMrangeMax);

		float rangeDamage = Mathf.Min(dmg * rangeRatio, dmg) * gameRules.ARMrangeMult;
		if (curArmor > Mathf.Max(0, dmg - rangeDamage)) // Range resist condition: if this shot wont break the armor, it will be range resisted
			dmg = Mathf.Max(0, dmg - rangeDamage);
		else
			dmg = Mathf.Max(0, dmg); // Not enough armor left, no longer grants range resist

		if (dmg <= 0)
			return false;

		float absorbLim = Mathf.Min(curArmor, maxArmor < Mathf.Epsilon ? 0 : (curArmor / maxArmor) * gameRules.ARMabsorbMax + gameRules.ARMabsorbFlat); // Absorbtion limit formula

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

		OnDamage();

		if (curHealth <= 0)
		{
			Die();
		}

		return true;
	}

	protected void OnDamage()
	{

	}

	public float DamageShield(float dmg)
	{
		if (shieldTeam == team)
		{
			float newShield = curShield - dmg;
			curShield = newShield;
			if (newShield < 0)
			{
				curShield = 0;
				return -newShield;
			}
			else
				return -newShield;
		}
		else
		{
			return dmg;
		}
	}

	private float StatusDamageMod(float dmgOrg)
	{
		float dmg = dmgOrg;
		int swarmShieldCounter = 0;

		foreach (Status s in statuses)
		{
			if (s.statusType == StatusType.SwarmShield)
				swarmShieldCounter++;
		}

		// Apply swarm shield damage reduction, which can stack a limited number of times
		dmg -= dmg * gameRules.STATswarmShieldDmgReduce * Mathf.Clamp(swarmShieldCounter, 0, gameRules.STATswarmShieldMaxStacks);
		return dmg;
	}

	public void DamageSimple(float healthDmg, float armorDmg) // Simple subtraction to armor and health
	{
		curArmor = Mathf.Clamp(curArmor - armorDmg, 0, maxArmor);

		curHealth = Mathf.Min(curHealth - healthDmg, maxHealth);
		if (curHealth <= 0)
		{
			Die();
		}
	}


	public void Die()
	{
		if (dead)
			return;
		dead = true; // Prevents multiple deaths

		foreach (Ability ab in abilities)
		{
			ab.End();
		}

		foreach (Unit un in notifyOnDeath)
		{
			un.DeathMessage(this);
		}

		if (deathClone)
			Instantiate(deathClone, model.transform.position, model.rotation);
		Destroy(hpBar.gameObject);
		Destroy(selCircle);
		Destroy(gameObject);

		//gameManager.Commanders[team].RefundUnit(this);
		gameManager.Commanders[0].RefundUnit(this); // TODO: While we have 1 commander
	}

	public void SubscribeToDeath(Unit u)
	{
		if (notifyOnDeath.Contains(u))
			return;
		else
			notifyOnDeath.Add(u);
	}

	public void DeathMessage(Unit u)
	{
		if (u.team != team)
		{
			// TODO: Hellrazor stack increase

		}
	}

	public Vector3 GetVelocity()
	{
		return velocity;
	}

	public Vector4 GetHP()
	{
		return new Vector4(curHealth, maxHealth, curArmor, maxArmor);
	}

	public Transform GetSwarmTarget()
	{
		return swarmTarget;
	}

	private void HPLog()
	{
		Debug.Log((int)(curHealth * 10) + " " + (int)(maxHealth * 10) + " :: " + (int)(curArmor * 10) + " " + (int)(maxArmor * 10));
	}
}
