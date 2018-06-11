using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Commander : MonoBehaviour
{
	public int team;

	[Header("GUI")]
	[SerializeField]
	private Controller_Commander controller; // Handles input and UI

	[Header("Building")]
	[SerializeField]
	private BuildUnit[] buildUnits; // What units we can build
	private int[] buildUnitCounters; // Tracks number of times corresponding build unit was built

	[Header("Resources")]
	[SerializeField]
	private int resPoints; // Resource points
	[SerializeField]
	private int reclaimPoints; // Transferred over time into resPoints
	private float reclaimTimer; // Progress turning a reclaimPoint into a resPoint

	private GameRules gameRules;

	// Use this for initialization
	void Start()
	{
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules

		buildUnitCounters = new int[buildUnits.Length]; // All BuildUnit counters start at 0
		Controller_InitBuildButtons(); // Initialize UI through our controller

		reclaimTimer = gameRules.RESreclaimTime; // Don't start at 0. This way the first reclaimPoint will take time to reclaim
		Controller_UpdateResourceAmounts(); // Initualize UI through our controller
	}

	public void SetController(Controller_Commander newController)
	{
		controller = newController;
	}

	// Update is called once per frame
	void Update()
	{
		UpdateReclaiming();
	}

	// For each reclaimPoint, check every frame if we are ready to convert it into a resPoint
	void UpdateReclaiming()
	{
		if (reclaimPoints > 0)
		{
			reclaimTimer -= Time.deltaTime;
			Controller_UpdateResourceTime();

			if (reclaimTimer <= 0)
			{
				reclaimTimer = gameRules.RESreclaimTime;
				reclaimPoints--;
				resPoints++;
				Controller_UpdateResourceAmounts();
			}
		}
	}

	// Pass updated information to our controller
	void Controller_UpdateResourceAmounts()
	{
		if (!controller)
			return;

		controller.UpdateResourceAmounts(resPoints, reclaimPoints);
		controller.UpdateBuildButtonInteract();
	}

	// Pass updated information to our controller
	void Controller_UpdateResourceTime()
	{
		if (controller)
			controller.UpdateResourceTime(reclaimTimer);
	}

	// Initialize building UI according to this Commander
	void Controller_InitBuildButtons()
	{
		if (!controller)
			return;

		controller.InitBuildButtons(buildUnitCounters);
	}


	public BuildUnit GetBuildUnit(int id)
	{
		return buildUnits[id];
	}

	public float GetResources()
	{
		return resPoints;
	}

	public float GetReclaim()
	{
		return reclaimPoints;
	}

	public void GiveResources(int amount)
	{
		AddResPoints(amount);
	}

	public void GiveReclaims(int amount)
	{
		AddReclaimPoints(amount);
	}

	public bool TakeResources(int amount)
	{
		// Return whether or not the subtraction was successful
		return SubtractResPoints(amount);
	}

	// This code will always run on unit death
	public void RefundUnitCounter(int index)
	{
		if (controller)
			controller.CleanSelection();

		if (index < 0) // Not initialized with a build index
			return;

		SubtractBuildUnitCounter(index);

		if (controller)
			controller.SetBuildButtonCounter(index, buildUnitCounters);
	}

	public void IncrementUnitCounter(int index)
	{
		AddBuildUnitCounter(index);

		if (controller) // This code will always run on unit construction
			controller.SetBuildButtonCounter(index, buildUnitCounters);
	}


	private void AddResPoints(int amount)
	{
		resPoints = resPoints + amount;
		Controller_UpdateResourceAmounts();
	}

	private void AddReclaimPoints(int amount)
	{
		reclaimPoints = reclaimPoints + amount;
		Controller_UpdateResourceAmounts();
	}

	private bool SubtractResPoints(int amount)
	{
		int newResPoints = resPoints - amount;

		if (CheckResPointSubtraction(amount)) // If we wont go negative subtracting this amount,
		{
			resPoints = newResPoints; // change resPoints
			Controller_UpdateResourceAmounts(); // and update our controller UI
			return true;
		}
		else
		{
			return false;
		}
	}

	private void SubtractBuildUnitCounter(int index)
	{
		buildUnitCounters[index]--;
	}

	private void AddBuildUnitCounter(int index)
	{
		buildUnitCounters[index]++;
	}

	// Used to determine if a subtraction is possible
	private bool CheckResPointSubtraction(int cost)
	{
		int newResPoints = resPoints - cost;

		if (newResPoints >= 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
}
