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

	[Header("Layer Masks")]
	public LayerMask entityLayerMask;
	public LayerMask targetLayerMask;
	public LayerMask collisionLayerMask;
	public LayerMask gridLayerMask;

	[Header("Armor")]
	public float ARMabsorbFlat = 5; // How much armor absorb is guaranteed
	public float ARMabsorbScaling = 15; // How much armor absorb is added based on current percentage of armor
	public float ARMrangeMin = 30f; // Range past which armor range resist begins
	public float ARMrangeMax = 60f; // Range past which armor range resist is at full effect
	public float ARMrangeMult = 0.8f; // Overall range resist multiplier

	[Header("Health")]
	public float HLTHburnThresh = 0.2001f; // How low does health drop before it starts automatically burning away
	public float HLTHburnMin = 2; // Burn damage per second
	public float HLTHburnMax = 3; // Burn damage per second

	[Header("Wrecks")]
	public float WRCKfallSpeedMax = 10;
	public float WRCKfallSpeedAccel = 3;
	public float WRCKlifetime = 10; // How long the wreck lasts before dissapearing. Also how long before resources begin to be recovered from a dead unit
	public float WRCKmassHealthMult = 1f; // When calculating mass, how much should max health count for ("mass" determines damage dealt on collision with a unit)
	public float WRCKmassArmorMult = 0.5f; // When calculating mass, how much should max armor count for
	public float WRCKinitialVelMult = 0.5f; // When a unit dies, this ratio of its current horizontal velocity is transferred to its wreck
	public float WRCKcollisionSpeedPenalty = 0.8f; // If it hits something, how much speed should it lose

	[Header("Flagship")]
	public float FLAGshieldMaxPool = 500;
	public float FLAGshieldRegenGPS = 5;
	public float FLAGshieldRegenDelay = 10;

	[Header("Objectives")]
	public float OBJV_captureRange = 25; // Range around objective which counts toward capture
	public float OBJV_captureTime = 20; // Time to go from neutral state to fully controlled state, given a contribution of 1
	public float OBJV_captureAddPerUnitMult = 1; // How much each unit contributes
	public float OBJV_captureAddMax = 3; // capped by this amount

	[Header("Resources")]
	public float RESreclaimTime = 5;

	[Header("Spawning")]
	public float SPWNflagshipRadius = 50; // Radius around flagship where units can be spawned

	[Header("Audio")]
	public float AUDpitchVariance = 0.05f; // Audio pitch variation for each clip instance

	[Header("Projectiles")]
	public float PRJmaxTimeAlive = 5f; // How long each projectile lives
	public float PRJhitOffset = 0.05f; // When hitting an object, a projectile always detonates this back far from the hit point
	public float PRJfriendlyFireCheckRangeMult = 1.0f; // When testing for the potential of friendly fire, how far ahead do we want to check? This is a multiplier on the turret's base range

	[Header("Damage")]
	public float DMG_ffDamageMult = 0.5f; // If we do hit an ally, do reduced damage because it was an accidental glancing hit
	public float DMG_ffDamageMultSplash = 0.25f; // If we do hit an ally, do reduced damage because splash damage friendly fire is almost unavoidable

	[Header("Movement")]
	public float MOVabilityAimingRSMult = 0.33f;
	public int MOV_heightCount = 10;
	public int MOV_heightSpacing = 5;

	//[Header("Vision")]
	//public float VIS_lol = 25;

	[Header("Abilities")]
	public float ABLYarmorDrainRange = 20;
	public float ABLYarmorDrainDPSEnemy = 9;
	public float ABLYarmorDrainDPSAlly = 3;
	public float ABLYarmorDrainGPSEnemy = 1; // How much armor is gained per second, per victim (total APS scales with number of victims)
	public float ABLYarmorDrainGPSAlly = 2; // How much armor is gained per second, per victim (total APS scales with number of victims)
	public float ABLYarmorDrainGPSBonusMult = 1; // How much armor is gained per second, per victim, per number of victims? (total APS scales with number of victims, squared)
	public float ABLYarmorDrainMaxVictims = 5;

	public float[] ABLYarmorRegenHPS = new float[] { 4, 8, 16, 16, 2 }; // Based on armor missing: 20% -> 4ps / 40% -> 8ps / 60% -> 16ps / 80% -> 16ps / 100% -> 2ps

	public float ABLYshieldProjectRangeUse = 20; // Max distance for cast
	public float ABLYshieldProjectRange = 30; // If distance exceeds this post-cast, shield is returned
	public float ABLYshieldProjectMaxPool = 200; // Size of shield
	public float ABLYshieldProjectOnGPS = 4; // How much shield pool is gained per second the shield is active
	public float ABLYshieldProjectOnGPSDelay = 5; // Delay after taking damage before shield pool gain begins while the shield is active
	public float ABLYshieldProjectOffGPS = 8; // How much shield pool is gained per second the shield is inactive
	public float ABLYshieldProjectOffGPSNegMult = 1.5f; // GPS multiplier if shield is negative

	public int ABLYswarmMaxUses = 3;
	public float ABLYswarmFirstUseSpeedMult = 0.75f;
	public int ABLYswarmDPS = 4;
	public int ABLYswarmInteractRadius = 5; // How close a swarm has to be to proc its damage reduction status / damage over time
	public float ABLYswarmFighterHealth = 20;

	public float ABLYhealFieldRange = 25;
	public float ABLYhealFieldAllyGPS = 5; // Fragile health gained per second by each ally
	public float ABLYhealFieldUserGPSMult = 2; // Multiplier of base GPS for user
	public float ABLYhealFieldConvertGPS = 5; // Fragile health exchanged into health per second
	public float ABLYhealFieldAllyGPSBonusMult = 0.02f; // plus this percent of max health
	public float ABLYhealFieldConvertDelay = 5; // Time it takes for fragile health to start transforming into health

	public int ABLYhealFieldResCost = 4; // Amount of resource points held by this ability while active
	public float ABLYhealFieldResTime = 5; // Delay to return resource points when the ability ends

	public float ABLYchainRange = 25; // Max distance for cast
	public float ABLYchainAllyMult = 0.667f; // Multiplier applied to velocity when adding to it the target
	public float ABLYchainEnemyMult = 0.667f;
	public float ABLYchainFlagshipMult = 0.5f; // Stacks with prior 2 multipliers (0.667 * 0.5 = 0.333)

	public float ABLYsuperlaserRangeTargeting = 60; // Max distance for cast and distance which the target must stay in during Superlaser targeting state
	public float ABLYsuperlaserCancelCDMult = 0.5f; // What ratio of cooldown is refunded if the target leaves the targeting range or the ability is cancelled manually
	public float ABLYsuperlaserCancelTime = 1f; // How long after initial cast do you have to wait before you can re-cast to cancel targeting
	public float ABLYsuperlaserDelay = 3.5f; // Delay before damage is dealt during ability can be interupted or range-cancelled
	public int ABLYsuperlaserInitStacks = 0; // Stacks the ability starts with
	public float ABLYsuperlaserStackDmgReq = 0.6f; // How much damage (by percentage of max health + max armor) has to be done to a unit to earn a stack from its death
	public float ABLYsuperlaserDmgBase = 200; // Base damage
	public float[] ABLYsuperlaserDmgByStacks = new float[] { -1, 50, 100, 150, 200}; // Damage added based on stacks: 0 -> cannot be activated / 1 -> 100 / 2 -> 200 / 3 -> 300 / 4 -> 400

	public float ABLY_statusMissileRangeUse = 40; // Max distance for cast
	public float ABLY_statusMissileRangeMissile = 60; // Max distance for before missile detonates
	public float ABLY_statusMissileMaxLifetime = 10; // How long the missile can exist before detonating
	public float ABLY_statusMissileVerticalOffset = 2; // How far above the unit should the missile try to detonate
	public float ABLY_statusMissileExplodeDist = 1.5f; // Distance from detonation point when missile should detonate
	public float ABLY_statusMissileDamage = 10; // Flat damage dealt once to targets caught in cloud
	public float ABLY_statusMissileDamageBonusMult = 0.05f; // Ratio of target's max health + max armor dealt once to targets caught in cloud

	public float ABLY_selfDestructRange = 25; // Radius for dealing damage
	public float ABLY_selfDestructDamage = 500; // Damage dealt
	public float ABLY_selfDestructDamageFlagMult = 0.33f; // Damage multiplier against flagship units
	public float ABLY_selfDestructDPSSelf = 10; // How much health per second is converted to fragile health while channeling
	public float ABLY_selfDestructSpeedMult = 1.25f; // Speed mult while channeling

	public float ABLY_ionMissileRangeUse = 40; // Max distance for cast
	public float ABLY_ionMissileCancelTime = 1f; // How long after initial cast do you have to wait before you can re-cast to cancel targeting
	public float ABLY_ionMissileRangeMissile = 60; // Max distance for before missile detonates
	public float ABLY_ionMissileMaxLifetime = 10; // How long the missile can exist before detonating
	public float ABLY_ionMissileDamage = 10; // Flat damage dealt on impact
	public float ABLY_ionMissileDamageBonusMult = 0.1f; // Ratio of target's current shields dealt on impact
	public float ABLY_ionMissileIonsFirst = 20f;
	public float ABLY_ionMissileIonsNext = 20f;
	public float ABLY_ionMissileArmorDmgToIons = 0.5f;
	public float ABLY_ionMissileDecayDelay = 10f;
	public float ABLY_ionMissileDecayLPS = 4;
	public float ABLY_ionMissileDecayCutoff = 10;
	public int ABLY_ionMissileMaxAmmo = 2;
	public int ABLY_ionMissileAmmoTick = 1;


	[Header("Statuses")]
	public float STATswarmResistMult = 0.1f; // How much of all incoming damage does each swarm protecting an ally absorb
	public float STATswarmResistMultSwarm = 1.0f; // (against enemy swarm damage)
	public int STATswarmResistMaxStacks = 3; // How many times can this damage absorption stack
	public float STATswarmResistTransferMult = 1.0f; // What ratio of absorbed damage is transferred to the absorbing ally swarms. The total damage is split between each of the absorbing swarms

	public float STAT_armorMeltDPS = 3; // Damage per second
	public float STAT_armorMeltAbsorbFlat = 1; // This replaces the constant part of the armor absorption limit formula
	public float STAT_armorMeltAbsorbScalingMult = 0.5f; // Multiplier on the scaling part of the armor absorption limit formula
}
