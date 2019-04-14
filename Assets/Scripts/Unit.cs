using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Unit : Entity, ITargetable
{
	public bool disableTurrets = false; // Should this unit spawn with the turret GameObjects inactive

	[Header("Objectives")]
	[SerializeField]
	private int objectiveWeight = 1; // How much this unit counts towards objective capture

	[Header("Feedback")]
	[SerializeField]
	private UI_HPBar hpBarPrefab; // The HPBar which is displayed above this unit
	[System.NonSerialized]
	public UI_HPBar hpBar; // Should be accessible by ability scripts
	[SerializeField]
	private float hpBarHeight;
	private Vector2 hpBarOffset;

	[Header("Effects")]
	[SerializeField]
	private Effect_HP hpEffects; // Emit fire and smoke based on the HP of this unit
	[SerializeField]
	private Effect_Engine engineEffects; // Engine particle effects which are constantly emitted

	[Header("Audio")]
	[SerializeField]
	private AudioEffect_Loop fireAudioLoopPrefab; // Played when the unit is affected by critical health burn
	private AudioEffect_Loop fireAudioLoop;

	[Header("Identification")]
	[SyncVar] // We want to sync the teams of all units when a player joins
	public int team = 0; // What team does this unit belong to
	[HideInInspector]
	public int buildIndex = -1; // What buildable unit should be refunded when this unit dies
	private UnitSelectable selectable; // What selection group does this unit belong to

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
	public bool alwaysBurnImmune = false; // Should this unit ignore critical health burn
	private bool isBurning = false;
	private float curBurnTimer; // When should the next tick of burn damage occur

	[SerializeField]
	private float curFragileHealth = 0;
	private float curFragileTimer; // When should fragile health start converting into health
	private float curFragileTickTimer; // When should next tick of fragile health convert into health

	[SerializeField]
	private float curIons = 0;
	private float curIonTimer; // When should ions start decaying
	private float curIonTickTimer; // When should next tick of ions decay

	[SerializeField]
	private GameObject deathClone; // Object to spawn on death
	private bool dead;

	[Header("Combat")]
	[SerializeField]
	private Turret[] turrets;
	[SerializeField]
	private MeshRenderer[] hideableExtras; // To hide/show based on FOW
	private Unit target; // What unit did we manually target

	[Header("Abilities")]
	[SerializeField]
	public List<Ability> abilities; // This unit's abilities
	[SerializeField]
	public List<Ability> recievingAbilities; // Other units' abiities currently attached to this unit (primarily used for FOW)

	[Header("Movement")]
	[SerializeField]
	private UnitMovement movement; // Handles position and rotation

	// State //
	private List<Status> statuses;
	//private int statusTickRate = 5;
	private float curStatusTickTimer; // When should statuses be updated next
	private List<VelocityMod> velocityMods;
	private List<ShieldMod> shieldMods;
	private List<FighterGroup> enemySwarms;

	private Manager_VFX vfx;
	private Multiplayer_Manager multManager;

	void Awake()
	{
		hpBar = Instantiate(hpBarPrefab);
		statuses = new List<Status>();
		velocityMods = new List<VelocityMod>();
		shieldMods = new List<ShieldMod>();
		enemySwarms = new List<FighterGroup>();

		vfx = GameObject.FindGameObjectWithTag("VFXManager").GetComponent<Manager_VFX>();

		multManager = GameObject.FindGameObjectWithTag("MultiplayerManager").GetComponent<Multiplayer_Manager>(); // For multiplayer

		// Init turrets
		for (int i = 0; i < turrets.Length; i++)
		{
			turrets[i].SetParentUnit(this, i);

			// Make sure weapon damage counts for Superlaser marks
			bool hasSuperlaser = false;
			foreach (Ability a in abilities)
				if (a.GetAbilityType() == AbilityType.Superlaser)
					hasSuperlaser = true;
			if (hasSuperlaser)
				turrets[i].SetOnHitStatus(new Status(gameObject, StatusType.SuperlaserMark));

			if (disableTurrets)
				turrets[i].gameObject.SetActive(false);
		}
	}

	//public void SetHeightCurrent(int cur)
	//{
	//	movement.SetVCurrent(cur);
	//}

	// Use this for initialization
	protected new void Start()
	{
		base.Start(); // Init Entity base class

		// Init movement
		movement.Init(this);
		selCircleSpeed = movement.GetRotationSpeed(); // Make the circle better reflect the unit's scale and mobility

		// Compatability with selection groups
		selectable = new UnitSelectable(this, 0);
		gameManager.GetCommander(team).AddSelectableUnit(selectable); // Make sure commander knows what units can be selected

		// Setting up HP
		if (gameRules.useTestValues)
		{
			curHealth = curHealth * gameRules.TEST_initHPMult + gameRules.TEST_initHPAdd;
			curArmor = curArmor * gameRules.TEST_initHPMult + gameRules.TEST_initHPAdd;
			//curFragileHealth = (maxHealth - curHealth) * gameRules.TESTinitHPMult;
		}
		curBurnTimer = 1;
		curFragileTimer = gameRules.ABLY_healFieldConvertDelay;
		curIonTimer = gameRules.ABLY_ionMissileDecayDelay;

		// Setting up UI
		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>(); // Grab copy of UI Manager
		//hpBar = Instantiate(hpBarPrefab);
		hpBar.transform.SetParent(uiManager.Canvas.transform, false);
		hpBarOffset = uiManager.UIRules.HPBoffset;
		UpdateHPBarPosAndVis(); // Make sure healthbar is hidden until the unit is first selected
		UpdateHPBarVal(true);
		UpdateHPBarValIon();



		// Effects and audio
		engineEffects.SetEngineActive(true);
		//hpEffects.UpdateHealthEffects(curHealth / maxHealth); // Not necessary
		if (fireAudioLoopPrefab)
		{
			fireAudioLoop = Instantiate(fireAudioLoopPrefab, transform.position, Quaternion.identity);
			fireAudioLoop.transform.parent = transform;
			fireAudioLoop.SetEffectActive(false);
		}
	}

	// Update is called once per frame
	protected new void Update ()
	{
		base.Update(); // Entity base class
		// Hide/show turrets
		foreach (Turret t in turrets)
		{
			foreach (MeshRenderer r in t.transform.GetComponentsInChildren<MeshRenderer>())
			{
				r.material.SetFloat("_Opacity", localOpacity);
				if (localOpacity < 0.01f)
					r.enabled = false;
				else
					r.enabled = true;
			}
		}
		// Hide/show extra models associated with abilities
		foreach (MeshRenderer r in hideableExtras)
		{
			r.material.SetFloat("_Opacity", localOpacity);
			if (localOpacity < 0.01f)
				r.enabled = false;
			else
				r.enabled = true;
		}

		// Update HPBar if its being shown
		if (isSelected || isHovered)
			UpdateHPBarPosAndVis();

		// Tick statuses
		curStatusTickTimer -= Time.deltaTime;
		if (curStatusTickTimer <= 0)
		{
			float delta = (1f / gameRules.TIK_statusRate);
			curStatusTickTimer = delta;
			TickStatuses(delta);
		}
		
		// Tick movement
		movement.Tick();

		// Burning
		if (!alwaysBurnImmune)
		{
			isBurning = curHealth / maxHealth <= gameRules.HLTH_burnThresh;

			foreach (Status s in statuses)
				if (s.statusType == StatusType.CriticalBurnImmune)
					isBurning = false;

			if (isBurning)
			{
				curBurnTimer -= Time.deltaTime;
				if (curBurnTimer <= 0)
				{
					curBurnTimer = 1;
					DamageSimple(Mathf.RoundToInt(Random.Range(gameRules.HLTH_burnMin, gameRules.HLTH_burnMax)), 0);
				}
			}
		}

		// Handle modifiers
		if (curFragileHealth > 0)
		{
			curFragileTimer -= Time.deltaTime;
			if (curFragileTimer <= 0)
			{
				curFragileTickTimer -= Time.deltaTime;
				if (curFragileTickTimer <= 0)
				{
					float delta = (1f / gameRules.TIK_fragileHealthConvertRate);
					curFragileTickTimer = delta;
					TickFragileHealth(delta);
				}
			}
		}

		if (curIons > gameRules.ABLY_ionMissileDecayCutoff)
		{
			// TODO: Also lose an ion every 2 seconds?

			curIonTimer -= Time.deltaTime;
			if (curIonTimer <= 0)
			{
				curIonTickTimer -= Time.deltaTime;
				if (curIonTickTimer <= 0)
				{
					float delta = (1f / gameRules.TIK_ionDecayRate);
					curIonTickTimer = delta;
					TickIons(delta);
				}
			}
		}

		//if (Time.frameCount == 2)
		//{
		//	ProxyAI();
		//}
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
		Vector3 barPosition = new Vector3(transform.position.x + hpBarOffset.x, transform.position.y + hpBarHeight + hpBarOffset.y, transform.position.z + hpBarOffset.x);
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
		//UpdateHPBarValIon(true);
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

	void UpdateHPBarValIon()
	{
		hpBar.SetIonHealth(curIons / 100);
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
		if (fireAudioLoop)
		{
			fireAudioLoop.SetEffectActive((curHealth / maxHealth) < gameRules.HLTH_burnThresh);
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

	void TickStatuses(float deltaTime)
	{
		string output = "Current statuses are: ";
		List<Status> toRemove = new List<Status>();

		int suspendedInit = 0;
		int suspendedRemoved = 0;

		int stunnedInit = 0;
		int stunnedRemoved = 0;
		bool ionStunned = false;

		foreach (Status s in statuses)
		{
			// Damage over time
			if (s.statusType == StatusType.ArmorMelt)
			{
				Damage(gameRules.STAT_armorMeltDPS * deltaTime, 0, DamageType.Chemical);
			}

			// Keep track of how many suspending statuses we have
			if (StatusUtils.ShouldSuspendAbilities(s.statusType))
			{
				suspendedInit++;
			}

			// Keep track of how many stunning statuses we have
			if (StatusUtils.ShouldStun(s.statusType))
			{
				stunnedInit++;
			}

			if (StatusUtils.ShouldCountDownDuration(s.statusType))
				if (!s.UpdateTimeLeft(deltaTime))
					toRemove.Add(s);

			output += s.statusType + " " + s.GetTimeLeft() + " ";
		}

		//if (printInfo && statuses.Count > 0)
		//	Debug.Log(output);

		// What are we removing?
		foreach (Status s in toRemove)
		{
			if (StatusUtils.ShouldSuspendAbilities(s.statusType))
			{
				suspendedRemoved++;
			}

			if (StatusUtils.ShouldStun(s.statusType))
			{
				stunnedRemoved++;
				if (s.statusType == StatusType.IonStunned)
					ionStunned = true;
			}

			statuses.Remove(s);
		}

		// Number of statuses changed, unsuspend things and update UI
		if (toRemove.Count > 0)
		{
			// If removing all ability-interrupting status, uninterrupt all abilities
			if (suspendedInit > 0 && suspendedRemoved >= suspendedInit)
			{
				for (int i = 0; i < abilities.Count; i++)
				{
					abilities[i].UnSuspend();
				}
			}

			// If removing all ability-interrupting status, uninterrupt all abilities
			if (stunnedInit > 0 && stunnedRemoved >= stunnedInit)
			{
				for (int i = 0; i < turrets.Length; i++)
				{
					turrets[i].UnSuspend();
				}
				movement.UnSuspend();

				// Remove ions when removing ion-stun
				if (ionStunned)
					UpdateHPBarValIon();
			}

			UpdateStatusUI();
		}
	}

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

		// If adding an ability-interrupting status, interrupt all abilities
		if (StatusUtils.ShouldSuspendAbilities(status.statusType))
		{
			for (int i = 0; i < abilities.Count; i++)
			{
				abilities[i].Suspend();
			}
		}

		// If adding an ability-interrupting status, interrupt all abilities
		if (StatusUtils.ShouldStun(status.statusType))
		{
			for (int i = 0; i < turrets.Length; i++)
			{
				turrets[i].Suspend();
			}
			movement.Suspend();
		}

		// Number of statuses changed, update UI
		if (statuses.Count != origCount)
			UpdateStatusUI();
	}

	public void RemoveStatus(Status status)
	{
		Status toRemove = null;

		int suspendedInit = 0;
		int stunnedInit = 0;

		foreach (Status s in statuses) // Search all current statuses
		{
			if (s.from != status.from)
				continue;
			if (s.statusType != status.statusType)
				continue;
			toRemove = s;

			// Keep track of how many suspending statuses we have
			if (StatusUtils.ShouldSuspendAbilities(s.statusType))
			{
				suspendedInit++;
			}

			// Keep track of how many suspending statuses we have
			if (StatusUtils.ShouldStun(s.statusType))
			{
				stunnedInit++;
			}
		}

		// Number of statuses changed, unsuspend things and update UI
		if (toRemove != null)
		{
			// If removing an ability-interrupting status, uninterrupt all abilities
			if (suspendedInit == 1 && StatusUtils.ShouldSuspendAbilities(toRemove.statusType))
			{
				for (int i = 0; i < abilities.Count; i++)
				{
					abilities[i].UnSuspend();
				}
			}

			// If removing an ability-interrupting status, uninterrupt all abilities
			if (stunnedInit == 1 && StatusUtils.ShouldStun(toRemove.statusType))
			{
				for (int i = 0; i < turrets.Length; i++)
				{
					turrets[i].UnSuspend();
				}
				movement.Suspend();

				if (toRemove.statusType == StatusType.IonStunned)
					UpdateHPBarValIon();
			}

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
			
			if (s.shieldModType == ShieldModType.ShieldProject) // Projected Shield
				total += s.shieldPercent * gameRules.ABLY_shieldProjectMaxPool;
			else if (s.shieldModType == ShieldModType.ShieldMode) // Shield Mode shield
				total += s.shieldPercent * gameRules.ABLY_shieldModeMaxPool;
			else if (s.shieldModType == ShieldModType.Flagship) // Flagship Shield
				total += s.shieldPercent * gameRules.FLAG_shieldMaxPool;
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
			if (s.shieldModType == ShieldModType.ShieldProject) // Projected Shield
				total += gameRules.ABLY_shieldProjectMaxPool;
			else if (s.shieldModType == ShieldModType.ShieldMode) // Shield Mode shield
				total += gameRules.ABLY_shieldModeMaxPool;
			else if (s.shieldModType == ShieldModType.Flagship) // Flagship Shield
				total += gameRules.FLAG_shieldMaxPool;
		}

		return total;
	}

	public bool AddShieldMod(ShieldMod shieldMod)
	{
		// A destroyed Projected Shield can't be applied to anything until it regenerates past 0
		if (shieldMod.shieldModType == ShieldModType.ShieldProject && shieldMod.shieldPercent <= 0)
			return false;

		// A destroyed Shield Mode shield can't be applied to anything until it regenerates past 0
		//if (shieldMod.shieldModType == ShieldModType.ShieldMode && shieldMod.shieldPercent <= 0)
		//	return false;

		foreach (ShieldMod s in shieldMods) // Check all current Shield mods
		{
			// Only one Projected Shield, Shield Mode shield, or Flagship Shield can be on a unit at a time
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

	// Note: does not update HPbar values! The only way you can remove fragile health is by dealing damage, in which you case you would update the HPBar anyway
	void RemoveFragileHealth()
	{
		curFragileHealth = 0;
		curFragileTimer = gameRules.ABLY_healFieldConvertDelay;
	}

	public void TickFragileHealth(float delta)
	{
		float amount = gameRules.ABLY_healFieldConvertGPS + gameRules.ABLY_healFieldAllyGPSBonusMult * maxHealth;
		AddFragileHealth(-amount * delta);
		DamageSimple(-amount * delta, 0);

		if (curFragileHealth <= 0)
			curFragileTimer = gameRules.ABLY_healFieldConvertDelay;
	}

	public void AddIons(float ionsToAdd, bool ionSeed)
	{
		if (ionsToAdd > 0)
		{
			foreach (Status s in statuses)
			{
				// Cannot accrue ions while ion-stunned
				if (s.statusType == StatusType.IonStunned)
				{
					if (printInfo)
						Debug.Log("returning");
					return;
				}
			}

			RefreshIonDecay();
			curIons += ionsToAdd;
			UpdateHPBarValIon();

			// Stun might be called here
			CheckIons();
		}
		else
		{
			curIons += ionsToAdd;

			if (curIons <= gameRules.ABLY_ionMissileDecayCutoff)
				curIons = 0;

			UpdateHPBarValIon();
		}
	}

	//void RemoveIons()
	//{
	//	curIons = 0;
	//	UpdateHPBarValIon();
	//}

	void CheckIons()
	{
		// Ion stun condition and unit isn't already dead from other causes
		//if ((Mathf.Round(curIons) / 100) >= (curHealth / maxHealth) && curHealth > 0)
		if ((curIons / 100) >= (curHealth / maxHealth) && curHealth > 0)
		{
			IonStun();
		}
	}

	void RefreshIonDecay()
	{
		curIonTimer = gameRules.ABLY_ionMissileDecayDelay;
	}

	void TickIons(float delta)
	{
		float amount = gameRules.ABLY_ionMissileDecayLPS;
		AddIons(-amount * delta, false);

		if (curIons <= 0)
			curIonTimer = gameRules.ABLY_ionMissileDecayDelay;
	}

	void IonStun()
	{
		AddStatus(new Status(gameObject, StatusType.IonStunned));

		// Since we cannot add more ions anyway, there's no need to remove all current ions
		// Do this now OR when ion-stun is over
		curIons = 0;
		//hpBar.SetIonHealth(1);
	}

	public DamageResult Damage(float damageBase, float range, DamageType dmgType)
	{
		if (!isServer) // Client hits always fail
		{
			return new DamageResult(false);
		}

		OnDamage();

		float dmg = damageBase;

		dmg = StatusDamageMod(dmg, dmgType); // Apply status modifiers to damage first

		if (dmg <= 0)
			return new DamageResult(false);

		dmg = DamageShield(dmg, dmgType); // Try to damage shield before damaging main health pool

		if (dmg <= 0)
			return new DamageResult(false);

		// Damage lost to range resist / damage falloff, incentivising shooting armor from up close
		float rangeRatio = Mathf.Max(0, (range - gameRules.ARM_rangeMin) / (gameRules.ARM_rangeMax - gameRules.ARM_rangeMin));

		float rangeDamage = Mathf.Min(dmg * rangeRatio, dmg) * gameRules.ARM_rangeMult;
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
		float armorFlat = armorMeltCount == 0 || Type == EntityType.Flagship ? gameRules.ARM_absorbFlat : gameRules.STAT_armorMeltAbsorbFlat;
		float armorScalingMult = armorMeltCount == 0 || Type == EntityType.Flagship ? 1 : gameRules.STAT_armorMeltAbsorbScalingMult;

		float absorbLim = Mathf.Min(curArmor, maxArmor < Mathf.Epsilon ? 0 : armorFlat + (curArmor / maxArmor) * gameRules.ARM_absorbScaling * armorScalingMult); // Absorbtion limit formula
		float dmgToArmor = canOverflow ? Mathf.Min(absorbLim, dmg) : dmg; // How much damage armor takes
		float overflowDmg = canOverflow ? Mathf.Max(0, dmg - absorbLim) : 0; // How much damage health takes (aka by how much damage exceeds absorbtion limit)

		// Setting new values
		curArmor += -dmgToArmor;
		float healthChange = Mathf.Min(curArmor /*ie armor is negative*/, 0) - overflowDmg;
		curHealth += healthChange;
		if (healthChange < 0) // If this damage tick penetrated armor,
		{
			// remove fragile health
			RemoveFragileHealth();
			// Refresh ion decay timer and check if the ratio is sufficient to stun
			RefreshIonDecay();
			CheckIons();
		}
		//else
		//{
			if (curIons > gameRules.ABLY_ionMissileDecayCutoff && dmgType != DamageType.IonMissile) // The minsicule actual damage from an ion missile should not create any ions
				AddIons((dmgToArmor / maxArmor) * 100 * gameRules.ABLY_ionMissileArmorDmgToIons, false);
		//}
		curArmor = Mathf.Max(curArmor, 0);
		
		UpdateHealth();

		ServerDamage(-healthChange, dmgToArmor);

		if (curHealth <= 0)
		{
			if (isServer) // Only actually die from damage on the server
			{
				multManager.CmdKillUnit(GetComponent<NetworkIdentity>(), dmgType);
				Die(dmgType);
			}
			return new DamageResult(true);
		}
		else
			return new DamageResult(false);
	}

	void ServerDamage(float healthDmg, float armorDmg)
	{
		multManager.CmdDmgUnit(GetComponent<NetworkIdentity>(), healthDmg, armorDmg);
	}

	public void ClientDamage(float healthDmg, float armorDmg)
	{
		if (isServer) // For clients only
			return;

		SubtractHealth(healthDmg, armorDmg);
	}

	protected virtual void OnDamage()
	{
		
	}

	// Apply damage to the highest priority shield types first
	// If a shield's percent is brought below zero, remove it (depending on the shield type)
	// Any excess after a breaking a shield is applied to the next shield in line
	// At the end, return how much damage was left after filtering through all shields
	public float DamageShield(float dmg, DamageType dmgType)
	{
		// TODO: Make shields not affect damage coming from the unit itself (like disintegration already on the unit over time)
		if (dmg <= 0)
			return -1;

		// Ion damage does bonus damage against shields
		if (dmgType == DamageType.IonMissile)
		{
			if (Type != EntityType.Flagship)
				dmg *= gameRules.ABLY_ionMissileDamageBonusMult;
			else
				dmg *= gameRules.ABLY_ionMissileDamageBonusMultFlagship;
		}

		// Each unit has at most one Projected Shield, at most one Shield Mode shield, and at most one Flagship Shield
		ShieldMod projShield = null;
		ShieldMod shieldModeShield = null;
		ShieldMod flagShield = null;
		foreach (ShieldMod s in shieldMods)
		{
			if (s.shieldModType == ShieldModType.ShieldProject)
				projShield = s;
			else if (s.shieldModType == ShieldModType.ShieldMode)
				shieldModeShield = s;
			else if (s.shieldModType == ShieldModType.Flagship)
				flagShield = s;
		}

		if (projShield != null)
		{
			float curShieldPool = projShield.shieldPercent * gameRules.ABLY_shieldProjectMaxPool;

			// If curShieldPool is positive, the shield held, and it now represents the remaining pool
			// otherwise, the shield was broken, and it now represents the leftover damage
			curShieldPool -= dmg;
			// dmg should also be updated for next shield types to take damage
			dmg = Mathf.Max(dmg - projShield.shieldPercent * gameRules.ABLY_shieldProjectMaxPool, 0);

			if (curShieldPool > 0)
			{
				projShield.shieldPercent = curShieldPool / gameRules.ABLY_shieldProjectMaxPool;

				projShield.from.GetComponent<Ability_ShieldProject>().OnDamage(); // TODO: Optimize

				UpdateShield();
				return -1; // Return a negative number so Damage() knows the shield was not broken
			}
			else // Negative value
			{
				// Apply damage to shield pool, which can go negative
				projShield.shieldPercent = curShieldPool / gameRules.ABLY_shieldProjectMaxPool;

				// Reset shield to a baseline pool value
				//projShield.shieldPercent = 0;

				projShield.from.GetComponent<Ability_ShieldProject>().OnDamage(); // TODO: Optimize

				// Notify source that the shield was destroyed, no further action on our side needed
				projShield.from.GetComponent<Ability_ShieldProject>().BreakShield(); // TODO: Optimize
			}
		}

		if (shieldModeShield != null)
		{
			float curShieldPool = shieldModeShield.shieldPercent * gameRules.ABLY_shieldModeMaxPool;

			// If curShieldPool is positive, the shield held, and it now represents the remaining pool
			// otherwise, the shield was broken, and it now represents the leftover damage
			curShieldPool -= dmg;
			// dmg should also be updated for next shield types to take damage
			dmg = Mathf.Max(dmg - shieldModeShield.shieldPercent * gameRules.ABLY_shieldModeMaxPool, 0);

			if (curShieldPool > 0)
			{
				shieldModeShield.shieldPercent = curShieldPool / gameRules.ABLY_shieldModeMaxPool;

				shieldModeShield.from.GetComponent<Ability_ShieldMode>().OnDamage(); // TODO: Optimize

				UpdateShield();
				return -1; // Return a negative number so Damage() knows the shield was not broken
			}
			else // Negative value
			{
				// Apply damage to shield pool, which can go negative
				//shieldModeShield.shieldPercent = curShieldPool / gameRules.ABLY_shieldModeMaxPool;
				shieldModeShield.shieldPercent = 0;

				shieldModeShield.from.GetComponent<Ability_ShieldMode>().OnDamage(); // TODO: Optimize

				// Reset shield to a baseline pool value
				//projShield.shieldPercent = 0;

				// Notify source that the shield was destroyed, no further action on our side needed
				//shieldModeShield.from.GetComponent<Ability_ShieldMode>().BreakShield(); // TODO: Optimize
			}
		}

		if (flagShield != null)
		{
			float curShieldPool = flagShield.shieldPercent * gameRules.FLAG_shieldMaxPool;

			// If curShieldPool is positive, the shield held, and it now represents the remaining pool
			// otherwise, the shield was broken, and it now represents the leftover damage
			curShieldPool -= dmg;
			// dmg should also be updated for next shield types to take damage
			dmg = Mathf.Max(dmg - flagShield.shieldPercent * gameRules.FLAG_shieldMaxPool, 0);

			if (curShieldPool > 0)
			{
				float percent = curShieldPool / gameRules.FLAG_shieldMaxPool;
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

		// Leftover ion damage should not be multiplied
		if (dmgType == DamageType.IonMissile)
		{
			if (Type != EntityType.Flagship)
				dmg /= gameRules.ABLY_ionMissileDamageBonusMult;
			else
				dmg /= gameRules.ABLY_ionMissileDamageBonusMultFlagship;
		}
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
			//int index = dmgType == DamageType.Swarm ? 0 : (int)(Random.value * stack);
			int index = (int)(Random.value * stack);
			if (allySwarms[index].from)
			{
				FighterGroup swarm = allySwarms[index].from.GetComponent<FighterGroup>();
				vfx.SpawnEffect(VFXType.Hit_Near, swarm.GetPosition());
				swarm.Damage(swarmAbsorbedDamage * gameRules.STATswarmResistTransferMult, 0, dmgType);
			}
			
			// TODO: Tell ALL allied swarms to attack enemy swarms
		}
		return dmg - swarmAbsorbedDamage;
	}

	// Treats health and armor seperately, and ignores shields, resitances, and statuses
	public void DamageSimple(float healthDmg, float armorDmg, bool handleDamage)
	{
		if (!isServer)
		{
			return;
		}

		ServerDamage(healthDmg, armorDmg);

		SubtractHealth(healthDmg, armorDmg);
	}

	public void DamageSimple(float healthDmg, float armorDmg)
	{
		DamageSimple(healthDmg, armorDmg, false);
	}

	void SubtractHealth(float healthDmg, float armorDmg)
	{
		bool handleDamage = true;
		curArmor = Mathf.Clamp(curArmor - armorDmg, 0, maxArmor);
		curHealth = Mathf.Min(curHealth - healthDmg, maxHealth);
		if (handleDamage)
		{
			if (healthDmg > 0)
				RemoveFragileHealth();
			if (armorDmg > 0 && curIons > gameRules.ABLY_ionMissileDecayCutoff)
				AddIons((armorDmg / maxArmor) * 100 * gameRules.ABLY_ionMissileArmorDmgToIons, false);
		}
		ClampFragileHealth(); // Fragile health cannot exceed the room left in the health bar
		CheckIons();
		UpdateHealth();

		if (curHealth <= 0)
		{
			if (isServer) // Only actually die from damage on the server
			{
				multManager.CmdKillUnit(GetComponent<NetworkIdentity>(), DamageType.Normal);
				Die(DamageType.Normal);
			}
		}
	}

	public virtual void Die(DamageType damageType)
	{
		if (dead)
			return;
		dead = true; // Prevents multiple deaths

		foreach (Ability ab in abilities)
		{
			ab.End();
		}

		// End effects
		if (hpEffects)
			hpEffects.End();
		if (engineEffects)
			engineEffects.End();

		if (isServer)
		{
			Debug.Log("Server die called");

			// Death clone
			if (deathClone)
			{
				if (damageType == DamageType.Superlaser || damageType == DamageType.Internal)
				{
					// No wreck
				}
				else
				{
					// Spawn wreck, though it will only actually do anything gameplay related on the server
					GameObject go = Instantiate(deathClone, transform.position, transform.rotation);
					Clone_Wreck wreck = go.GetComponent<Clone_Wreck>();
					if (wreck)
					{
						wreck.SetMass(maxHealth, maxArmor);
						wreck.SetHVelocity(movement.GetVelocity());
					}
				}
			}

			// Grant superlaser stacks
			foreach (Status s in statuses)
			{
				// Check if sufficient damage was dealt to grant a Superlaser stack
				if (s.statusType == StatusType.SuperlaserMark)
				{
					float ratio = s.GetTimeLeft() / (maxHealth + maxArmor);

					if (ratio >= gameRules.ABLY_superlaserStackDmgReq)
						if (s.from) // The recipient of the stack potentially does not exist anymore
							s.from.GetComponent<Ability_Superlaser>().GiveStack(this);
				}
			}

			// Refund resources and unit counter
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

					resDelay.GiveRecAfterDelay(comm.GetBuildUnit(buildIndex).cost, gameRules.WRCK_lifetime, team);
				}
			}
		}
		else
			Debug.Log("Client die called");

		Destroy(hpBar.gameObject);
		Destroy(selCircle);
		Destroy(gameObject);
	}

	public void ClientDie(DamageType damageType)
	{
		if (isServer) // This is for clients only
			return;
		Die(damageType);
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

	public float GetIons()
	{
		return curIons;
	}

	public bool IsDead()
	{
		return dead;
	}

	public Transform GetSwarmTarget()
	{
		return swarmTarget;
	}

	// Used by Multiplayer_Manager
	public UnitMovement GetMovement()
	{
		return movement;
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
		if (i == 1) // Set Group
		{
			if (Type != EntityType.Flagship)
				selectable.groupId = 1;
		}
		else if (i == 3) // Reset Target
		{
			foreach (Turret tur in turrets)
				tur.SetManualTarget(this);
		}
	}

	public void RevealNearbyUnits()
	{
		Collider[] cols = Physics.OverlapSphere(transform.position, visionRange, gameRules.entityLayerMask);
		//List<Entity> ents = new List<Entity>();
		for (int i = 0; i < cols.Length; i++)
		{
			Entity ent = cols[i].GetComponentInParent<Entity>();
			if (ent)
				ent.SetTeamVisibility(team, true);
		}
	}

	override protected void UpdateLocalVisibility()
	{
		base.UpdateLocalVisibility();
		engineEffects.SetVisible(localVisible);
		if (hpEffects)
			hpEffects.SetVisible(localVisible);
		if (fireAudioLoop)
			fireAudioLoop.SetVisible(localVisible);
		foreach (Ability a in abilities)
			a.SetEffectsVisible(localVisible);
		movement.SetEffectsVisible(localVisible);
		foreach (Ability a in recievingAbilities)
			a.SetRecievingEffectsVisible(localVisible);
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

	public bool GetVisibleTo(int team)
	{
		return VisibleBy(team);
	}

	public GameObject GetGameObject()
	{
		return gameObject;
	}


	// TODO: Causes problems when scene ends
	// When the server un-spawns this object, we need to clean up after it
	//void OnDestroy()
	//{
	//	if (!dead) // Make sure this Destroy() didn't happen because of calling Die() previously
	//		Die(DamageType.Internal);
	//}

	public void OnDrawGizmos()
	{
		movement.OnDrawGizmos();
	}
}
