using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UIRules
{
	public Vector2 HPBoffset = new Vector2(0, 1);
	public float HPBbordColorThresh = 0.0001f;
	public float HPBvalUpdateTime = 0.2f;

	public float SELrotateSpeed = 15;
}
