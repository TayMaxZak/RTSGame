using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class Particles_Swarming : MonoBehaviour {
	[SerializeField]
	private Transform targetTransform;
	[SerializeField]
	private ParticleSystem pS;
	[SerializeField]
	private GameObject centerOfSwarmPrefab;
	private GameObject centerOfSwarm;
	private Particle[] particles;
	private Vector4[] randomVectors;

	[SerializeField]
	private float randomSpeed = 0.2f;
	[SerializeField]
	private Vector3 randomDistribution = Vector3.one;
	[SerializeField]
	private int randomFreq = 10; // How often new random vectors are chosen



	[SerializeField]
	private float distanceSpeed = 1;

	[SerializeField]
	private float maxSpeed = 5f;

	// Use this for initialization
	void Start ()
	{
		particles = new Particle[pS.main.maxParticles];

		randomVectors = new Vector4[pS.main.maxParticles];
		randomDistribution.Normalize();

		centerOfSwarm = Instantiate(centerOfSwarmPrefab, transform.position, Quaternion.identity);
		/*
		if (doFollowTransform)
			main.customSimulationSpace = targetTransform;
		*/
	}
	
	// Update is called once per frame
	void LateUpdate ()
	{
		if (particles == null || pS == null)
		{
			return;
		}

		int numAlive = pS.GetParticles(particles);

		if (numAlive == 0)
			return;

		Vector3 avgPos = Vector3.zero;

		// Do changes
		for (int i = 0; i < numAlive; i++)
		{
			randomVectors[i].w += Time.deltaTime;
			if (randomVectors[i].w >= 1.0f / randomFreq)
			{
				randomVectors[i] = new Vector3(RandomValue() * randomDistribution.x, RandomValue() * randomDistribution.y, RandomValue() * randomDistribution.z).normalized;
				//randomVectors[i].w = 0;
			}


			Vector3 distanceVel;


			distanceVel = (particles[i].position - targetTransform.position) * distanceSpeed;
			particles[i].velocity += distanceVel;

			Vector3 randomVel = randomVectors[i] * randomSpeed;
			particles[i].velocity += randomVel;

			particles[i].velocity = Vector3.ClampMagnitude(particles[i].velocity, maxSpeed);
			avgPos += distanceVel / distanceSpeed;
		}

		avgPos /= numAlive;
		avgPos += targetTransform.position;
		centerOfSwarm.transform.position = avgPos;

		// Reassign back to emitter
		pS.SetParticles(particles, numAlive);
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
