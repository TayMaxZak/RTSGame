﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Multiplayer_Manager : NetworkBehaviour
{
	public int syncRate_movement = 8;

	void Start()
	{

	}

	[Command]
	public void CmdSyncUnitPosition(NetworkIdentity mover, Vector3 newPos, Vector3 newVel)
	{
		//Debug.Log("Command to sync " + mover.name);
		RpcSyncUnitPosition(mover, newPos, newVel);
	}

	[ClientRpc]
	void RpcSyncUnitPosition(NetworkIdentity mover, Vector3 newPos, Vector3 newVel)
	{
		//Debug.Log("Synced " + mover.name);
		UnitMovement um = mover.GetComponent<Unit>().GetMovement();
		um.SyncPosAndVel(newPos, newVel);
	}

	[Command]
	public void CmdSyncUnitRotation(NetworkIdentity mover, Quaternion newRot, float newRotVel)
	{
		//Debug.Log("Command to sync " + mover.name);
		RpcSyncUnitRotation(mover, newRot, newRotVel);
	}

	[ClientRpc]
	void RpcSyncUnitRotation(NetworkIdentity mover, Quaternion newRot, float newRotVel)
	{
		//Debug.Log("Synced " + mover.name);
		UnitMovement um = mover.GetComponent<Unit>().GetMovement();
		um.SyncRotAndRotVel(newRot, newRotVel);
	}
}
