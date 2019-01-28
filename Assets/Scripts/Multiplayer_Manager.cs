using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Multiplayer_Manager : NetworkBehaviour
{
	public int syncRate_movement = 8;

	//private Manager_Projectiles projs;

	//void Awake()
	//{
	//	projs = GameObject.FindGameObjectWithTag("ProjsManager").GetComponent<Manager_Projectiles>(); // Grab reference to Projectiles Manager);
	//}

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

	[Command]
	public void CmdFireTurret(NetworkIdentity parentUnit, int turretId)
	{
		//Debug.Log("Command to sync shooting from " + parentUnit.name + "'s turret " + turretId);
		RpcFireTurret(parentUnit, turretId);
	}

	[ClientRpc]
	public void RpcFireTurret(NetworkIdentity parentUnit, int turretId)
	{
		//Debug.Log("Synced shooting from " + parentUnit.name + "'s turret " + turretId);
		Unit u = parentUnit.GetComponent<Unit>();
		u.GetTurrets()[turretId].ClientFire();
	}

	[Command]
	public void CmdKillUnit(NetworkIdentity target, DamageType damageType)
	{
		//Debug.Log("Command to sync killing " + target.name);
		RpcKillUnit(target, damageType);
	}

	[ClientRpc]
	public void RpcKillUnit(NetworkIdentity target, DamageType damageType)
	{
		//Debug.Log("Synced killing " + target.name);
		Unit u = target.GetComponent<Unit>();
		u.ClientDie(damageType);
	}
}
