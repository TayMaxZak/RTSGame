using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UIRules
{
	[Header("Healthpool Bar")]
	public Vector2 HPB_offset = new Vector2(0, 1);
	public float HPB_borderColorThresh = 0.0001f; // How close to zero does armor have to get before health becomes the border color
	public float HPB_updateTime = 0.2f;
	public float HPB_blinkTime = 0.05f;
	public float HPB_ionBlinkTime = 0.1f;
	public int HPB_healthPerTick = 100;
	public int HPB_armorPerTick = 100;
	public int HPB_shieldPerTick = 100;
	[Header("Build Progress Bar")]
	public Vector2 BPB_offset = new Vector2(0, 3);
	[Header("Capture Progress Bar")]
	public Vector2 CPB_offset = new Vector2(0, 5);
	[Header("Selection Indicator")]
	public float SEL_rotateSpeed = 30;

	[Header("Ability Bars")]
	// ShieldProject //
	public float AB_SP_updateTime = 0.2f;
	public float AB_SP_blinkTime = 0.2f;

	[Header("Entity Stats")]
	public float ES_iconBState1Speed = 30f;
	public float ES_iconBState2Speed = -15f;

	[Header("Tooltip")]
	public float TT_appearTime = 1f;
	public float TT_audioPitchVariance = 0.02f;
}
