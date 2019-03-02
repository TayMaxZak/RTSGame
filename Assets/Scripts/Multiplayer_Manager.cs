using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Multiplayer_Manager : NetworkBehaviour
{
	public int syncRate_movement = 8;

	//private Manager_Projectiles projs;

	void Awake()
	{
		// Set random seed
		//Random.InitState(0);
	}

	//---------------------------------------- MOVEMENT ----------------------------------------//

	[Command]
	public void CmdSyncUnitPosition(NetworkIdentity mover, Vector3 newPos, Vector3 newVel)
	{
		//Debug.Log("Command to sync " + mover.name);
		RpcSyncUnitPosition(mover, newPos, newVel);
	}

	[ClientRpc]
	void RpcSyncUnitPosition(NetworkIdentity mover, Vector3 newPos, Vector3 newVel)
	{
		if (mover == null)
		{
			Debug.LogWarning("[SyncUnitPosition] Can't find unit which was supposed to move to " + newPos);
			return;
		}

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
		if (mover == null)
		{
			Debug.LogWarning("[SyncUnitRotation] Can't find unit which was supposed to rotate.");
			return;
		}

		//Debug.Log("Synced " + mover.name);
		UnitMovement um = mover.GetComponent<Unit>().GetMovement();
		um.SyncRotAndRotVel(newRot, newRotVel);
	}

	//---------------------------------------- TURRETS ----------------------------------------//

	// TODO: Sync random deviation
	[Command]
	public void CmdFireTurret(NetworkIdentity parentUnit, int turretId)
	{
		//Debug.Log("Command to sync shooting from " + parentUnit.name + "'s turret " + turretId);
		RpcFireTurret(parentUnit, turretId);
	}

	// TODO: Sync random deviation
	[ClientRpc]
	public void RpcFireTurret(NetworkIdentity parentUnit, int turretId)
	{
		//Debug.Log("Synced shooting from " + parentUnit.name + "'s turret " + turretId);
		Unit u = parentUnit.GetComponent<Unit>();
		u.GetTurrets()[turretId].ClientFire();
	}

	[Command]
	public void CmdSyncTarget(NetworkIdentity parentUnit, int turretId, NetworkIdentity target, bool manual)
	{
		//Debug.Log("Command to sync shooting from " + parentUnit.name + "'s turret " + turretId);
		RpcSyncTarget(parentUnit, turretId, target, manual);
	}

	[ClientRpc]
	public void RpcSyncTarget(NetworkIdentity parentUnit, int turretId, NetworkIdentity target, bool manual)
	{
		if (target == null)
		{
			Debug.LogWarning("[SyncTarget] Can't find unit which was supposed to be targeted.");
			return;
		}
		if (parentUnit == null)
		{
			Debug.LogWarning("[SyncTarget] Can't find unit which was supposed to have a new target set.");
			return;
		}

		//Debug.Log("Synced shooting from " + parentUnit.name + "'s turret " + turretId);
		Unit u = parentUnit.GetComponent<Unit>();
		u.GetTurrets()[turretId].ClientUpdateTarget(target, manual);
	}

	//---------------------------------------- DAMAGE ----------------------------------------//

	[Command]
	public void CmdKillUnit(NetworkIdentity target, DamageType damageType)
	{
		Debug.Log("Command to sync killing " + target.name);
		RpcKillUnit(target, damageType);
	}

	// TODO: Make sure the unit isn't deleted before we tell it to die
	[ClientRpc]
	public void RpcKillUnit(NetworkIdentity target, DamageType damageType)
	{
		if (target == null)
		{
			Debug.LogWarning("[KillUnit] Can't find unit which was supposed to die from " + damageType + " damage.");
			return;
		}

		Debug.Log("Synced killing " + target.name);
		Unit u = target.GetComponent<Unit>();
		u.ClientDie(damageType);
	}

	[Command]
	public void CmdDmgUnit(NetworkIdentity target, float healthDmg, float armorDmg)
	{
		Debug.Log("Command to sync damaging " + target.name + " by " + (int)healthDmg + " health and " + (int)armorDmg + " armor.");
		RpcDmgUnit(target, healthDmg, armorDmg);
	}

	// TODO: Make sure the unit isn't deleted before we tell it to take damage
	[ClientRpc]
	public void RpcDmgUnit(NetworkIdentity target, float healthDmg, float armorDmg)
	{
		if (target == null)
		{
			Debug.LogWarning("[DmgUnit] Can't find unit which was supposed to take " + (int)healthDmg + " health and " + (int)armorDmg + " armor damage.");
			return;
		}
			

		Debug.Log("Synced damaging " + target.name + " by " + (int)healthDmg + " health and " + (int)armorDmg + " armor.");
		Unit u = target.GetComponent<Unit>();
		u.ClientDamage(healthDmg, armorDmg);
	}

	//---------------------------------------- UNIT SPAWNING ----------------------------------------//

	//[Command]
	//public void CmdSpawnUnit(NetworkIdentity toSpawm, float healthDmg, float armorDmg)
	//{
	//	Debug.Log("Command to sync damaging " + target.name + " by " + (int)healthDmg + " health and " + (int)armorDmg + " armor.");
	//	RpcDmgUnit(target, healthDmg, armorDmg);
	//}
}
