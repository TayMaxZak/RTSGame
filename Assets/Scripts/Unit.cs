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

	[Header("Movement")]
	[SerializeField]
	private UnitMovement movement;

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

		movement.Init(this);

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
		{
			tur.SetParentUnit(this);

			// Make sure weapon damage counts for Superlaser marks
			bool hasSuperlaser = false;
			foreach (Ability a in abilities)
				if (a.GetAbilityType() == AbilityType.Superlaser)
					hasSuperlaser = true;
			if (hasSuperlaser)
				tur.SetOnHitStatus(new Status(gameObject, StatusType.SuperlaserMark));
		}
	}

	// Banking
	//float bank = bankAngle * -Vector3.Dot(transform.right, direction);
	//banker.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.forward);

	// Update is called once per frame
	protected new void Update ()
	{
		base.Update(); // Entity base class
		UpdateStatuses();
		movement.Tick();

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

		if (Time.frameCount == 2)
		{
			ProxyAI();
		}
	}

	void ProxyAI()
	{
		// Attack a main enemy unit and use abilities
		if (team != 0)
		{
			GameObject go = GameObject.FindGameObjectWithTag("Player");
			if (go)
			{
				Unit mainUnit = go.GetComponent<Unit>();
				OrderAttack(mainUnit);
				foreach (Ability a in abilities)
				{
					switch (a.GetAbilityType())
					{
						case AbilityType.SpawnSwarm:
							a.UseAbility(new AbilityTarget(mainUnit));
							break;
						case AbilityType.ShieldProject:
							{
								Collider[] cols = Physics.OverlapSphere(transform.position, 10);
								Transform parent = cols[0].transform.parent;
								if (parent)
								{
									Unit u = parent.GetComponent<Unit>();
									if (u)
									{
										a.UseAbility(new AbilityTarget(u));
									}
								}

							}
							break;
						case AbilityType.HealField:
							a.UseAbility(new AbilityTarget(mainUnit));
							break;
					} // switch
				} // foreach
			} // mainunit
		} // team
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

		Vector3 barPosition = new Vector3(transform.position.x + hpBarOffset.x, swarmTarget.position.y + hpBarOffset.y, transform.position.z + hpBarOffset.x);
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

	public List<VelocityMod> GetVelocityMods()
	{
		return velocityMods;
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

	public List<Status> GetStatuses()
	{
		return statuses;
	}

	void UpdateStatuses()
	{
		string output = "Current statuses are: ";
		List<Status> toRemove = new List<Status>();

		foreach (Status s in statuses)
		{
			if (StatusUtils.ShouldCountDownDuration(s.statusType))
				if (!s.UpdateTimeLeft(Time.deltaTime))
					toRemove.Add(s);

			output += s.statusType + " " + s.GetTimeLeft() + " ";
		}

		if (printInfo && statuses.Count > 0)
			Debug.Log(output);

		foreach (Status s in toRemove)
		{
			statuses.Remove(s);
		}
	}

	// TODO: Do a better job accounting for armor overflow when adding up SuperlaserMark damage, sometimes projectiles will do way more effective damage than is counted here
	public void AddStatus(Status status)
	{
		foreach (Status s in statuses) // Check all current statuses
		{
			if (s.from != status.from)
				continue;
			if (s.statusType != status.statusType)
				continue;

			// If we already have this instance of a status effect, refresh its timer
			if (!StatusUtils.ShouldStackDuration(s.statusType))
				s.RefreshTimeLeft();
			else // or add the new timer to the current timer
			{
				// Superlaser mark damage
				if (s.statusType == StatusType.SuperlaserMark && status.statusType == StatusType.SuperlaserMark) // Don't count damage which wasn't necessary for the kill
				{
					s.AddTimeLeft(Mathf.Min(status.GetTimeLeft(), (curHealth + curArmor)));
				}
				else
					s.AddTimeLeft(status.GetTimeLeft());
			}
			return;
		}

		// Superlaser mark damage
		if (status.statusType == StatusType.SuperlaserMark) // Don't count damage which wasn't necessary for the kill
		{
			status.SetTimeLeft(Mathf.Min(status.GetTimeLeft(), (curHealth + curArmor)));
		}
		statuses.Add(status);
	}

	public void RemoveStatus(Status status)
	{
		Status toRemove = null;
		foreach (Status s in statuses) // Search all current velocity mods
		{
			if (s.from != status.from)
				continue;
			if (s.statusType != status.statusType)
				continue;
			toRemove = s;
		}

		if (toRemove != null)
			statuses.Remove(toRemove);
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

	public void OnShieldChange()
	{
		UpdateShield();
	}

	protected void UpdateShield()
	{
		UpdateHPBarVal(false);
	}
	
	public void UpdateAbilityDisplay(int index, bool updateStacks)
	{
		if (controller)
			controller.UpdateStatsAbilities(this, index, updateStacks);
	}
	
	public void OrderMove(Vector3 newGoal)
	{
		movement.OrderMove(newGoal);
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

	public bool Damage(float damageBase, float range, DamageType dmgType) // TODO: How much additional information is necessary (i.e. team, source, projectile type, etc.)
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
				projShield.from.GetComponent<Ability_ShieldProject>().OnDamage(); // TODO: Optimize
				return -1; // Return a negative number so Damage() knows the shield was not broken
			}
			else // Negative value
			{
				// Apply damage to shield pool, which can go negative
				projShield.shieldPercent = curShieldPool / gameRules.ABLYshieldProjectMaxPool;

				// Reset shield to a baseline pool value
				//projShield.shieldPercent = 0;

				// Notify source that the shield was destroyed, no further action on our side needed
				projShield.from.GetComponent<Ability_ShieldProject>().BreakShield(); // TODO: Optimize
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

		foreach (Status s in statuses)
		{
			// Check if sufficient damage was dealt to grant a Superlaser stack
			if (s.statusType == StatusType.SuperlaserMark)
			{
				float ratio = s.GetTimeLeft() / (maxHealth + maxArmor);

				if (ratio >= gameRules.ABLYsuperlaserDmgAmount)
					if (s.from) // Potentially the recipient of the stack does not exist anymore
						s.from.GetComponent<Ability_Superlaser>().GiveStack();
			}
		}

		foreach (Ability ab in abilities)
		{
			ab.End();
		}

		if (hpEffects)
			hpEffects.End();

		if (deathClone)
		{
			GameObject go = Instantiate(deathClone, transform.position, transform.rotation);
			Clone_Wreck wreck = go.GetComponent<Clone_Wreck>();
			if (wreck)
				wreck.SetHP(maxHealth, maxArmor);
		}

		//if (gameManager.Commanders.Length >= team + 1)
		gameManager.GetCommander(team).RefundUnitCounter(buildIndex);

		// Refund resources
		if (buildIndex >= 0)
		{
			GameObject go2 = Instantiate(new GameObject());
			Util_ResDelay resDelay = go2.AddComponent<Util_ResDelay>();

			resDelay.GiveRecAfterDelay(gameManager.GetCommander(team).GetBuildUnit(buildIndex).cost, gameRules.WRCKlifetime, team);
		}

		Destroy(hpBar.gameObject);
		Destroy(selCircle);
		Destroy(gameObject);
	}

	public Vector3 GetVelocity()
	{
		return movement.GetVelocity();
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
