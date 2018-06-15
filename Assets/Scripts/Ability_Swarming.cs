using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class Ability_Swarming : Ability
{
	[SerializeField]
	private ParticleSystem pS;
	[SerializeField]
	private int swarmSize = 6;
	[SerializeField]
	private GameObject swarmCenterPrefab; // Spawned for each swarm, handles gameplay
	private List<GameObject> swarmCenters;

	[SerializeField]
	private GameObject swarmsCenterPrefab; // Middle of all swarms, handles effects that don't need to be done for every single swarm i.e. sound
	private GameObject swarmsCenter;

	private Particle[] particles;
	private Vector4[] randomVectors;

	[SerializeField]
	private float randomSpeed = 0.2f;
	[SerializeField]
	private Vector3 randomDistribution = Vector3.one;
	[SerializeField]
	private int randomFreq = 3; // How often new random vectors are chosen



	[SerializeField]
	private float distanceAccel = 0.8f;
	[SerializeField]
	private float distanceAccelMaxMult = 4;

	[SerializeField]
	private float maxSpeed = 5f;

	private int livingParticles;

	void Awake()
	{
		// TODO: Multi-ability
		abilityType = AbilityType.SpawnSwarm;
	}

	// Use this for initialization
	new void Start ()
	{
		base.Start();
		stacks = gameRules.ABLYswarmMaxUses;

		particles = new Particle[pS.main.maxParticles];

		randomVectors = new Vector4[pS.main.maxParticles];
		randomDistribution.Normalize();

		swarmCenters = new List<GameObject>();
	}

	public override void End()
	{
		foreach (GameObject go in swarmCenters)
			Destroy(go);
		Destroy(swarmsCenter);
	}

	void Update()
	{
	}
	
	// Update is called once per frame
	void LateUpdate ()
	{
		if (particles == null || pS == null)
		{
			return;
		}

		if (target == null || !target.unit)
			return;

		Vector3 targPos = target.unit.GetSwarmTarget().position;

		int numAlive = pS.GetParticles(particles);
		livingParticles = numAlive;

		if (numAlive == 0)
			return;

		Vector3 overallAvgPos = Vector3.zero;

		Vector3[] avgPositions = new Vector3[swarmCenters.Count];
		for (int i = 0; i < avgPositions.Length; i++)
			avgPositions[i] = Vector3.zero;

		// Do changes
		for (int i = 0; i < numAlive; i++)
		{
			randomVectors[i].w += Time.deltaTime;
			if (randomVectors[i].w >= 1.0f / randomFreq)
			{
				randomVectors[i] = new Vector3(RandomValue() * randomDistribution.x, RandomValue() * randomDistribution.y, RandomValue() * randomDistribution.z).normalized;
				//randomVectors[i].w = 0;
			}

			float correctedAccel = -distanceAccel * Time.deltaTime;
			Vector3 distanceVel = (particles[i].position - targPos) * correctedAccel;
			Vector3 clampedVel = (particles[i].position - targPos).normalized * Mathf.Min(distanceAccelMaxMult, (particles[i].position - targPos).magnitude) * correctedAccel;
			particles[i].velocity += clampedVel;

			float correctedRandom = randomSpeed * Time.deltaTime;
			Vector3 randomVel = randomVectors[i] * correctedRandom;
			particles[i].velocity += randomVel;
			particles[i].velocity = Vector3.ClampMagnitude(particles[i].velocity, maxSpeed);

			int avgPosIndex = Mathf.FloorToInt(i / swarmSize);
			avgPositions[avgPosIndex] += distanceVel / correctedAccel;
			overallAvgPos += distanceVel / correctedAccel;
		}

		for (int i = 0; i < swarmCenters.Count; i++)
		{
			avgPositions[i] /= Mathf.Clamp(numAlive - i * swarmSize, 1, swarmSize); // Only divide by the number of most recently spawned ships or by a maximum of swarmSize
			if (avgPositions[i] != Vector3.zero)
				avgPositions[i] += targPos;
			else
				avgPositions[i] = transform.position;
			swarmCenters[i].transform.position = avgPositions[i];

			if (Vector3.SqrMagnitude(swarmCenters[i].transform.position - targPos) < gameRules.ABLYswarmDamageRadius * gameRules.ABLYswarmDamageRadius)
			{
				if (target.unit.team != team) // If target is an enemy unit, damage it
					target.unit.Damage(gameRules.ABLYswarmDPS * Time.deltaTime, 0); // 0 range = point blank, armor has no effect
				else
				{
					target.unit.AddStatus(new Status(swarmCenters[i], StatusType.SwarmShield));
				}
			}
		}

		overallAvgPos /= numAlive;
		overallAvgPos += targPos;
		swarmsCenter.transform.position = overallAvgPos;

		// Reassign back to emitter
		pS.SetParticles(particles, numAlive);
	}

	bool SpawnSwarm()
	{
		if (stacks > 0)
		{
			// Worst case scenario: we used ability once already but particle system did not have enough time to spawn sufficient number of particles
			// This condition avoids messy situations like this
			if (livingParticles < swarmCenters.Count * swarmSize)
				return false;

			// Successful ability cast
			stacks--;

			if (livingParticles == 0)
				swarmsCenter = Instantiate(swarmsCenterPrefab, transform.position, Quaternion.identity);

			swarmCenters.Add(Instantiate(swarmCenterPrefab, transform.position, Quaternion.identity));
			pS.Play();
			return true;
		}
		else
			return false;
	}

	public override void UseAbility(AbilityTarget targ)
	{
		base.UseAbility(targ);
		SpawnSwarm(); // If this fails, swarm will just move
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
