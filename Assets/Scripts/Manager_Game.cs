using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager_Game : MonoBehaviour
{
	[SerializeField]
	public GameRules GameRules;

	[SerializeField]
	private Commander[] commanders;

	public Commander GetCommander(int index)
	{
		return commanders[index];
	}
}
