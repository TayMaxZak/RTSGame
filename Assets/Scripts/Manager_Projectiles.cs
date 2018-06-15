using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;
using MainModule = UnityEngine.ParticleSystem.MainModule;
using EmitParams = UnityEngine.ParticleSystem.EmitParams;

public class Manager_Projectiles : MonoBehaviour
{
	public GameObject test;
	[SerializeField]
	public List<Projectile> projectiles;
	private List<Projectile> toDelete;
	[SerializeField]
	private LayerMask layerMask;
	[SerializeField]
	private ParticleSystem pS;
	private Particle[] particles;
	//private MainModule main;

	private int numAlive = 0;

	private int newProjectilesThisFrame;

	private GameRules gameRules;

	void Start()
	{
		particles = new Particle[pS.main.maxParticles];
		if (particles == null || pS == null)
		{
			throw new System.Exception("Assign particle system to projectile manager.");
		}

		projectiles = new List<Projectile>();
		toDelete = new List<Projectile>();
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
	}

	public void Update()
	{
		if (projectiles.Count <= 0) // If nobodys shooting, dont bother getting/setting particles
			return;

		numAlive = pS.GetParticles(particles);

		for (int i = 0; i < projectiles.Count; i++)
		{
			Projectile proj = projectiles[i];
			
			RaycastHit hit;
			if (Physics.Raycast(proj.position, proj.direction, out hit, proj.GetSpeed() * Time.deltaTime, layerMask))
			{
				if (hit.collider.transform.parent) // Is this a unit?
				{
					Unit unit = hit.collider.transform.parent.GetComponent<Unit>();
					if (unit) // If we hit a unit and its not on our team, damage it
					{
						unit.Damage(proj.GetDamage(), proj.CalcRange());
					}
				}

				proj.position = hit.point - proj.direction * gameRules.PRJhitOffset; // Move to contact point
				particles[i].position = proj.position;

				particles[i].remainingLifetime = 0; // Destroy projectile
				toDelete.Add(proj);
				continue;
			}//if Raycast
			
			proj.UpdateTimeAlive(Time.deltaTime);
			if (proj.GetTimeAlive() > gameRules.PRJmaxTimeAlive)
			{
				particles[i].remainingLifetime = 0; // Destroy projectile
				toDelete.Add(proj);
				//continue;
			}
			
			proj.position += proj.direction * proj.GetSpeed() * Time.deltaTime; // Update projectile position

			particles[i].position = proj.position; // Sync particle with projectile
			particles[i].velocity = proj.direction;

			//Debug.DrawRay(proj.position, proj.direction, Color.red);
		}//for

		for (int i = 0; i < toDelete.Count; i++)
		{
			//Instantiate(test, toDelete[i].position, Quaternion.identity);
			projectiles.Remove(toDelete[i]);
		}
		toDelete.Clear();

		//newProjectilesThisFrame = 0;
		pS.SetParticles(particles, numAlive);
	} //Update()

	void Remove(int i)
	{
		
	}

	void LogParticles()
	{
		if ((int)(Time.time * 100) % 100 == 0)
		{
			int count = pS.subEmitters.subEmittersCount;
			int subTotal = 0;

			for (int i = 1; i < count; i++)
			{
				subTotal += pS.subEmitters.GetSubEmitterSystem(i).particleCount;
			}
			Debug.Log("NUM ALIVE: " + (pS.particleCount) + " -> " + (pS.particleCount + subTotal));
		}
	}

	public void SpawnProjectile(Projectile temp, int team, Vector3 position, Vector3 direction)
	{
		if (numAlive >= particles.Length)
			throw new System.Exception("Too many projectiles.");

		Projectile proj = new Projectile(temp);
		proj.SetTeam(team);
		proj.SetStartPosition(position);
		proj.position = position;
		proj.direction = direction;
		projectiles.Add(proj);

		EmitParams param = new EmitParams();
		param.position = position;
		param.velocity = direction;
		pS.Emit(param, 1);
	}

	void FillHole(int index)
	{
		Particle temp = particles[index];
		for (int i = 0; i < pS.GetParticles(particles); i++)
		{
			if (i > index)
				particles[i] = particles[i - 1];
		}
		particles[particles.Length - 1] = temp;
	}
}
