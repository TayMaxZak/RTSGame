using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Multiplayer_Player : NetworkBehaviour
{
	private Controller_Commander commanderController;
	[SerializeField]
	private GameObject flagshipPrefab;
	private Vector3 offset = new Vector3(0, 0, 50);

	void Start()
	{
		if (!isLocalPlayer)
		{
			return;
		}

		Debug.Log("Spawning my flagship");
		CmdSpawnMyFlagship();
	}

	[Command]
	void CmdSpawnMyFlagship()
	{
		// Create the flagship on all instances
		GameObject go = Instantiate(flagshipPrefab, offset * -1 + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * 10, Quaternion.identity);

		NetworkServer.Spawn(go);
	}
}
