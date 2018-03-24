using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;
using MainModule = UnityEngine.ParticleSystem.MainModule;
using EmitParams = UnityEngine.ParticleSystem.EmitParams;

public class Manager_Projectiles : MonoBehaviour
{
	[SerializeField]
	private int maxProjectiles = 99; // TODO: Change
	[SerializeField]
	public List<Projectile> projectiles;
	[SerializeField]
	private LayerMask layerMask;
	[SerializeField]
	private ParticleSystem pS;
	private Particle[] particles;
	private MainModule main;

	void Start()
	{
		particles = new Particle[pS.main.maxParticles];
		main = pS.main;
		projectiles = new List<Projectile>();
	}

	public void Update()
	{

		if (particles == null || pS == null)
		{
			return;
		}

		int numAlive = pS.GetParticles(particles);

		for (int i = 0; i < projectiles.Count; i++)
		{

			Projectile proj = projectiles[i];
			proj.position += proj.direction * proj.speed * Time.deltaTime;
			particles[i].position = proj.position;
			particles[i].velocity = proj.direction;

			Debug.DrawRay(proj.position, proj.direction, Color.red);

			RaycastHit hit;
			if (Physics.Raycast(proj.position, proj.direction, out hit, proj.speed * Time.fixedDeltaTime, layerMask))
			{
				if (!hit.collider.transform.parent) // Unit just died
					return;
				Unit unit = hit.collider.transform.parent.GetComponent<Unit>();
				if (unit)
				{
					unit.Damage(proj.damage, proj.Range());
				}

				projectiles.RemoveAt(i);
				particles[i].remainingLifetime = 0;
				//FillHole(i);
				//i--;
				//numAlive--;
			}
		}

		pS.SetParticles(particles, numAlive);
	} //Update()

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
