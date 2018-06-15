using UnityEngine;
using System.Collections;

//Credit to Dave Hampson
public class FPSCounter : MonoBehaviour
{
	[SerializeField]
	private float LFPSresetTime = 7.0f;
	[SerializeField]
	private Color textColor = new Color(1f, 0f, 0f, 1f);
	[SerializeField]
	private int textSize = 15;

	private float deltaTime = 0.0f;
	private float lowestFPS = 1000f;
	private float LFPSresetCounter = 10.0f;
	

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

		Rect rect = new Rect(textSize, textSize, w, h - 10 - textSize);
		style.alignment = TextAnchor.UpperLeft;
		style.fontSize = textSize;
		style.normal.textColor = textColor;
		float msec = deltaTime * 1000.0f;
		float fps = 1.0f / deltaTime;

		if (fps < lowestFPS)
		{
			lowestFPS = fps;
		}

		string text = string.Format("{0:0.0} ms ({1:0.} fps), {2:0.} lowest (resetting in {3:0.0} s)", msec, fps, lowestFPS, LFPSresetCounter);
		GUI.Label(rect, text, style);
	}
}