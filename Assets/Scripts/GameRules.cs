using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameRules
{
	public float ARMabsorbFlat = 20; // How much armor absorb is guarenteed
	public float ARMabsorbMax = 15; // How much armor absorb is added based on current percentage of armor
	public float ARMrangeMin = 25f; // Range past which armor range resist begins
	public float ARMrangeMax = 125f; // Range past which armor range resist is at full effect
	public float ARMrangeMult = 0.5f; // Overall range resist multiplier

	public float HLTHthreshBurn = 0.201f; // How low does health drop before it starts automatically burning away
	public float HLTHburnMin = 2; // Burn damage per second
	public float HLTHburnMax = 3;

	public float ABLYarmorDrainRange = 20;
	public float ABLYarmorDrainDPSEnemy = 10;
	public float ABLYarmorDrainDPSAlly = 5;
	public float ABLYarmorDrainHPS = 2;
	public float ABLYarmorDrainMaxVictims = 5;

	public float[] ABLYarmorRegenHPS = new float[] { 4, 8, 16, 16, 2 }; // 2 4 8 8 1

	public float SPWNeffectTime = 1f; // How long before the spawn should the spawn effect start
	public float SPWNwarpTime = 0.05f; // How long the actual warping of the model takes

	public float AUDpitchVariance = 0.05f;

	public float PRJmaxTimeAlive = 9f;
	public float PRJhitOffset = 0.05f; // When hitting an object, a projectile always detonates this far from the hit point

	public LayerMask entityLayerMask;
	public LayerMask gridLayerMask;
}
