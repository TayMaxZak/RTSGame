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

	[SerializeField]
	private Commander playerCommander;

	[SerializeField]
	private Controller_Commander commanderController;

	public Commander GetCommander(int index)
	{
		if (index < commanders.Length)
			return commanders[index];
		else
			return null;
	}

	public void SetPlayerCommander(Commander commander)
	{
		UpdateVisibilityForCommander(false);
		playerCommander = commander;
		UpdateVisibilityForCommander(true);
	}

	public Controller_Commander GetController()
	{
		return commanderController;
	}

	void Start()
	{
		InvokeRepeating("FOWTick", FOWtickRate, FOWtickRate);
	}

	void FOWTick()
	{
		// Default enemy units as invisible and your units as visible
		for (int i = 0; i < commanders.Length; i++)
		{
			List<UnitSelectable> allUnits = commanders[i].GetSelectableUnits();
			if (commanders[i] != playerCommander)
			{
				for (int j = 0; j < allUnits.Count; j++)
				{
					allUnits[j].unit.SetVisibility(false);
				}
			}
			else
			{
				for (int j = 0; j < allUnits.Count; j++)
				{
					allUnits[j].unit.SetVisibility(true);
				}
			}
		}

		// Every second check for nearby enemy entities around each of your selectable units
		List<UnitSelectable> unitSels = playerCommander.GetSelectableUnits();
		for (int j = 0; j < unitSels.Count; j++)
		{
			unitSels[j].unit.UseVision();
		}
	}

	void UpdateVisibilityForCommander(bool vis)
	{
		if (!playerCommander)
			return;

		List<UnitSelectable> unitSels = playerCommander.GetSelectableUnits();
		for (int j = 0; j < unitSels.Count; j++)
		{
			unitSels[j].unit.SetVisibility(vis);
		}
	}

	public void Defeat(int losingTeam)
	{
		Debug.Log("Hey player " + losingTeam +", you lost!");
		SceneManager.LoadScene("Defeat");
	}
}
