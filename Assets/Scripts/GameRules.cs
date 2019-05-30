using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameRules
{
	[Header("Testing")]
	public bool useTestValues = false;
	public float TEST_timeMultCooldown = 1;
	public float TEST_timeMultBuild = 0.2f;
	public float TEST_initHPAdd = 0;
	public float TEST_initHPMult = 1;
	public float TEST_spawnRangeMult = 3;

	[Header("Layer Masks")]
	public LayerMask entityLayerMask;
	public LayerMask targetLayerMask;
	public LayerMask collisionLayerMask;
	public LayerMask gridLayerMask;

	[Header("Armor")]
	public float ARM_absorbFlat = 5; // How much armor absorb is guaranteed
	public float ARM_absorbScaling = 15; // How much armor absorb is added based on current percentage of armor
	public float ARM_rangeMin = 20; // Range past which armor range resist begins
	public float ARM_rangeMax = 60; // Range past which armor range resist is at full effect
	public float ARM_rangeMult = 0.8f; // Overall range resist multiplier

	[Header("Health")]
	public float HLTH_burnThresh = 0.2001f; // How low does health drop before it starts automatically burning away
	public float HLTH_burnMin = 2; // Burn damage per second
	public float HLTH_burnMax = 3; // Burn damage per second

	[Header("Wrecks")]
	public float WRCK_fallSpeedMax = 10;
	public float WRCK_fallSpeedAccel = 3;
	public float WRCK_lifetime = 10; // How long the wreck lasts before dissapearing. Also how long before resources begin to be recovered from a dead unit
	public float WRCK_massHealthMult = 1; // When calculating mass, how much should max health count for ("mass" determines damage dealt on collision with a unit)
	public float WRCK_massArmorMult = 0.5f; // When calculating mass, how much should max armor count for
	public float WRCK_initialVelMult = 0.5f; // When a unit dies, this ratio of its current horizontal velocity is transferred to its wreck
	public float WRCK_collisionSpeedPenalty = 0.8f; // If it hits something, how much speed should it lose

	[Header("Flagship")]
	public float FLAG_shieldMaxPool = 300; // Hit points of shield
	public float FLAG_shieldRegenDelay = 10; // Delay after taking damage before shield begins to regen
	public float FLAG_shieldRegenGPS = 5; // Shield hit points gained per second

	[Header("Objectives")]
	public float OBJV_captureRange = 25; // Range around objective which counts toward capture
	public float OBJV_captureTime = 20; // Time to go from neutral state to fully controlled state, given a contribution of 1
	public float OBJV_captureAddPerUnitMult = 1; // How much each unit contributes,
	public float OBJV_captureAddMax = 3; // Total capped by this amount

	[Header("Resources")]
	public int RES_startingResPoints = 20; // What does each commander start with
	public int RES_objMinorResPoints = 5; // What does each minor relay grant
	public int RES_objMajorResPoints = 10; // What does each major relay grant
	public float RES_reclaimTime = 5; // How long it takes to convert a raw material point into a resource point

	[Header("Spawning")]
	public float SPWN_flagshipRadius = 50; // Radius around flagship where units can be spawned

	[Header("Audio")]
	public float AUD_randomPitchVariance = 0.05f; // Audio pitch variation for each clip instance
	public float AUD_enginePitchVariance = 0.05f; // By how much engine pitch goes up from moving faster

	[Header("Projectiles")]
	public float PRJ_maxTimeAlive = 4; // How long each projectile lives
	public float PRJ_hitOffset = 0.1f; // When hitting an object, a projectile always detonates this back far from the hit point
	public float PRJ_friendlyFireCheckRangeMult = 1; // When testing for the potential of friendly fire, how far ahead do we want to check? This is a multiplier on the turret's base range

	[Header("Damage")]
	public float DMG_ffDamageMult = 0.5f; // If we do hit an ally, do reduced damage because it was an accidental glancing hit
	public float DMG_ffDamageMultSplash = 0.25f; // If we do hit an ally, do reduced damage because splash damage friendly fire is almost unavoidable

	[Header("Movement")]
	public float MOV_abilityAimingRSMult = 1; // While a unit is being aimed by an ability, what should be the multiplier on its rotation speed
	public int MOV_heightCount = 10; // How many different heights can units move on
	public int MOV_heightSpacing = 5; // How far apart is each height

	[Header("Tickrates")]
	public int TIK_statusRate = 5; // How many times per second should statuses update
	public int TIK_fighterInteractRate = 5; // How many times per second should fighters interact with their target
	public int TIK_fragileHealthConvertRate = 5; // How many times per second should fragile health transform into normal health
	public int TIK_ionDecayRate = 5; // How many times per second should ions decay
	public int TIK_radarOverlapRate = 5; // How many times per second should radar update

	//[Header("Vision")]
	//public float VIS_lol = 25;

	[Header("Abilities")]
	[Header("Armor Drain")]
	public float ABLY_armorDrainRange = 20;
	public float ABLY_armorDrainDPSEnemy = 9;
	public float ABLY_armorDrainDPSAlly = 3;
	public float ABLY_armorDrainGPSEnemy = 6; // How much armor is gained per second, per victim (total APS scales with number of victims)
	public float ABLY_armorDrainGPSAlly = 2; // How much armor is gained per second, per victim (total APS scales with number of victims)
	public float ABLY_armorDrainGPSBonusMult = 1; // How much armor is gained per second, per victim, per number of victims? (total APS scales with number of victims, squared)
	public float ABLY_armorDrainMaxVictims = 5;

	//public float[] ABLY_armorRegenHPS = new float[] { 4, 8, 16, 16, 2 }; // Based on armor missing: 20% -> 4ps / 40% -> 8ps / 60% -> 16ps / 80% -> 16ps / 100% -> 2ps

	[Header("Shield Project")]
	public float ABLY_shieldProjectRangeUse = 20; // Max distance for cast
	public float ABLY_shieldProjectRange = 30; // If distance exceeds this post-cast, shield is returned
	public float ABLY_shieldProjectMaxPool = 200; // Hitpoints of shield
	public float ABLY_shieldProjectOnGPS = 4; // How much shield pool is gained per second the shield is active
	public float ABLY_shieldProjectOnGPSDelay = 5; // Delay after taking damage before regen begins while the shield is active
	public float ABLY_shieldProjectOffGPS = 8; // How much shield pool is gained per second the shield is inactive
	public float ABLY_shieldProjectOffGPSNegMult = 1.5f; // GPS multiplier if shield is negative

	[Header("Swarm")]
	public int ABLY_swarmMaxUses = 3;
	public float ABLY_swarmFirstUseSpeedMult = 0.75f; // After using the ability once and until it is no longer usable, the carrier is encumbered with a slow
	public int ABLY_swarmDPS = 4;
	public int ABLY_swarmInteractRadius = 5; // How close a swarm has to be to proc its damage reduction status / damage over time
	public float ABLY_swarmFighterHealth = 20;

	[Header("Heal Field")]
	public float ABLY_healFieldRange = 25;
	public float ABLY_healFieldAllyGPS = 5; // Fragile health gained per second by each ally
	public float ABLY_healFieldUserGPSMult = 1.5f; // Multiplier of base GPS for user
	public float ABLY_healFieldConvertGPS = 5; // Fragile health exchanged into health per second
	public float ABLY_healFieldAllyGPSBonusMult = 0.02f; // plus this percent of max health
	public float ABLY_healFieldConvertDelay = 5; // Time it takes for fragile health to start transforming into health
	public int ABLY_healFieldResCost = 4; // Amount of resource points held by this ability while active
	public float ABLY_healFieldResTime = 5; // Delay to return resource points when the ability ends

	[Header("Chain")]
	public float ABLY_chainRange = 25; // Max distance for cast
	public float ABLY_chainAllyMult = 0.667f; // Multiplier applied to velocity when adding to it the target
	public float ABLY_chainEnemyMult = 0.667f;
	public float ABLY_chainFlagshipMult = 0.5f; // Stacks with prior 2 multipliers (ex. 0.667 * 0.5 = 0.333)

	[Header("Superlaser")]
	public float ABLY_superlaserRangeTargeting = 60; // Max distance for cast and distance which the target must stay in during Superlaser targeting state
	public float ABLY_superlaserCancelCDMult = 0.5f; // What ratio of cooldown is refunded if the target leaves the targeting range or the ability is cancelled manually
	public float ABLY_superlaserCancelTime = 1; // How long after initial cast do you have to wait before you can re-cast to cancel targeting
	public float ABLY_superlaserDelay = 3.5f; // Delay before damage is dealt during ability can be interupted or range-cancelled
	public int ABLY_superlaserInitStacks = 0; // Stacks the ability starts with
	public float ABLY_superlaserStackDmgReq = 0.6f; // How much damage (by percentage of max health + max armor) has to be done to a unit to earn a stack from its death
	public float ABLY_superlaserDmgBase = 200; // Base damage
	public float[] ABLY_superlaserDmgByStacks = new float[] { -1, 50, 100, 150, 200}; // Damage added based on stacks: 0 -> cannot be activated / 1 -> 100 / 2 -> 200 / 3 -> 300 / 4 -> 400

	[Header("Status Missile")]
	public float ABLY_statusMissileRangeUse = 40; // Max distance for cast
	public float ABLY_statusMissileRangeMissile = 60; // Max distance for before missile detonates
	public float ABLY_statusMissileMaxLifetime = 10; // How long the missile can exist before detonating
	public float ABLY_statusMissileVerticalOffset = 2; // How far above the unit should the missile try to detonate
	public float ABLY_statusMissileExplodeDist = 1.5f; // Distance from detonation point when missile should detonate
	public float ABLY_statusMissileDamage = 10; // Flat damage dealt once to targets caught in cloud
	public float ABLY_statusMissileDamageBonusMult = 0.02f; // Ratio of target's max health + max armor dealt once to targets caught in cloud

	[Header("Self Destruct")]
	public float ABLY_selfDestructRange = 25; // Radius for dealing damage
	public float ABLY_selfDestructDamage = 500; // Damage dealt
	public float ABLY_selfDestructDamageFlagMult = 0.33f; // Damage multiplier against flagship units
	public float ABLY_selfDestructDPSSelf = 10; // How much health per second is converted to fragile health while channeling
	public float ABLY_selfDestructSpeedMult = 1.25f; // Speed mult while channeling

	[Header("Ion Missile")]
	public float ABLY_ionMissileRangeUse = 40; // Max distance for cast
	public float ABLY_ionMissileCancelTime = 1; // How long after initial cast do you have to wait before you can re-cast to cancel targeting
	public float ABLY_ionMissileRangeMissile = 80; // Max distance for before missile detonates
	public float ABLY_ionMissileMaxLifetime = 10; // How long the missile can exist before detonating
	public float ABLY_ionMissileDamage = 10; // Flat damage dealt on impact
	public float ABLY_ionMissileDamageBonusMult = 20; // Damage multiplier against shields
	public float ABLY_ionMissileDamageBonusMultFlagship = 10; // Damage multiplier reduced against flagship shields
	public float ABLY_ionMissileIonsFirst = 20; // How many ions are added to a unit with no ions
	public float ABLY_ionMissileIonsNext = 20; // How many ions are added to a unit with ions
	public float ABLY_ionMissileArmorDmgToIons = 0.5f; // At what rate is damage dealt to armor converted to ions (percent to percent with this multiplier)
	public float ABLY_ionMissileDecayDelay = 10; // Time after last taking damage before ions begin to decay
	public float ABLY_ionMissileDecayLPS = 4; // How many ions are decayed per second
	public float ABLY_ionMissileDecayCutoff = 5; // If ion count drops below this number, it is rounded down to zero
	public int ABLY_ionMissileMaxAmmo = 2; // How many missiles are stored at once
	public int ABLY_ionMissileAmmoTick = 1; // How many missiles are restored per reload

	[Header("Modes")]
	public float ABLY_modeSpeedMult = 0.5f; // While a mode is active, the unit's movement speed is multiplied by this amount

	public float ABLY_shieldModeMaxPool = 40; // Hitpoints of shield
	public float ABLY_shieldModeRegenDelay = 0.25f; // Delay after taking damage before regen starts
	public float[] ABLY_shieldModeRegenGPS = new float[] { 5, 10, 10, 5, 5, 5 }; // Based on shields missing: 20% -> 8ps / 40% -> 12ps / 60% -> 12ps / 80% -> 8ps / 100% -> 8ps / over 100& -> 4ps

	[Header("Radar")]
	public float ABLY_radarPeriod = 20; // Time between pulses
	public float ABLY_radarPulseRange = 80; // How big the sphere gets
	public float ABLY_radarPulseTime = 3; // How long it takes the sphere to get to its biggest size

	[Header("Radar")]
	public float ABLY_bombsLockOnTime = 1.5f; // How long it takes to lock on
	public float ABLY_bombsKeepTargetTime = 1.5f; // How long it takes to lock on
	public int ABLY_bombsStacks = 3; // How many bombing runs total
	public int ABLY_bombsCount = 6; // How many bombs dropped per use
	public float ABLY_bombsDropTime = 1.5f; // How long does it take to drop all the bombs
	public float ABLY_bombsDamage = 30; // How big the sphere gets
	public float ABLY_bombsDamageBonusMult = 0.05f; // How long it takes the sphere to get to its biggest size


	[Header("Statuses")]
	public float STATswarmResistMult = 0.1f; // How much of all incoming damage does each swarm protecting an ally absorb
	public float STATswarmResistMultSwarm = 1; // (against enemy swarm damage)
	public int STATswarmResistMaxStacks = 3; // How many times can this damage absorption stack
	public float STATswarmResistTransferMult = 1; // What ratio of absorbed damage is transferred to the absorbing ally swarms. The total damage is split between each of the absorbing swarms

	public float STAT_armorMeltDPS = 3; // Damage per second
	public float STAT_armorMeltAbsorbFlat = 1; // This replaces the constant part of the armor absorption limit formula
	public float STAT_armorMeltAbsorbScalingMult = 0.5f; // Multiplier on the scaling part of the armor absorption limit formula
}
