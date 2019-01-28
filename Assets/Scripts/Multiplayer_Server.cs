using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Multiplayer_Server : NetworkBehaviour
{
	[SyncVar]
	public int currentTeam = 0;

	void Start()
	{

	}
}
