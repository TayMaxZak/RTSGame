using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager_Game : MonoBehaviour
{
	[SerializeField]
	public GameRules GameRules;

	[SerializeField]
	private Commander[] commanders;

	[SerializeField]
	private Controller_Commander commanderController;

	public Commander GetCommander(int index)
	{
		if (index < commanders.Length)
			return commanders[index];
		else
			return null;
	}

	public Controller_Commander GetController()
	{
		return commanderController;
	}
}
