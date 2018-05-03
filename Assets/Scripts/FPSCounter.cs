using UnityEngine;
using System.Collections;

//Credit to Dave Hampson
public class FPSCounter : MonoBehaviour
{
	float deltaTime = 0.0f;
	float lowestFPS = 1000f;
	public float LFPSresetTime = 10.0f;
	float LFPSresetCounter = 10.0f;

	void Start()
	{
		LFPSresetCounter = LFPSresetTime;
	}

	void Update()
	{
		deltaTime += (Time.deltaTime - deltaTime) * 0.1f;


		LFPSresetCounter -= Time.deltaTime;

		if (LFPSresetCounter <= 0)
		{
			lowestFPS = LFPSresetTime * 1000;
			LFPSresetCounter = LFPSresetTime;
		}
	}

	void OnGUI()
	{
		int w = Screen.width, h = Screen.height;

		GUIStyle style = new GUIStyle();

		Rect rect = new Rect(20, 20, w, h * 2 / 100);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = h * 2 / 75;
		style.normal.textColor = new Color(0.1f, 0.6f, 1f, 1.0f);
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;

		if (fps < lowestFPS)
		{
			//Debug.Log("new low (cur) " + lowestFPS + " (new) " + fps);
			lowestFPS = fps;
		}

		string text = string.Format("{0:0.0} ms ({1:0.} fps), {2:0.} lowest (resetting in {3:0.0} s)", msec, fps, lowestFPS, LFPSresetCounter);
		GUI.Label(rect, text, style);
	}
}