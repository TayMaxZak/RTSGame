using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;
using MainModule = UnityEngine.ParticleSystem.MainModule;
using EmitParams = UnityEngine.ParticleSystem.EmitParams;

public class Manager_Projectiles : MonoBehaviour
{
	[SerializeField]
	public List<Projectile> projectiles;
	[SerializeField]
	private LayerMask layerMask;
	[SerializeField]
	private ParticleSystem pS;
	private Particle[] particles;
	//private MainModule main;

	private GameRules gameRules;

	void Start()
	{
		particles = new Particle[pS.main.maxParticles];
		//main = pS.main;
		projectiles = new List<Projectile>();
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
	}

	public void Update()
	{

		if (particles == null || pS == null)
		{
			return;
		}

		int numAlive = pS.GetParticles(particles);

		//LogParticles();

		for (int i = 0; i < projectiles.Count; i++)
		{

			Projectile proj = projectiles[i];
			proj.position += proj.direction * proj.GetSpeed() * Time.deltaTime;
			proj.UpdateTimeAlive(Time.deltaTime);
			if (proj.GetTimeAlive() > gameRules.PRJmaxTimeAlive)
			{
				Remove(i);
				continue;
			}

			particles[i].position = proj.position;
			particles[i].velocity = proj.direction;

			//Debug.DrawRay(proj.position, proj.direction, Color.red);

			RaycastHit hit;
			if (Physics.Raycast(proj.position, proj.direction, out hit, proj.GetSpeed() * Time.fixedDeltaTime, layerMask))
			{
				if (!hit.collider.transform.parent) // Unit just died
					return;
				Unit unit = hit.collider.transform.parent.GetComponent<Unit>();
				if (unit)
				{
					unit.Damage(proj.GetDamage(), proj.CalcRange());
				}

				Remove(i);
				continue;

				//FillHole(i);
				//i--;
				//numAlive--;
			}
		}

		pS.SetParticles(particles, numAlive);
	} //Update()

	void Remove(int i)
	{
		projectiles.RemoveAt(i);
		particles[i].remainingLifetime = 0;
	}

	void LogParticles()
	{
		if ((int)(Time.time * 100) % 100 == 0)
		{
			int count = pS.subEmitters.subEmittersCount;
			int subTotal = 0;

			for (int i = 1; i < count; i++)
			{
				//Debug.Log("count = " + count);
				subTotal += pS.subEmitters.GetSubEmitterSystem(i).particleCount;
			}
			Debug.Log("NUM ALIVE: " + (pS.particleCount) + " -> " + (pS.particleCount + subTotal));
		}
	}

	public void SpawnProjectile(Projectile temp, Vector3 position, Vector3 direction)
	{
		Projectile proj = new Projectile(temp);
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
