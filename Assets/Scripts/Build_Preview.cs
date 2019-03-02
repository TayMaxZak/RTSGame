using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Build_Preview : MonoBehaviour
{
	[HideInInspector]
	public BuildUnit buildUnit;
	[SerializeField]
	private GameObject previewModel;
	[SerializeField]
	private GameObject spawnEffect;

	private GameRules gameRules;
	private Multiplayer_Manager multManager;

	//void Start()
	//{
	//	gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
	//	multManager = GameObject.FindGameObjectWithTag("MultiplayerManager").GetComponent<Multiplayer_Manager>();
	//}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
