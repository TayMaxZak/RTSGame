using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : Entity, ITargetable
{
	public bool disableTurrets = false;

	[Header("Objectives")]
	[SerializeField]
	private int objectiveWeight = 1; // How much this unit counts towards objective capture

	[Header("Feedback")]
	[SerializeField]
	private UI_HPBar hpBarPrefab;

	[Header("Effects")]
	[SerializeField]
	private Effect_HP hpEffects;
	[SerializeField]
	private Effect_Engine engineEffects;

	[Header("Identification")]
	public int team = 0;
	[HideInInspector]
	public int buildIndex = -1;
	private UnitSelectable selectable;

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
	private float curBurnTimer;

	[SerializeField]
	private float curFragileHealth = 0;
	private float curFragileTimer;

	[SerializeField]
	private GameObject deathClone; // Object to spawn on death
	private bool dead;

	[Header("Combat")]
	[SerializeField]
	private Turret[] turrets;
	private Unit target;

	[Header("Abilities")]
	[SerializeField]
	public List<Ability> abilities;

	[Header("Movement")]
	[SerializeField]
	private UnitMovement movement;

	private bool selected;

	[System.NonSerialized]
	public UI_HPBar hpBar; // Should be accessible by ability scripts
	private Vector2 hpBarOffset;

	// State //
	private List<Status> statuses;
	private List<VelocityMod> velocityMods;
	private List<ShieldMod> shieldMods;
	//private List<Unit> damageTaken; // Units that assisted in this unit's death
	private List<FighterGroup> enemySwarms;

	void Awake()
	{
		hpBar = Instantiate(hpBarPrefab); // TODO: In awake?
		statuses = new List<Status>();
		velocityMods = new List<VelocityMod>();
		shieldMods = new List<ShieldMod>();
		enemySwarms = new List<FighterGroup>();
	}

	//public void SetHeightCurrent(int cur)
	//{
	//	movement.SetVCurrent(cur);
	//}

	// Use this for initialization
	protected new void Start()
	{
		base.Start(); // Init Entity base class

		selCircleSpeed = movement.GetRotationSpeed(); // Make the circle better reflect the unit's scale and mobility
		movement.Init(this);

		selectable = new UnitSelectable(this, 0);
		gameManager.GetCommander(team).AddSelectableUnit(selectable); // Make sure commander knows what units can be selected

		if (gameRules.useTestValues)
		{
			curHealth = curHealth * gameRules.TESTinitHPMult + gameRules.TESTinitHPAdd;
			curArmor = curArmor * gameRules.TESTinitHPMult + gameRules.TESTinitHPAdd;
			//curFragileHealth = (maxHealth - curHealth) * gameRules.TESTinitHPMult;
		}
		curFragileTimer = gameRules.ABLYhealFieldConvertDelay;

		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>(); // Grab copy of UI Manager
		 //hpBar = Instantiate(hpBarPrefab);
		hpBar.transform.SetParent(uiManager.Canvas.transform, false);
		hpBarOffset = uiManager.UIRules.HPBoffset;
		UpdateHPBarPosAndVis(); // Make sure healthbar is hidden until the unit is first selected
		UpdateHPBarVal(true);

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

			if (disableTurrets)
				tur.gameObject.SetActive(false);
		}

		engineEffects.SetEngineActive(true);
	}

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
			isBurning = curHealth / maxHealth <= gameRules.HLTHburnThresh;

			foreach (Status s in statuses)
				if (s.statusType == StatusType.CriticalBurnImmune)
					isBurning = false;

			if (isBurning)
			{
				curBurnTimer += Time.deltaTime;
				if (curBurnTimer >= 1)
				{
					curBurnTimer = 0;
					DamageSimple(Mathf.RoundToInt(Random.Range(gameRules.HLTHburnMin, gameRules.HLTHburnMax)), 0);
				}
			}
		}

		if (curFragileHealth > 0)
		{
			curFragileTimer -= Time.deltaTime;
			if (curFragileTimer <= 0)
			{
				float amount = gameRules.ABLYhealFieldConvertGPS + gameRules.ABLYhealFieldAllyGPSBonusMult * maxHealth;
				AddFragileHealth(-amount * Time.deltaTime);
				DamageSimple(-amount * Time.deltaTime, 0);

				if (curFragileHealth <= 0)
					curFragileTimer = gameRules.ABLYhealFieldConvertDelay;
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

		// Updating health effects now would interfere with the End() method of effect objects
		UpdateHPEffects();
	}

	// Update HPBar position and enemy/ally state
	void UpdateHPBarPosAndVis()
	{
		if (!isSelected && !isHovered)
		{
			if (hpBar.gameObject.activeSelf)
				hpBar.gameObject.SetActive(false);
			return;
		}

		// Position bar
		Vector3 barPosition = new Vector3(transform.position.x + hpBarOffset.x, swarmTarget.position.y + hpBarOffset.y, transform.position.z + hpBarOffset.x);
		Vector3 screenPoint = Camera.main.WorldToScreenPoint(barPosition);

		// Is the bar behind us, should it be hidden?
		float dot = Vector3.Dot((barPosition - Camera.main.transform.position).normalized, Camera.main.transform.forward);
		if (dot < 0)
		{
			if (hpBar.gameObject.activeSelf)
			{
				hpBar.gameObject.SetActive(false);
				return;
			}
		}
		else
		{
			if (!hpBar.gameObject.activeSelf)
				hpBar.gameObject.SetActive(true);

			RectTransform rect = hpBar.GetComponent<RectTransform>();
			rect.position = new Vector2(screenPoint.x, screenPoint.y);
		}

		UpdateHPBarAlly();
	}

	protected void UpdateHPBarVal(bool fastUpdate)
	{
		int armorMax = Mathf.RoundToInt(maxArmor);
		int shieldMax = Mathf.RoundToInt(CalcShieldPoolMax());
		bool newValues = hpBar.SetHealthArmorShield(new Vector3(curHealth / maxHealth, curArmor / (armorMax == 0 ? 1 : armorMax), CalcShieldPoolCur() / (shieldMax == 0 ? 1 : shieldMax)), isBurning);
		UpdateHPBarValFrag();
		if (fastUpdate)
			hpBar.FastUpdate();

		// Update EntityStats only if the new values differ from the current ones
		if (newValues && controller)
		{
			controller.UpdateStatsHP(this);
			controller.UpdateStatsShields(this);
		}
	}

	void UpdateHPBarValFrag()
	{
		hpBar.SetFragileHealth(curFragileHealth / maxHealth);
	}

	void UpdateHPBarAlly()
	{
		// Check enemy/ally state
		if (gameManager.GetController().team == team)
			hpBar.SetIsAlly(true);
		else
			hpBar.SetIsAlly(false);
	}

	void UpdateHPEffects()
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
			// Damage over time
			if (s.statusType == StatusType.ArmorMelt)
			{
				Damage(gameRules.STAT_armorMeltDPS * Time.deltaTime, 0, DamageType.Chemical);
			}

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

		// Number of statuses changed, update UI
		if (toRemove.Count > 0)
			UpdateStatusUI();
	}

	// TODO: Do a better job accounting for armor overflow when adding up SuperlaserMark damage, sometimes projectiles will do way more effective damage than is counted here
	// TODO: Ensure a last hit counts for a Superlaser stack always
	public void AddStatus(Status status)
	{
		int origCount = statuses.Count;

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
				s.AddTimeLeft(status.GetTimeLeft());
			}
			return;
		}
		statuses.Add(status);

		// Number of statuses changed, update UI
		if (statuses.Count != origCount)
			UpdateStatusUI();
	}

	public void RemoveStatus(Status status)
	{
		Status toRemove = null;
		foreach (Status s in statuses) // Search all current statuses
		{
			if (s.from != status.from)
				continue;
			if (s.statusType != status.statusType)
				continue;
			toRemove = s;
		}

		if (toRemove != null)
		{
			statuses.Remove(toRemove);
			// Number of statuses changed, update UI
			UpdateStatusUI();
		}
	}

	// Notify EntityStats that statuses have changed
	void UpdateStatusUI()
	{
		if (!controller)
			return;

		controller.UpdateStatsStatuses(statuses);
	}

	public void AddEnemySwarm(FighterGroup swarm)
	{
		foreach (FighterGroup f in enemySwarms) // Check all current swarms
		{
			if (f != swarm)
				continue;
			return;
		}
		enemySwarms.Add(swarm);
	}

	public void RemoveEnemySwarm(FighterGroup swarm)
	{
		FighterGroup toRemove = null;
		foreach (FighterGroup f in enemySwarms) // Search all current swarms
		{
			if (f != swarm)
				continue;
			toRemove = f;
		}

		if (toRemove != null)
		{
			enemySwarms.Remove(toRemove);
		}
	}

	public List<FighterGroup> GetEnemySwarms()
	{
		return enemySwarms;
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
			return 0;

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
				if (s.from == shieldMod.from)
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
	
	public void UpdateAbilityDisplay(int index, bool updateStacks, bool updateIconB)
	{
		if (!controller)
			return;

		controller.UpdateStatsAbility(this, index, updateStacks);

		if (updateIconB)
			controller.UpdateStatsAbilityIconB(this, index);
	}

	public void AddKill(Unit bounty)
	{
		for (int i = 0; i < abilities.Count; i++)
		{
			if (abilities[i].GetAbilityType() == AbilityType.Superlaser)
			{
				GetComponent<Ability_Superlaser>().GiveStack(bounty);
			}
		}
	}

	public void AddFragileHealth(float frag)
	{
		curFragileHealth = curFragileHealth + frag;
		ClampFragileHealth(); // Fragile health cannot exceed the room left in the health bar
		UpdateHPBarVal(false);
	}

	void ClampFragileHealth()
	{
		curFragileHealth = Mathf.Clamp(curFragileHealth, 0, maxHealth - curHealth); // Fragile health cannot exceed the room left in the health bar
	}

	// Note: does not update HPbar values!
	public void RemoveFragileHealth()
	{
		curFragileHealth = 0;
		curFragileTimer = gameRules.ABLYhealFieldConvertDelay;
	}

	// TODO: How much additional information is necessary (i.e. team, source, projectile type, etc.)
	// TODO: Handle Superlaser mark damage stacking
	public DamageResult Damage(float damageBase, float range, DamageType dmgType)
	{
		OnDamage();

		float dmg = damageBase;

		dmg = StatusDamageMod(dmg, dmgType); // Apply status modifiers to damage first

		if (dmg <= 0)
			return new DamageResult(false);

		dmg = DamageShield(dmg); // Try to damage shield before damaging main health pool

		if (dmg <= 0)
			return new DamageResult(false);

		// Damage lost to range resist / damage falloff, incentivising shooting armor from up close
		float rangeRatio = Mathf.Max(0, (range - gameRules.ARMrangeMin) / (gameRules.ARMrangeMax - gameRules.ARMrangeMin));

		float rangeDamage = Mathf.Min(dmg * rangeRatio, dmg) * gameRules.ARMrangeMult;
		if (!DamageUtils.IgnoresRangeResist(dmgType)) // Range-resist-exempt damage types
		{
			if (curArmor > Mathf.Max(0, dmg - rangeDamage)) // Range resist condition: if this shot wont break the armor, it will be range resisted
				dmg = Mathf.Max(0, dmg - rangeDamage);
			else
				dmg = Mathf.Max(0, dmg); // Not enough armor left, no longer grants range resist
		}

		if (dmg <= 0)
			return new DamageResult(false);

		int armorMeltCount = 0;
		// Taking non-chemical damage refreshes the duration of ArmorMelt
		if (dmgType != DamageType.Chemical)
		{
			foreach (Status s in statuses)
			{
				if (s.statusType == StatusType.ArmorMelt)
				{
					s.RefreshTimeLeft();
					armorMeltCount++;
				}
			}
		}

		// Some damage types handle armor as if it cannot be overflowed
		bool canOverflow = !DamageUtils.CannotOverflowArmor(dmgType);

		// ArmorMelt status affects armor absorption limit (unless this is a Flagship)
		float meltPenaltyFlat = armorMeltCount == 0 || Type == EntityType.Flagship ? gameRules.ARMabsorbFlat : gameRules.STAT_armorMeltAbsorbFlat;
		float meltScalingMult = armorMeltCount == 0 || Type == EntityType.Flagship ? 1 : gameRules.STAT_armorMeltAbsorbScalingMult;

		float absorbLim = Mathf.Min(curArmor, maxArmor < Mathf.Epsilon ? 0 : meltPenaltyFlat + (curArmor / maxArmor) * gameRules.ARMabsorbScaling * meltScalingMult); // Absorbtion limit formula
		float dmgToArmor = canOverflow ? Mathf.Min(absorbLim, dmg) : dmg; // How much damage armor takes
		float overflowDmg = canOverflow ? Mathf.Max(0, dmg - absorbLim) : 0; // How much damage health takes (aka by how much damage exceeds absorbtion limit)

		// Setting new values
		curArmor += -dmgToArmor;
		float healthChange = Mathf.Min(curArmor /*ie armor is negative*/, 0) - overflowDmg;
		curHealth += healthChange;
		if (healthChange < 0) // If this damage tick penetrated armor, remove fragile health
		{
			RemoveFragileHealth();
		}
		curArmor = Mathf.Max(curArmor, 0);
		
		UpdateHealth();

		if (curHealth <= 0)
		{
			Die(dmgType);
			return new DamageResult(true);
		}
		else
			return new DamageResult(false);
	}

	protected virtual void OnDamage()
	{
		
	}

	// Apply damage to the highest priority shield types first
	// If a shield's percent is brought below zero, remove it (depending on the shield type)
	// Any excess after a breaking a shield is applied to the next shield in line
	// At the end, return how much damage was left after filtering through all shields
	public float DamageShield(float dmg)
	{
		// TODO: Make shields not affect damage coming from the unit itself (like damage over time)
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

	private float StatusDamageMod(float dmgOrg, DamageType dmgType)
	{
		// TODO: Make swarm resistance not affect damage coming from the unit itself (like damage over time)
		// TODO: Make swarm resistance not affect area of effect damage
		float dmg = dmgOrg;

		List<Status> allySwarms = new List<Status>();

		// Count number of allied swarms
		foreach (Status s in statuses)
		{
			if (s.statusType == StatusType.SwarmResist)
				allySwarms.Add(s);
		}

		int stack = Mathf.Min(allySwarms.Count, gameRules.STATswarmResistMaxStacks);
		// Apply swarm damage reduction, which can stack a limited number of times
		float swarmAbsorbedDamage = dmgType == DamageType.Swarm ? Mathf.Min(dmg * gameRules.STATswarmResistMultSwarm * stack, dmgOrg)
			 : Mathf.Min(dmg * gameRules.STATswarmResistMult * stack, dmgOrg);

		if (stack > 0)
		{
			// Transfer absorbed damage to the swarms
			int index = dmgType == DamageType.Swarm ? 0 : (int)(Random.value * stack); 
			if (allySwarms[index].from)
			{
				FighterGroup swarm = allySwarms[index].from.GetComponent<FighterGroup>();
				swarm.Damage(swarmAbsorbedDamage * gameRules.STATswarmResistTransferMult, 0, dmgType);
			}
			
			// TODO: Tell ALL allied swarms to attack enemy swarms
		}
		return dmg - swarmAbsorbedDamage;
	}

	public void DamageSimple(float healthDmg, float armorDmg) // Simple subtraction to armor and health
	{
		curArmor = Mathf.Clamp(curArmor - armorDmg, 0, maxArmor);
		curHealth = Mathf.Min(curHealth - healthDmg, maxHealth);
		ClampFragileHealth(); // Fragile health cannot exceed the room left in the health bar
		UpdateHealth();

		if (curHealth <= 0)
		{
			Die(DamageType.Normal);
		}
	}

	public void Die(DamageType damageType)
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
				if (ratio >= gameRules.ABLYsuperlaserStackDmgReq)
					if (s.from) // Potentially the recipient of the stack does not exist anymore
						s.from.GetComponent<Ability_Superlaser>().GiveStack(this);
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
			if (damageType == DamageType.Superlaser || damageType == DamageType.Internal)
			{
				// No wreck
			}
			else
			{
				// Spawn wreck
				GameObject go = Instantiate(deathClone, transform.position, transform.rotation);
				Clone_Wreck wreck = go.GetComponent<Clone_Wreck>();
				if (wreck)
				{
					wreck.SetMass(maxHealth, maxArmor);
					wreck.SetHVelocity(movement.GetVelocity());
				}
			}
		}

		Commander comm = gameManager.GetCommander(team);
		if (comm)
		{
			comm.RemoveSelectableUnit(selectable);
			comm.RefundUnitCounter(buildIndex);

			// Refund resources if build index is initialized
			if (buildIndex >= 0)
			{
				GameObject go2 = new GameObject();
				Util_ResDelay resDelay = go2.AddComponent<Util_ResDelay>();

				resDelay.GiveRecAfterDelay(comm.GetBuildUnit(buildIndex).cost, gameRules.WRCKlifetime, team);
			}
		}

		Destroy(hpBar.gameObject);
		Destroy(selCircle);
		Destroy(gameObject);
	}

	public int GetObjectiveWeight()
	{
		return objectiveWeight;
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

	/// <summary>
	/// Current total shields, maximum total shields
	/// </summary>
	public Vector2 GetShields()
	{
		return new Vector2(CalcShieldPoolCur(), CalcShieldPoolMax());
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
		{
			UpdateHPBarAlly(); // Must be done here before UpdateHPBarVal to prevent visual incongruity
			UpdateHPBarVal(true); // Instantly update HPBar the moment this object is hovered
		}
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


	public void OrderMove(Vector3 newGoal, bool group)
	{
		movement.SetHGoal(newGoal, group);
	}

	public void SetAbilityGoal(AbilityTarget newGoal)
	{
		movement.SetAbilityGoal(newGoal);
	}

	public void ClearAbilityGoal()
	{
		movement.ClearAbilityGoal();
	}

	public void OrderChangeHeight(int heightChange)
	{
		movement.SetVGoal(heightChange);
	}

	public void OrderAttack(Unit newTarg)
	{
		target = newTarg;
		foreach (Turret tur in turrets)
			tur.SetManualTarget(target);
	}

	public void OrderAbility(int i, AbilityTarget targ)
	{
		abilities[i].UseAbility(targ);
	}

	public void OrderCommandWheel(int i, AbilityTarget targ)
	{
		if (i == 1) // Set Squadron
		{
			if (Type != EntityType.Flagship)
				selectable.squadronId = 1;
		}
		else if (i == 3) // Clear Manual Target
		{
			foreach (Turret tur in turrets)
				tur.SetManualTarget(null);
		}
	}

	public Turret[] GetTurrets()
	{
		return turrets;
	}


	public Vector3 GetPosition()
	{
		return transform.position;
	}

	public int GetTeam()
	{
		return team;
	}

	public bool HasCollision()
	{
		return true;
	}

	public TargetType GetTargetType()
	{
		return TargetType.Unit;
	}


	public void OnDrawGizmos()
	{
		movement.OnDrawGizmos();
	}
}
