using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UIRules
{
	public Vector2 HPBoffset = new Vector2(0, 1);
	public float HPBbordColorThresh = 0.0001f; // How close to zero does armor have to get before health becomes the border color
	public float HPBupdateTime = 0.2f;
	public float HPBblinkTime = 0.05f;

	public Vector2 BPBoffset = new Vector2(0, 3);

	public float SELrotateSpeed = 50;
}
