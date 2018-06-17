using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameRules
{
	[Header("Testing")]
	public bool useTestValues = false;
	public float TESTtimeMult = 0.25f;
	public float TESTinitHPAdd = 0;
	public float TESTinitHPMult = 0.2f;
	[Header("Armor")]
	public float ARMabsorbFlat = 20; // How much armor absorb is guarenteed
	public float ARMabsorbMax = 15; // How much armor absorb is added based on current percentage of armor
	public float ARMrangeMin = 25f; // Range past which armor range resist begins
	public float ARMrangeMax = 100f; // Range past which armor range resist is at full effect
	public float ARMrangeMult = 0.8f; // Overall range resist multiplier
	[Header("Health")]
	public float HLTHthreshBurn = 0.201f; // How low does health drop before it starts automatically burning away
	public float HLTHburnMin = 2; // Burn damage per second
	public float HLTHburnMax = 3; // Burn damage per second
	[Header("Wrecks")]
	public float WRCKfallSpeedMax = 10;
	public float WRCKfallSpeedAccel = 3;
	public float WRCKlifetime = 10;
	public float WRCKmassHealthMult = 1f; // When calculating mass, how much should max health count for ("mass" determines damage dealt on collision with a unit)
	public float WRCKmassArmorMult = 0.5f; // When calculating mass, how much should max armor count for
	public float WRCKcollisionSpeedPenalty = 0.6f; // If it hits something, how much speed should it lose
	[Header("Flagship")]
	public float FLAGshieldMaxPool = 500;
	public float FLAGshieldRegenGPS = 5;
	public float FLAGshieldRegenDelay = 10;
	[Header("Resources")]
	public float RESreclaimTime = 5;
	[Header("Spawning")]
	public float SPWNflagshipRadius = 50; // Radius around flagship where units can be spawned
	[Header("Audio")]
	public float AUDpitchVariance = 0.05f; // Audio pitch variation for each clip instance
	[Header("Projectiles")]
	public float PRJmaxTimeAlive = 9f; // How long each projectile lives
	public float PRJhitOffset = 0.05f; // When hitting an object, a projectile always detonates this back far from the hit point
	[Header("Layer Masks")]
	public LayerMask entityLayerMask;
	public LayerMask gridLayerMask;

	[Header("Abilities")]
	public float ABLYarmorDrainRange = 20;
	public float ABLYarmorDrainDPSEnemy = 9;
	public float ABLYarmorDrainDPSAlly = 4;
	public float ABLYarmorDrainGPS = 1; // How much armor is gained per second, per victim (total APS scales with number of victims)
	public float ABLYarmorDrainGPSBonusMult = 1; // How much armor is gained per second, per victim, per number of victims? (total APS scales with number of victims, squared)
	public float ABLYarmorDrainMaxVictims = 5;

	public float[] ABLYarmorRegenHPS = new float[] { 4, 8, 16, 16, 2 }; // Based on armor missing: 20% -> 4ps / 40% -> 8ps / 60% -> 16ps / 80% -> 16ps / 100% -> 2ps

	public float ABLYshieldProjectRangeUse = 20; // Can be cast at this distance
	public float ABLYshieldProjectRange = 30; // If distance exceeds this post-cast, shield is returned
	public float ABLYshieldProjectMaxPool = 200; // Size of shield
	public float ABLYshieldProjectOnGPS = 4; // How much shield pool is gained per second the shield is active
	public float ABLYshieldProjectOnGPSDelay = 5; // Delay after taking damage before shield pool gain begins while the shield is active
	public float ABLYshieldProjectOffGPS = 8; // How much shield pool is gained per second the shield is inactive
	public float ABLYshieldProjectOffGPSNegMult = 2; // GPS multiplier if shield is negative

	public int ABLYswarmMaxUses = 3;
	public float ABLYswarmFirstUseSpeedMult = 0.75f;
	public int ABLYswarmDPS = 4;
	public int ABLYswarmDamageRadius = 7; // How close a swarm has to be to proc its damage reduction status / damage over time

	public float ABLYhealFieldRange = 25;
	public float ABLYhealFieldAllyGPS = 2; // Health gained per second by each ally
	public float ABLYhealFieldAllyGPSBonusMult = 0.005f; // What percentage of missing health should contribute to health gained per second
	public float ABLYhealFieldUserGPS = 5; // Health gained per second by user as long as one ally is being healed
	public int ABLYhealFieldResCost = 4; // Amount of resource points held by this ability while active
	public float ABLYhealFieldResTime = 10; // Delay to return resource points when the ability ends

	public float ABLYchainAllyMult = 0.7f;
	public float ABLYchainEnemyMult = 0.7f;
	public float ABLYchainRange = 25;

	[Header("Statuses")]
	public float STATswarmShieldDmgReduce = 0.1f; // How much of all incoming damage does each swarm protecting an ally reduce
	public float STATswarmShieldMaxStacks = 2; // How many times can this damage reduction stack
}
