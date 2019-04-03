using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Manager_Game : MonoBehaviour
{
	[SerializeField]
	private float FOWtickRate = 1f;

	[SerializeField]
	public GameRules GameRules;

	[SerializeField]
	private Commander[] commanders;

	//[SerializeField]
	//private Commander playerCommander;

	[SerializeField]
	private Controller_Commander commanderController;

	public Commander GetCommander(int index)
	{
		if (index < commanders.Length)
			return commanders[index];
		else
			return null;
	}

	//public void SetPlayerCommander(Commander commander)
	//{
	//	//UpdateVisibilityForCommander(false);
	//	playerCommander = commander;
	//	//UpdateVisibilityForCommander(true);
	//}

	public void SetController(Controller_Commander conCom)
	{
		Debug.Log("SET CONTROLLER");
		commanderController = conCom;
	}

	public Controller_Commander GetController()
	{
		return commanderController;
	}

	void Start()
	{
		InvokeRepeating("FOWTick", FOWtickRate, FOWtickRate);
	}

	// Mark
	void FOWTick()
	{
		// TODO: Better way of knowing if game is going on or not
		if (!commanderController)
			return;

		//int t = 0;
		// Initialize visibility for all units
		for (int i = 0; i < commanders.Length; i++)
		{
			List<UnitSelectable> allUnits = commanders[i].GetSelectableUnits();
			for (int j = 0; j < allUnits.Count; j++)
			{
				//t++;
				// Reset flags
				allUnits[j].unit.ClearTeamVisibility();
				// You can see your own units
				allUnits[j].unit.SetTeamVisibility(i, true);
			}
		}

		// Each unit will reveal nearby enemy units as visible
		for (int i = 0; i < commanders.Length; i++)
		{
			List<UnitSelectable> allUnits = commanders[i].GetSelectableUnits();
			for (int j = 0; j < allUnits.Count; j++)
			{
				//t++;
				allUnits[j].unit.RevealNearbyUnits();
			}
		}

		//Debug.Log(t);

		// Update unit visuals based on who the player is in this game instance
		FOWVisuals();
	}

	void FOWVisuals()
	{
		for (int i = 0; i < commanders.Length; i++)
		{
			List<UnitSelectable> allUnits = commanders[i].GetSelectableUnits();
			for (int j = 0; j < allUnits.Count; j++)
			{
				allUnits[j].unit.SetLocalVisiblity(allUnits[j].unit.VisibleBy(commanderController.team));
			}
		}
	}

	/*
	// Default enemy units as invisible and your units as visible
	for (int i = 0; i<commanders.Length; i++)
	{
		List<UnitSelectable> allUnits = commanders[i].GetSelectableUnits();
		if (commanders[i] != playerCommander)
		{
			for (int j = 0; j<allUnits.Count; j++)
			{
				allUnits[j].unit.SetLocalVisiblity(false);
			}
		}
		else
		{
			for (int j = 0; j<allUnits.Count; j++)
			{
				allUnits[j].unit.SetLocalVisiblity(true);
			}
		}
	}
	*/

	//void UpdateVisibilityForCommander(bool vis)
	//{
	//	if (!playerCommander)
	//		return;

	//	List<UnitSelectable> unitSels = playerCommander.GetSelectableUnits();
	//	for (int j = 0; j < unitSels.Count; j++)
	//	{
	//		unitSels[j].unit.SetVisibility(vis);
	//	}
	//}

	public void Defeat(int losingTeam)
	{
		Debug.Log("Hey player " + losingTeam +", you lost!");
		SceneManager.LoadScene("Defeat");
	}
}
