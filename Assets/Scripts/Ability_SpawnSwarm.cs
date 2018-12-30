using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Particle = UnityEngine.ParticleSystem.Particle;

public class Ability_SpawnSwarm : Ability
{
	[SerializeField]
	private Ability_MoveSwarm swarmMover;
	[SerializeField]
	private ParticleSystem pS;
	[SerializeField]
	private FighterGroup swarmCenterPrefab; // Spawned for each swarm, handles gameplay
	private List<FighterGroup> fighterGroups;
	private int livingFighterGroups = 0;

	[SerializeField]
	private GameObject swarmsCenterPrefab; // Middle of all swarms, handles effects that don't need to be done for every single swarm i.e. sound
	private GameObject swarmsCenter;

	private int swarmSize;

	[SerializeField]
	private float deployTime = 2;
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

	private Unit targetUnit;
	private bool checkIfDead = false;
	private UI_AbilBar_SpawnSwarm abilityBar;

	private Particle[] particles;
	private Vector4[] randomVectors;

	private int numberOfLivingParticles;

	private List<int> particlesRemoved;
	private List<int> particlesJustRemoved;
	private List<int> fighterCountPerGroup;

	private Vector3 graveyardPosition = new Vector3(9999, 9999, 9999);

	private float timeError = 0.05f;

	private bool readyToSpawnNextSwarm = true;

	new void Awake()
	{
		base.Awake();

		abilityType = AbilityType.SpawnSwarm;
		InitCooldown();

		stacks = gameRules.ABLYswarmMaxUses;

		fighterGroups = new List<FighterGroup>();
		particlesRemoved = new List<int>();
		particlesJustRemoved = new List<int>();
		fighterCountPerGroup = new List<int>();

		swarmSize = pS.main.maxParticles / gameRules.ABLYswarmMaxUses; // Calculate swarm size
		particles = new Particle[pS.main.maxParticles];
		ParticleSystem.EmissionModule emission = pS.emission;
		emission.rateOverTime = new ParticleSystem.MinMaxCurve(swarmSize / deployTime);
		ParticleSystem.MainModule main = pS.main;
		main.duration = deployTime + timeError;

		randomVectors = new Vector4[pS.main.maxParticles];
		randomDistribution.Normalize();

		displayInfo.stacks = stacks;
		displayInfo.displayStacks = true;
	}

	// Use this for initialization
	new void Start ()
	{
		base.Start();

		abilityBar = parentUnit.hpBar.GetComponent<UI_AbilBar_SpawnSwarm>();
		Display();
	}

	void Display()
	{
		displayInfo.stacks = stacks;
		if (stacks <= 0)
			displayInfo.displayInactive = true;
		else
			displayInfo.displayInactive = false;
		UpdateDisplay(abilityIndex, true);
		UpdateAbilityBar();
	}

	protected override void UpdateAbilityBar()
	{
		abilityBar.SetFighterCounts(fighterCountPerGroup);
	}

	public override void UseAbility(AbilityTarget target)
	{
		if (suspended)
			return;

		if (!offCooldown)
			return;

		base.UseAbility(target);

		if (SpawnSwarm()) // If spawn cast succeeds, also cast move
			MoveSwarm(target.unit);
		else // If cast fails, reset cooldown
			ResetCooldown();
	}

	public void MoveSwarm(Unit unit)
	{
		targetUnit = unit;

		for (int i = 0; i < fighterGroups.Count; i++)
			fighterGroups[i].SetTarget(targetUnit);

		swarmMover.SetTargetUnit(targetUnit);
		if (unit != parentUnit)
			checkIfDead = true;
	}

	bool SpawnSwarm()
	{
		if (stacks > 0)
		{
			// Worst case scenario: we used ability once already but particle system did not have enough time to spawn sufficient number of particles
			// This also makes sure that the FinishSpawnCoroutine always activates the correct fighterGroup and does not leave behind unactivated ones
			// This condition avoids messy situations like these
			if (!readyToSpawnNextSwarm)
				return false;

			// Successful ability cast
			stacks--;
			swarmMover.SetDisplayInactive(false); // At least one swarm is controllable
			if (stacks > 0) // If still over 0 stacks, can be used again
			{
				// If used for the first time,
				if (stacks == gameRules.ABLYswarmMaxUses - 1)
				{
					// nerf parent unit's movement speed
					parentUnit.AddStatus(new Status(gameObject, StatusType.SpawnSwarmSpeedNerf));
				}
			}
			else // Otherwise, cannot be used in the future
			{
				// Restore parent unit's movement speed
				parentUnit.RemoveStatus(new Status(gameObject, StatusType.SpawnSwarmSpeedNerf));
			}

			if (numberOfLivingParticles == 0)
				swarmsCenter = Instantiate(swarmsCenterPrefab, transform.position, Quaternion.identity);

			GameObject go = Instantiate(swarmCenterPrefab.gameObject, transform.position, Quaternion.identity);
			FighterGroup group = go.GetComponent<FighterGroup>();
			group.SetTeam(team);
			fighterGroups.Add(group);
			livingFighterGroups++;
			// Initialize fighter count for this group
			// Since it isn't targetable anyway, it might as well be considered as 5 fighters
			fighterCountPerGroup.Add(swarmSize);

			readyToSpawnNextSwarm = false;
			StartCoroutine(FinishSpawnCoroutine()); // Will finish setup for group

			pS.Play();

			Display();
			return true;
		}
		else
			return false;
	}

	// Assign particle data to fighter group
	IEnumerator FinishSpawnCoroutine()
	{
		yield return new WaitForSeconds(deployTime + timeError);
		int first = (fighterGroups.Count - 1) * swarmSize;
		List<int> list = new List<int>();
		for (int i = 0; i < swarmSize; i++)
		{
			list.Add(first + i);
		}

		int lastIndex = fighterGroups.Count - 1;
		fighterGroups[lastIndex].SetParticles(pS, list.ToArray()); // Indices based on how many particles have already been emitted and how many were just emitted
		fighterGroups[lastIndex].SetTeam(team); // Make sure it belongs to the correct team so turrets know whether or not to shoot it
		fighterGroups[lastIndex].SetParentAbility(this); // Establish connection back to us
		//fighterGroups[lastIndex].SetTarget(targetUnit); // Set target to let it actually influence the game world
		fighterGroups[lastIndex].Activate(); // Now that it has particle data, it can begin to behave like a proper fighter group

		readyToSpawnNextSwarm = true;
	}

	public void RemoveFighter(int index)
	{
		// Stop simulating these particles
		particlesRemoved.Add(index);
		particlesJustRemoved.Add(index);

		// Subtract 1 from apropriate group fighter count
		int groupIndex = index / swarmSize;
		
		fighterCountPerGroup[groupIndex]--;

		Display();
	}

	public void RemoveFighterGroup()
	{
		livingFighterGroups--;
		if (livingFighterGroups == 0) // No swarms left to control
			swarmMover.SetDisplayInactive(true);
	}

	public override void End()
	{
		foreach (FighterGroup go in fighterGroups)
			if (go)
				Destroy(go.gameObject);
		Destroy(swarmsCenter);
	}
	
	// Update is called once per frame
	void LateUpdate ()
	{
		if (particles == null || pS == null)
		{
			return;
		}

		if (targetUnit == null)
		{
			if (checkIfDead)
			{
				MoveSwarm(parentUnit);
			}
			else
				return;
		}

		Vector3 targPos = targetUnit.GetSwarmTarget().position;

		int numAlive = pS.GetParticles(particles);
		numberOfLivingParticles = numAlive;

		if (numAlive == 0)
			return;

		Vector3 overallAvgPos = Vector3.zero;

		Vector3[] avgPositions = new Vector3[fighterGroups.Count];
		for (int i = 0; i < avgPositions.Length; i++)
			avgPositions[i] = Vector3.zero;

		// Do changes
		for (int i = 0; i < numAlive; i++)
		{
			// This fighter is dead, simulation is much simpler in this case
			if (particlesRemoved.Contains(i))
			{
				// Just added
				if (particlesJustRemoved.Contains(i))
				{
					particles[i].velocity = Vector3.zero;
					particles[i].position = graveyardPosition; // Move offscreen
					particlesJustRemoved.Remove(i);
				}
				// Accelerate it downwards
				// TODO: Add this behaviour to a managed VFX
				//particles[i].velocity = new Vector3(particles[i].velocity.x, Mathf.Clamp(particles[i].velocity.y + Time.deltaTime * Physics.gravity.y, -5, 5), particles[i].velocity.z);
				//particles[i].velocity += Vector3.up * Time.deltaTime * Physics.gravity.y;
				continue;
			}

			randomVectors[i].w += Time.deltaTime;
			if (randomVectors[i].w >= 1.0f / randomFreq)
			{
				randomVectors[i] = new Vector3(RandomValue() * randomDistribution.x, RandomValue() * randomDistribution.y, RandomValue() * randomDistribution.z).normalized;
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
			//overallAvgPos += distanceVel / correctedAccel;
		}

		for (int i = 0; i < fighterGroups.Count; i++)
		{
			if (!fighterGroups[i]) // Only run this logic if this fighter group is still alive
			{
				continue;
			}

			int maxNumber = fighterCountPerGroup[i];

			// Number alive (because fighters are emitted one at a time), minus the ones we already know have been emitted before, minus removed ones
			int livingNumber = i == fighterGroups.Count - 1 ? numAlive - Mathf.Max(fighterGroups.Count - 1, 0) * swarmSize : swarmSize;

			// Only divide by the number of most recently spawned particles, or by a maximum of swarmSize taking into account removed fighters
			avgPositions[i] /= Mathf.Clamp(Mathf.Min(livingNumber, maxNumber), 1, swarmSize);

			if (avgPositions[i] != Vector3.zero)
				avgPositions[i] += targPos;
			else
				avgPositions[i] = transform.position;

			fighterGroups[i].transform.position = avgPositions[i];
			overallAvgPos += avgPositions[i];
		}

		if (livingFighterGroups > 0)
		{
			overallAvgPos /= livingFighterGroups;
			swarmsCenter.transform.position = overallAvgPos;
			SetSwarmsCenterActive(true);
		}
		else
			SetSwarmsCenterActive(false);

		// Reassign back to emitter
		pS.SetParticles(particles, numAlive);
	}

	void SetSwarmsCenterActive(bool state)
	{
		if (state && !swarmsCenter.activeSelf)
			swarmsCenter.SetActive(true);
		else if (!state && swarmsCenter.activeSelf)
			swarmsCenter.SetActive(false);
	}

	public bool HasUsed()
	{
		return stacks < gameRules.ABLYswarmMaxUses;
	}

	public Unit GetTargetUnit()
	{
		return targetUnit;
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
