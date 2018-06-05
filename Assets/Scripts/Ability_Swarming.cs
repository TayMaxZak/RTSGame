using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class Ability_Swarming : MonoBehaviour {
	private int team; // Doesn't need to be public

	[SerializeField]
	private AbilityTarget target;
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
	private float distanceSpeed = 0.03f;
	[SerializeField]
	private float distanceSpeedMaxMult = 4;

	[SerializeField]
	private float maxSpeed = 5f;

	private int livingParticles;

	private GameRules gameRules;

	private Unit parentUnit;

	// Use this for initialization
	void Start ()
	{
		parentUnit = GetComponent<Unit>();
		team = parentUnit.team;

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;

		particles = new Particle[pS.main.maxParticles];

		randomVectors = new Vector4[pS.main.maxParticles];
		randomDistribution.Normalize();

		swarmCenters = new List<GameObject>();
		/*
		if (doFollowTransform)
			main.customSimulationSpace = targetTransform;
		*/
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

		if (!target.unit)
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

			Vector3 distanceVel = (particles[i].position - targPos) * -distanceSpeed;
			particles[i].velocity += Vector3.ClampMagnitude(distanceVel, distanceSpeed * distanceSpeedMaxMult);

			Vector3 randomVel = randomVectors[i] * randomSpeed;
			particles[i].velocity += randomVel;
			particles[i].velocity = Vector3.ClampMagnitude(particles[i].velocity, maxSpeed);

			int avgPosIndex = Mathf.FloorToInt(i / swarmSize);
			avgPositions[avgPosIndex] += distanceVel / -distanceSpeed;
			overallAvgPos += distanceVel / -distanceSpeed;
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

	public void SetTarget(AbilityTarget targ)
	{
		target = targ;
	}

	public AbilityTarget GetTarget()
	{
		return target;
	}


	public void SpawnSwarm()
	{
		// Worst case scenario: we used ability once already but particle system did not have enough time to spawn sufficient number of particles
		// This condition avoids messy situations like this
		if (livingParticles < swarmCenters.Count * swarmSize)
			return;

		if (livingParticles == 0)
			swarmsCenter = Instantiate(swarmsCenterPrefab, transform.position, Quaternion.identity);

		swarmCenters.Add(Instantiate(swarmCenterPrefab, transform.position, Quaternion.identity));
		pS.Play();
	}

	public void End()
	{
		// TODO: Write this
		//Debug.LogError("Unfinished function");
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
