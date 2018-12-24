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
	private List<Projectile> toDelete;
	private LayerMask mask;
	[SerializeField]
	private ParticleSystem pS;
	private Particle[] particles;
	//private MainModule main;

	private int numAlive = 0;

	private int lifetime = 99;

	private Manager_VFX vfx;
	private GameRules gameRules;
	private float SL_turretMult = 1.15f; // Empirical multiplier to make 20-damage shots count as expected for superlaser marks
	private float SL_superlaserMult = 20f; // Guarentee a superlaser mark if you use a superlaser shot on a target

	void Awake()
	{
		particles = new Particle[pS.main.maxParticles];
		if (particles == null || pS == null)
		{
			throw new System.Exception("Assign particle system to projectile manager.");
		}

		projectiles = new List<Projectile>();
		toDelete = new List<Projectile>();

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
		mask = gameRules.collisionLayerMask;

		vfx = GameObject.FindGameObjectWithTag("VFXManager").GetComponent<Manager_VFX>();
	}

	public void Update()
	{
		if (projectiles.Count <= 0) // If nobodys shooting, dont bother getting/setting particles
			return;

		numAlive = pS.GetParticles(particles);

		for (int i = 0; i < projectiles.Count; i++)
		{
			Projectile proj = projectiles[i];
			
			// TODO: Also do positional hit detection for situations where a projectile slips into a unit without raycast hitting the outer shell (ie rotation)
			// Raycast according to movement path
			RaycastHit hit;
			if (Physics.Raycast(proj.position, proj.direction, out hit, proj.GetSpeed() * Time.deltaTime, mask))
			{
				int projTeam = proj.GetFrom().team;
				Unit unit = null;
				bool hitSelf = false;
				if (hit.collider.transform.parent) // Is this a unit?
				{
					unit = hit.collider.transform.parent.GetComponent<Unit>();
					if (unit) // Is this a unit?
					{
						if (unit != proj.GetFrom()) // If we hit a unit and its not us, damage it
						{
							Status status = proj.GetStatus();
							if (status != null)
							{
								if (status.statusType == StatusType.SuperlaserMark)
									status.SetTimeLeft(proj.GetDamage() < gameRules.ABLYsuperlaserDmgByStacks[1] ? proj.GetDamage() * SL_turretMult : proj.GetDamage() * SL_superlaserMult); // Store damage in timeLeft field of status

								unit.AddStatus(status);
							}

							// If we hit an ally, do reduced damage because it was an accidental hit
							bool doFullDamage = DamageUtils.IgnoresFriendlyFire(proj.GetDamageType()) || unit.team != projTeam;

							DamageResult result = unit.Damage(doFullDamage ? proj.GetDamage() : proj.GetDamage() * gameRules.DMG_ffDamageMult, proj.CalcRange(), proj.GetDamageType());

							if (result.lastHit)
								proj.GetFrom().AddKill(unit);
						}
						else
						{
							// Ignore this collision
							hitSelf = true;
						}
					}
				}

				// Don't do anything if we are passing through the unit that fired us
				if (!hitSelf)
				{
					proj.position = hit.point - proj.direction * gameRules.PRJhitOffset; // Move to contact point
					particles[i].position = proj.position;

					particles[i].remainingLifetime = 0; // Destroy particle
					toDelete.Add(proj); // Destroy projectile

					Vector3 direction = ((-proj.direction + hit.normal) / 2).normalized;

					if (unit)
					{
						if (unit.GetShields().x > 0) // Shielded
							vfx.SpawnEffect(VFXType.Hit_Absorbed, proj.position, direction, projTeam);
						else // Normal hit
							vfx.SpawnEffect(VFXType.Hit_Normal, proj.position, direction, projTeam);
					}
					else // Terrain
						vfx.SpawnEffect(VFXType.Hit_Normal, proj.position, direction, projTeam);
					continue;
				}
			}//if Raycast
			
			proj.UpdateTimeAlive(Time.deltaTime);
			if (proj.GetTimeAlive() > gameRules.PRJmaxTimeAlive)
			{
				particles[i].remainingLifetime = 0; // Destroy particle
				toDelete.Add(proj); // Destroy projectile
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

	public void SpawnProjectile(Projectile temp, Vector3 position, Vector3 direction, Unit from, Status onHit)
	{
		if (numAlive >= particles.Length)
			throw new System.Exception("Too many projectiles.");

		Projectile proj = new Projectile(temp);
		proj.SetStartPosition(position);
		proj.position = position;
		proj.direction = direction;
		proj.SetFrom(from);
		proj.SetStatus(onHit);
		projectiles.Add(proj);

		EmitParams param = new EmitParams()
		{
			position = position,
			velocity = direction,
			startLifetime = lifetime
		};
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
