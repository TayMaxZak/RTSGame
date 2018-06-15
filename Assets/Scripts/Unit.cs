using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : Entity
{
	public bool printInfo = false;

	[Header("Feedback")]
	[SerializeField]
	private UI_HPBar hpBarPrefab;
	[SerializeField]
	private Effect_HP hpEffects;

	private Unit target;
	public int team = 0;
	[HideInInspector]
	public int buildIndex = -1;

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
	public bool alwaysBurnImmune = false;
	private bool isBurning = false;
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

	protected Manager_Game gameManager;
	protected GameRules gameRules;
	[System.NonSerialized]
	public UI_HPBar hpBar; // Should be accessible by ability scripts
	private Vector2 hpBarOffset;

	// State //
	private List<Status> statuses;
	private List<VelocityMod> velocityMods;
	private List<ShieldMod> shieldMods;
	private List<Unit> damageTaken; // Units that assisted in this unit's death

	private List<Unit> notifyOnDeath; // These units will be messaged when we die

	void Awake()
	{
		hpBar = Instantiate(hpBarPrefab);
	}

	// Use this for initialization
	protected new void Start()
	{
		base.Start(); // Init Entity base class
		statuses = new List<Status>();
		velocityMods = new List<VelocityMod>();
		shieldMods = new List<ShieldMod>();
		notifyOnDeath = new List<Unit>();

		goal = transform.position; // Path towards current location (i.e. nowhere)

		if (gameManager == null)
			gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>(); // Find Game Manager
		if (gameRules == null) // Subclass may have already set this field
			gameRules = gameManager.GameRules; // Grab copy of Game Rules

		if (gameRules.useTestValues)
		{
			curHealth = curHealth * gameRules.TESTinitHPMult + gameRules.TESTinitHPAdd;
			curArmor = curArmor * gameRules.TESTinitHPMult + gameRules.TESTinitHPAdd;
		}

		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>(); // Grab copy of UI Manager
		 //hpBar = Instantiate(hpBarPrefab);
		hpBar.transform.SetParent(uiManager.Canvas.transform, false);
		hpBarOffset = uiManager.UIRules.HPBoffset;
		UpdateHPBarPosAndVis(); // Make sure healthbar is hidden until the unit is first selected
		UpdateHPBarVal(true);

		//foreach (AbilityOld ab in abilities) // Init abilities
		//	ab.Init(this, gameRules);

		foreach (Turret tur in turrets) // Init turrets
			tur.team = team;
	}

	// Banking
	//float bank = bankAngle * -Vector3.Dot(transform.right, direction);
	//banker.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.forward);

	// Update is called once per frame
	protected new void Update ()
	{
		base.Update(); // Entity base class
		UpdateMovement();
		//UpdateAbilities(); // TODO: REMOVE
		UpdateStatuses();

		if (isSelected || isHovered)
			UpdateHPBarPosAndVis();

		// Burning
		if (!alwaysBurnImmune)
		{
			isBurning = curHealth / maxHealth <= gameRules.HLTHthreshBurn;

			foreach (Status s in statuses)
				if (s.statusType == StatusType.CriticalBurnImmune)
					isBurning = false;

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
	}

	void UpdateHealth()
	{
		UpdateHPBarVal(false);
		UpdateEffects();
	}

	void UpdateHPBarPosAndVis()
	{
		if (!isSelected && !isHovered)
		{
			if (hpBar.gameObject.activeSelf)
				hpBar.gameObject.SetActive(false);
			return;
		}

		Vector3 barPosition = new Vector3(model.position.x + hpBarOffset.x, swarmTarget.position.y + hpBarOffset.y, model.position.z + hpBarOffset.x);
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
		}
	}

	protected void UpdateHPBarVal(bool fastUpdate)
	{
		hpBar.SetHealthArmorShield(new Vector3(curHealth / maxHealth, curArmor / maxArmor, CalcShieldPoolCur() / CalcShieldPoolMax()), isBurning);
		if (fastUpdate)
			hpBar.FastUpdate();

		if (controller)
			controller.UpdateStatsHealth(this);
	}

	void UpdateEffects()
	{
		if (hpEffects)
		{
			hpEffects.UpdateHealthEffects(curHealth / maxHealth);
		}
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

		Vector3 Hvel = MS * curMSRatio * new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
		//transform.position += Hvel * Time.deltaTime;

		// Vertical Movement
		float targetYMSRatio = (isPathing ? 1 : 0);

		//float YMSdelta = Mathf.Sign(targetYMSRatio - curYMSRatio) * (1f / MSAccel) * Time.deltaTime;
		float YMSdelta = Mathf.Clamp((targetYMSRatio - curYMSRatio) * deltaBias, -1, 1) * (1f / MSAccel) * Time.deltaTime;
		curYMSRatio = Mathf.Clamp01(curYMSRatio + YMSdelta);

		Vector3 Yvel = MS * curYMSRatio * Vector3.up * Mathf.Clamp(direction.y * 2, -1, 1) * MSVerticalMod;
		Vector4 chainVel = CalcChainVel((Hvel + Yvel).magnitude);
		// Final velocity is combination of independent movement and velocity mods

		velocity = Vector3.ClampMagnitude(Yvel + Hvel + new Vector3(chainVel.x, chainVel.y, chainVel.z), chainVel.w);
		transform.position += velocity * Time.deltaTime;
	}

	Vector4 CalcChainVel(float currentSpeed)
	{
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
			if (velocityMods[i].from.team == team)
			{
				total += velocityMods[i].vel * gameRules.ABLYchainAllyMult * dot;
			}
			else
			{
				total += velocityMods[i].vel * gameRules.ABLYchainEnemyMult * dot;
			}
		}

		return new Vector4(total.x, total.y, total.z, maxMagnitude);
	}

	public void AddVelocityMod(VelocityMod velMod)
	{
		foreach (VelocityMod v in velocityMods) // Check all current velocity mods
		{
			if (v.from != velMod.from)
				continue;
			if (v.velModType != velMod.velModType)
				continue;
			v.vel = velMod.vel; // Overwrite current velocity
			return;
		}

		// Otherwise add it
		velocityMods.Add(velMod);
	}

	public void RemoveVelocityMod(VelocityMod velMod)
	{
		VelocityMod toRemove = null;
		foreach (VelocityMod v in velocityMods) // Search all current velocity mods
		{
			if (v.from != velMod.from)
				continue;
			if (v.velModType != velMod.velModType)
				continue;
			toRemove = v;
		}

		if (toRemove != null)
			velocityMods.Remove(toRemove);
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
	}

	public void AddStatus(Status status)
	{
		foreach (Status s in statuses) // Check all current statuses
		{
			if (s.from != status.from)
				continue;
			if (s.statusType != status.statusType)
				continue;
			s.RefreshTimeLeft(); // If we already have this instance of a status effect, refresh its timer
			return;
		}

		// Otherwise add it
		statuses.Add(status);
	}

	float CalcShieldPoolCur()
	{
		if (shieldMods.Count == 0)
			return 0;

		float total = 0;
		foreach (ShieldMod s in shieldMods) // Check all current shield mods
		{
			// Projected Shield
			if (s.shieldModType == ShieldModType.ShieldProject)
				total += s.shieldPercent * gameRules.ABLYshieldProjectMaxPool;
			else if (s.shieldModType == ShieldModType.Flagship)
				total += s.shieldPercent * gameRules.FLAGshieldMaxPool;
		}

		return total;
	}

	float CalcShieldPoolMax()
	{
		if (shieldMods.Count == 0)
			return 1;

		float total = 0;
		foreach (ShieldMod s in shieldMods) // Check all current shield mods
		{
			// Projected Shield
			if (s.shieldModType == ShieldModType.ShieldProject)
				total += gameRules.ABLYshieldProjectMaxPool;
			else if (s.shieldModType == ShieldModType.Flagship)
				total += gameRules.FLAGshieldMaxPool;
		}

		return total;
	}

	public bool AddShieldMod(ShieldMod shieldMod)
	{
		bool isProjectedShield = shieldMod.shieldModType == ShieldModType.ShieldProject;

		// A destroyed Projected Shield can't be applied to anything until it regenerates past 0
		if (isProjectedShield && shieldMod.shieldPercent <= 0)
			return false;

		foreach (ShieldMod s in shieldMods) // Check all current Shield mods
		{
			// Only one Projected Shield or Flagship Shield on a unit at a time
			// If it belongs to the same unit,
			if (s.shieldModType == shieldMod.shieldModType)
			{
				if (s.from = shieldMod.from)
				{
					s.shieldPercent = shieldMod.shieldPercent;
					return false;
				}
				else // Otherwise return
				{
					return false;
				}
			}
		}

		// Otherwise add the new shield mod
		shieldMods.Add(shieldMod);
		UpdateShield();
		return true;
	}

	public void RemoveShieldMod(ShieldMod shieldMod)
	{
		ShieldMod toRemove = null;
		foreach (ShieldMod s in shieldMods) // Search all current velocity mods
		{
			if (s.from != shieldMod.from)
				continue;
			if (s.shieldModType != shieldMod.shieldModType)
				continue;
			toRemove = s;
		}

		if (toRemove != null)
		{
			shieldMods.Remove(toRemove);
			UpdateShield();
		}
	}

	protected void UpdateShield()
	{
		UpdateHPBarVal(false);
	}
	/*
	void UpdateAbilities() // TODO: REMOVE
	{
		for (int i = 0; i < abilities.Count; i++)
		{
			//if (abilities[i].isActive)
			abilities[i].AbilityTick();
		}

		if (controller)
			controller.UpdateStatsAbilities(this);
	}
	*/
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
		abilities[i].UseAbility(targ);
	}

	public void OrderCommandWheel(int i, AbilityTarget targ)
	{
		if (i == 2)
			foreach (Turret tur in turrets)
				tur.SetTarget(null);
	}

	public bool Damage(float damageBase, float range) // TODO: How much additional information is necessary (i.e. team, source, projectile type, etc.)
	{
		OnDamage();

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
		UpdateHealth();
		

		if (curHealth <= 0)
		{
			Die();
		}

		return true;
	}

	protected virtual void OnDamage()
	{ }

	// Apply damage to the highest priority shield types first
	// If a shield's percent is brought below zero, remove it (depending on the shield type)
	// Any excess after a breaking a shield is applied to the next shield in line
	// At the end, return how much damage was left after filtering through all shields
	public float DamageShield(float dmg)
	{
		if (dmg <= 0)
			return -1;

		// Each unit has at most one Projected Shield and at most one Flagship Shield
		ShieldMod projShield = null;
		ShieldMod flagShield = null;
		foreach (ShieldMod s in shieldMods)
		{
			if (s.shieldModType == ShieldModType.ShieldProject)
				projShield = s;
			else if (s.shieldModType == ShieldModType.Flagship)
				flagShield = s;
		}

		if (projShield != null)
		{
			float curShieldPool = projShield.shieldPercent * gameRules.ABLYshieldProjectMaxPool;

			// If projShieldPool is positive, the shield held, and it now represents the remaining pool
			// otherwise, the shield was broken, and it now represents the leftover damage
			curShieldPool -= dmg;
			// dmg should also be updated for next shield types to take damage
			dmg = Mathf.Max(dmg - projShield.shieldPercent * gameRules.ABLYshieldProjectMaxPool, 0);

			if (curShieldPool >= 0)
			{
				projShield.shieldPercent = curShieldPool / gameRules.ABLYshieldProjectMaxPool;

				UpdateShield();
				projShield.from.GetComponent<Ability_ShieldProject>().UpdateVisuals();
				return -1; // Return a negative number so Damage() knows the shield was not broken
			}
			else // Negative value
			{
				// Apply damage to shield pool, which can go negative
				projShield.shieldPercent = curShieldPool / gameRules.ABLYshieldProjectMaxPool;
				// or reset shield to a baseline pool value
				//projShield.shieldPercent = 0;

				// Notify source that the shield was destroyed, no further action on our side needed
				projShield.from.GetComponent<Ability_ShieldProject>().BreakShield();
			}
		}

		if (flagShield != null)
		{
			float curShieldPool = flagShield.shieldPercent * gameRules.FLAGshieldMaxPool;

			// If projShieldPool is positive, the shield held, and it now represents the remaining pool
			// otherwise, the shield was broken, and it now represents the leftover damage
			curShieldPool -= dmg;
			// dmg should also be updated for next shield types to take damage
			dmg = Mathf.Max(dmg - flagShield.shieldPercent * gameRules.FLAGshieldMaxPool, 0);

			if (curShieldPool >= 0)
			{
				float percent = curShieldPool / gameRules.FLAGshieldMaxPool;
				flagShield.shieldPercent = percent;

				UpdateShield();
				return -1; // Return a negative number so Damage() knows the shield was not broken
			}
			else // Negative value
			{
				// Do not remove flagship shield, it is permanent
				// Reset shield pool to a baseline value
				flagShield.shieldPercent = 0;
			}
		}

		UpdateShield();
		return dmg; // Leftover damage, should be positive
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

		// Apply swarm Shield damage reduction, which can stack a limited number of times
		dmg -= dmg * gameRules.STATswarmShieldDmgReduce * Mathf.Clamp(swarmShieldCounter, 0, gameRules.STATswarmShieldMaxStacks);
		return dmg;
	}

	public void DamageSimple(float healthDmg, float armorDmg) // Simple subtraction to armor and health
	{
		curArmor = Mathf.Clamp(curArmor - armorDmg, 0, maxArmor);
		curHealth = Mathf.Min(curHealth - healthDmg, maxHealth);
		UpdateHealth();

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

		if (hpEffects)
			hpEffects.End();

		foreach (Unit un in notifyOnDeath)
		{
			un.DeathMessage(this);
		}

		if (deathClone)
		{
			GameObject go = Instantiate(deathClone, model.transform.position, model.rotation);
			Clone_Wreck wreck = go.GetComponent<Clone_Wreck>();
			if (wreck)
				wreck.SetHP(maxHealth, maxArmor);
		}

		if (gameManager.Commanders.Length >= team + 1)
			gameManager.Commanders[team].RefundUnitCounter(buildIndex);

		// Refund resources
		if (buildIndex >= 0)
		{
			GameObject go2 = Instantiate(new GameObject());
			Util_ResDelay resDelay = go2.AddComponent<Util_ResDelay>();

			resDelay.GiveRecAfterDelay(gameManager.Commanders[team].GetBuildUnit(buildIndex).cost, gameRules.WRCKlifetime, team);
		}

		Destroy(hpBar.gameObject);
		Destroy(selCircle);
		Destroy(gameObject);
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

	/// <summary>
	/// Current health, maximum health, current armor, maximum armor
	/// </summary>
	public Vector4 GetHP()
	{
		return new Vector4(curHealth, maxHealth, curArmor, maxArmor);
	}

	public bool IsDead()
	{
		return dead;
	}

	public Transform GetSwarmTarget()
	{
		return swarmTarget;
	}

	public override void OnHover(bool hovered)
	{
		base.OnHover(hovered);
		if (hovered)
			UpdateHPBarVal(true); // Instantly update HPBar the moment this object is hovered
		UpdateHPBarPosAndVis();
	}

	public override void OnSelect(bool selected)
	{
		base.OnSelect(selected);
		UpdateHPBarPosAndVis();
	}

	public override void LinkStats(bool detailed, Controller_Commander controller)
	{
		base.LinkStats(detailed, controller);
	}
}
