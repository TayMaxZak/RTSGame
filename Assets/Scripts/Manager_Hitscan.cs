using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;
using MainModule = UnityEngine.ParticleSystem.MainModule;
using EmitParams = UnityEngine.ParticleSystem.EmitParams;

public class Manager_Hitscan : MonoBehaviour
{
	[SerializeField]
	public List<Hitscan> hitscans;
	private List<Hitscan> toDelete;
	private LayerMask mask;
	[SerializeField]
	private ParticleSystem pS;
	//private MainModule main;

	private int numAlive = 0;

	private float width = 0.1f;
	private float directionMult = 0.01f;

	private int newProjectilesThisFrame;

	private GameRules gameRules;

	private int id = 0;

	private int counter = 0;

	void Awake()
	{
		hitscans = new List<Hitscan>();
		toDelete = new List<Hitscan>();

		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
		mask = gameRules.entityLayerMask;

		width = pS.main.startSizeX.constant;
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

	public void SpawnHitscan(Hitscan temp, Vector3 position, Vector3 direction, Unit from, Status onHit)
	{
		Hitscan scan = new Hitscan(temp);
		scan.startPosition = position;
		scan.direction = direction;
		scan.SetFrom(from);
		scan.SetStatus(onHit);
		scan.id = id;
		id++;
		//hitscans.Add(scan);

		// Immediately raycast to do damage. Use actual distance / hit information to inform visuals
		//int index = hitscans.IndexOf(scan);
		float length = Raycast(scan);

		Vector3 size = new Vector3(width, length, 1);

		EmitParams param = new EmitParams()
		{
			position = position,
			velocity = direction * directionMult,
			startSize3D = size,
			//startColor = Random.value * Color.red + Random.value * Color.green + Random.value * Color.blue,
			startLifetime = scan.GetLifetime() // 2x just in case. Particles dying prematurely is the worst thing that could happen to this system
		};
		pS.Emit(param, 1);
	}

	// Called immediately after a hitscan is spawned
	float Raycast(Hitscan scan)
	{
		// Raycast according to start position, direction, and range
		RaycastHit hit;
		if (Physics.Raycast(scan.startPosition, scan.direction, out hit, scan.GetRange(), mask))
		{
			bool hitSelf = false;
			if (hit.collider.transform.parent) // Is this a unit?
			{
				Unit unit = hit.collider.transform.parent.GetComponent<Unit>();
				if (unit != scan.GetFrom()) // If we hit a unit and its not us, damage it
				{
					Status status = scan.GetStatus();
					if (status != null)
					{
						if (status.statusType == StatusType.SuperlaserMark)
							status.SetTimeLeft(scan.GetDamage()); // Store damage in timeLeft field of status

						unit.AddStatus(status);
					}

					float actualRange = (hit.point - scan.startPosition).magnitude;
					if (unit.team != scan.GetFrom().team) // If we hit an enemy, do full damage
					{
						unit.Damage(scan.GetDamage(), actualRange, scan.GetDamageType());
					}
					else // If we hit an ally, do reduced damage because it was an accidental hit
					{
						unit.Damage(scan.GetDamage() * gameRules.PRJfriendlyFireDamageMult, actualRange, scan.GetDamageType());
					}
				}
				else
				{
					// Ignore this collision
					hitSelf = true; // TODO: Adapt friendly fire code for raycasting here
				}
			}

			// Don't do anything if we are passing through the unit that fired us
			if (!hitSelf)
			{
				scan.endPosition = hit.point - scan.direction * gameRules.PRJhitOffset; // Move end to contact point
				return (scan.startPosition - scan.endPosition).magnitude; // Return actual length of hitscan
			}
		}//if Raycast
		return scan.GetRange();
	}
}
