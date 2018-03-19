using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameRules
{
	public float ARMabsorbFlat = 20; // How much armor absorb is guarenteed
	public float ARMabsorbMax = 15; // How much armor absorb is added based on current percentage of armor
	public float ARMrangeResist = 0.5f; // How much damage is lost per meter of projectile travel when damaging armor

	public float HLTHthreshDisable = 0.667f; // How low does health drop before abilities are disabled
	public float HLTHthreshBurn = 0.201f; // How low does health drop before it starts automatically burning away
	public float HLTHburnMin = 2; // Burn damage per second
	public float HLTHburnMax = 3;
}
