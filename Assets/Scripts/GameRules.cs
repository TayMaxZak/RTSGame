using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameRules
{
	public float ARMabsorbFlat = 20; // How much armor absorb is guarenteed
	public float ARMabsorbMax = 15; // How much armor absorb is added based on current percentage of armor
	public float ARMrangeMin = 25f; // Range past which armor range resist begins
	public float ARMrangeMax = 100f; // Range past which armor range resist is at full effect
	public float ARMrangeMult = 0.8f; // Overall range resist multiplier

	public float HLTHthreshBurn = 0.201f; // How low does health drop before it starts automatically burning away
	public float HLTHburnMin = 2; // Burn damage per second
	public float HLTHburnMax = 3; // Burn damage per second

	public float FLAGshieldMax = 500;
	public float FLAGshieldRegenGPS = 5;
	public float FLAGshieldRegenDelay = 10;

	public float ABLYarmorDrainRange = 20;
	public float ABLYarmorDrainDPSEnemy = 9;
	public float ABLYarmorDrainDPSAlly = 4;
	public float ABLYarmorDrainGPS = 1; // How much armor is gained per second, per victim (total APS scales with number of victims)
	public float ABLYarmorDrainGPSBonusMult = 1; // How much armor is gained per second, per victim, per number of victims? (total APS scales with number of victims, squared)
	public float ABLYarmorDrainMaxVictims = 5;

	public float[] ABLYarmorRegenHPS = new float[] { 4, 8, 16, 16, 2 }; // Based on armor missing: 20% -> 4ps / 40% -> 8ps / 60% -> 16ps / 80% -> 16ps / 100% -> 2ps

	public float ABLYshieldProjectMaxPool = 200;
	public float ABLYshieldProjectInactiveGPS = 8; // How much shield pool is gained per second the shield is inactive

	public int ABLYswarmMaxUses = 3;
	public int ABLYswarmDPS = 4;
	public int ABLYswarmDamageRadius = 7; // How close a swarm has to be to proc its damage reduction status / damage over time


	public float STATswarmShieldDmgReduce = 0.1f; // How much of all incoming damage does each swarm protecting an ally reduce
	public float STATswarmShieldMaxStacks = 2; // How many times can this damage reduction stack


	public float SPWNeffectTime = 1f; // How long before the spawn should the spawn effect start
	public float SPWNwarpTime = 0.05f; // How long the actual warping of the model takes
	public float SPWNflagshipRadius = 20; // Radius around flagship where units can be spawned

	public float AUDpitchVariance = 0.05f; // Audio pitch variation for each clip instance

	public float PRJmaxTimeAlive = 9f; // How long each projectile lives
	public float PRJhitOffset = 0.05f; // When hitting an object, a projectile always detonates this far from the hit point

	public LayerMask entityLayerMask;
	public LayerMask gridLayerMask;
}
