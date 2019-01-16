using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class FighterGroup : MonoBehaviour, ITargetable
{
	private Ability_SpawnSwarm parentAbility;
	private int team = 0;

	private Unit targetUnit;
	//private int interactTickRate = 5; // How often does a swarm damage its target
	private float curInteractTimer; // When should damge be dealt next

	private int[] indices;
	private ParticleSystem pS;
	private bool isActive;
	private int currentIndex = 0;
	
	private float[] hp; // TODO: Add passive health repair over time

	private Particle[] particles;

	private int frameAccessed = -1; // Frame when particles were last accessed. They should not be accessed more than once per frame

	private Collider collision;

	private GameRules gameRules;
	private Manager_VFX vfx;

	void Awake()
	{
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules

		collision = GetComponent<Collider>();
		collision.enabled = false;

		vfx = GameObject.FindGameObjectWithTag("VFXManager").GetComponent<Manager_VFX>();
	}

	public void SetParticles(ParticleSystem system, int[] ind)
	{
		pS = system;
		particles = new Particle[pS.main.maxParticles];

		indices = ind;

		// Initialize each fighter's health
		hp = new float[ind.Length];
		for (int i = 0; i < hp.Length; i++)
			hp[i] = gameRules.ABLY_swarmFighterHealth;
	}

	public void SetTeam(int t)
	{
		team = t;
	}

	public void SetParentAbility(Ability_SpawnSwarm swarmManager)
	{
		parentAbility = swarmManager;
	}

	public void SetTarget(Unit targ)
	{
		if (targetUnit) // Have a previous target
			targetUnit.RemoveEnemySwarm(this); // No longer attacking previous target
		targetUnit = targ;
	}

	public void Activate()
	{
		collision.enabled = true;
		isActive = true;
	}

	void Update()
	{
		if (!isActive)
			return;

		curInteractTimer -= Time.deltaTime;
		if (curInteractTimer <= 0)
		{
			float delta = (1f / gameRules.TIK_fighterInteractRate);
			curInteractTimer = delta;
			Interact(delta);
		}


	}

	void Interact(float delta)
	{
		if (targetUnit)
		{
			if (Vector3.SqrMagnitude(transform.position - targetUnit.GetSwarmTarget().position) < gameRules.ABLY_swarmInteractRadius * gameRules.ABLY_swarmInteractRadius)
			{
				if (targetUnit.team != team) // If target is an enemy unit, damage it
				{
					targetUnit.AddEnemySwarm(this);
					targetUnit.Damage(gameRules.ABLY_swarmDPS * delta, 0, DamageType.Swarm); // 0 range = point blank, armor has no effect
				}
				else // Ally unit
				{
					// Protect ally unit
					targetUnit.AddStatus(new Status(gameObject, StatusType.SwarmResist));
					// Engage enemy swarms
					List<FighterGroup> enemySwarms = targetUnit.GetEnemySwarms();
					int stack = Mathf.Min(enemySwarms.Count, gameRules.STATswarmResistMaxStacks);
					int index = (int)(Random.value * stack);
					
					if (enemySwarms.Count > 0)
					{
						if (!IsNull(enemySwarms[index]))
						{
							vfx.SpawnEffect(VFXType.Hit_Near, enemySwarms[index].GetPosition());
							enemySwarms[index].Damage(gameRules.ABLY_swarmDPS * delta, 0, DamageType.Swarm);
						}
					} // enemy swarms present
				} // ally unit
			} // distance
		} // has target unit
	}

	void GetParticles()
	{
		// Particle positions only update once per frame anyway
		if (Time.frameCount == frameAccessed)
			return;

		// Update particles
		pS.GetParticles(particles);

		frameAccessed = Time.frameCount;
	}
	

	void Die()
	{
		if (targetUnit)
			targetUnit.RemoveEnemySwarm(this); // No longer attacking previous target
		parentAbility.RemoveFighterGroup();
		Destroy(gameObject);
	}

	protected bool IsNull(ITargetable t)
	{
		if ((MonoBehaviour)t == null)
			return true;
		else
			return false;
	}

	int RandomIndex()
	{
		return (int)(Random.value * indices.Length - 1);
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}


	public Vector3 GetPosition()
	{
		if (!isActive)
			return Vector3.zero;

		// Get particles
		GetParticles();

		// Return position
		return particles[indices[currentIndex]].position;
		//return particles[indices[RandomIndex()]].position;
	}

	public int GetTeam()
	{
		return team;
	}

	public bool HasCollision()
	{
		return false;
	}

	public TargetType GetTargetType()
	{
		return TargetType.Fighter;
	}

	public bool GetVisibleTo(int team)
	{
		return true;
	}

	public DamageResult Damage(float damageBase, float range, DamageType dmgType)
	{
		bool die = false;

		// Deal damage
		hp[currentIndex] -= damageBase;

		if (hp[currentIndex] <= 0)
		{
			// Kill fighter
			vfx.SpawnEffect(VFXType.Fighter_Die_Explode, GetPosition());
			parentAbility.RemoveFighter(indices[currentIndex]); // Mark a fighter to not be simulated anymore

			if (currentIndex < hp.Length - 1) // More fighters left
				currentIndex++;
			else // Last fighter died
				die = true;
		}

		if (die)
		{
			Die();
			return new DamageResult(true);
		}
		else
			return new DamageResult(false);
	}
}
