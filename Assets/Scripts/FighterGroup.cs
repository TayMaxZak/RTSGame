﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class FighterGroup : MonoBehaviour, ITargetable
{
	private Ability_SpawnSwarm parentAbility;
	private Unit targetUnit;

	private int[] indices;
	private ParticleSystem pS;
	private bool isActive;
	private int currentIndex = 0;
	private int team = 0;

	private float[] hp; // TODO: Add passive health repair over time

	private Particle[] particles;

	private int frameAccessed = -1; // Frame when particles were last accessed. They should not be accessed more than once per frame

	private Collider collision;

	private GameRules gameRules;

	void Awake()
	{
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules

		collision = GetComponent<Collider>();
		collision.enabled = false;
	}

	public void SetParticles(ParticleSystem system, int[] ind)
	{
		pS = system;
		particles = new Particle[pS.main.maxParticles];

		indices = ind;

		// Initialize each fighter's health
		hp = new float[ind.Length];
		for (int i = 0; i < hp.Length; i++)
			hp[i] = gameRules.ABLYswarmFighterHealth;
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

		if (targetUnit)
		{
			if (Vector3.SqrMagnitude(transform.position - targetUnit.transform.position) < gameRules.ABLYswarmInteractRadius * gameRules.ABLYswarmInteractRadius)
			{
				if (targetUnit.team != team) // If target is an enemy unit, damage it
					targetUnit.Damage(gameRules.ABLYswarmDPS * Time.deltaTime, 0, DamageType.Swarm); // 0 range = point blank, armor has no effect
				else
				{
					targetUnit.AddStatus(new Status(gameObject, StatusType.SwarmResist));
				}
			}
		}
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


	int RandomIndex()
	{
		return (int)(Random.value * indices.Length - 1);
	}

	void Die()
	{
		parentAbility.RemoveFighterGroup();
		Destroy(gameObject);
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

	public bool Damage(float damageBase, float range, DamageType dmgType)
	{
		bool die = false;

		// Deal damage
		hp[currentIndex] -= damageBase;

		// Get particles
		//GetParticles();
		//particles[indices[currentIndex]].position += new Vector3(RandomValue(), RandomValue(), RandomValue()).normalized * 0.33f;


		if (hp[currentIndex] <= 0)
		{
			// Kill fighter
			parentAbility.RemoveFighter(indices[currentIndex]); // Mark a fighter to not be simulated anymore

			if (currentIndex < hp.Length - 1) // More fighters left
				currentIndex++;
			else // Last fighter died
				die = true;
		}

		//pS.SetParticles(particles, numAlive);
		// After setting particles, we can die
		if (die)
			Die();

		return true;
	}
}