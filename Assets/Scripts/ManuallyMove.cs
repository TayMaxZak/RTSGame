using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class ManuallyMove : MonoBehaviour {
	[SerializeField]
	private ParticleSystem pS;
	private Particle[] particles;

	[SerializeField]
	private float randomSpeed = 0.2f;
	[SerializeField]
	private Vector3 randomDistribution = Vector3.one;
	[SerializeField]
	private bool randomAdd = true;
	[SerializeField]
	private float distanceSpeed = 1;
	[SerializeField]
	private bool distanceAdd = true;

	[SerializeField]
	private float maxSpeed = 5f;

	// Use this for initialization
	void Start ()
	{
		particles = new Particle[pS.main.maxParticles];
		randomDistribution.Normalize();
	}
	
	// Update is called once per frame
	void LateUpdate ()
	{
		if (particles == null || pS == null)
		{
			return;
		}

		int numAlive = pS.GetParticles(particles);

		// Do changes
		for (int i = 0; i < numAlive; i++)
		{
			Vector3 distanceVel;

			if (pS.main.simulationSpace == ParticleSystemSimulationSpace.Local) // Set this condition to != to get old behaviour back
				distanceVel = (particles[i].position) * distanceSpeed;
			else
				distanceVel = (particles[i].position - pS.transform.position) * distanceSpeed;

			if (distanceAdd)
				particles[i].velocity += distanceVel;
			else if (Mathf.Abs(distanceSpeed) >= Mathf.Epsilon)
				particles[i].velocity = distanceVel;

			Vector3 randomVel = new Vector3(RandomValue() * randomDistribution.x, RandomValue() * randomDistribution.y, RandomValue() * randomDistribution.z) * randomSpeed;
			if (randomAdd)
				particles[i].velocity += randomVel;
			else if (Mathf.Abs(randomSpeed) >= Mathf.Epsilon)
				particles[i].velocity = randomVel;

			particles[i].velocity = Vector3.ClampMagnitude(particles[i].velocity, maxSpeed);
		}

		// Reassign back to emitter
		pS.SetParticles(particles, numAlive);
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
