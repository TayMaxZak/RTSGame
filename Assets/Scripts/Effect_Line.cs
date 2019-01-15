using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Line : MonoBehaviour
{
	[SerializeField]
	private LineRenderer lineMain;
	[SerializeField]
	private LineRenderer lineSecondary;

	private bool mainOn = false;
	private bool secOn = false;

	public void SetEffectActive(int state)
	{
		SetEffectActive(state, Vector3.zero, Vector3.zero);
	}

	public void SetEffectActive(Vector3 start, Vector3 end)
	{
		SetEffectActive(1, Vector3.zero, Vector3.zero);
	} //SetEffectActive

	public void SetEffectActive(int state, Vector3 start, Vector3 end)
	{
		if (state == 1)
		{
			mainOn = true;
			secOn = false;

			lineMain.enabled = true;
			if (lineSecondary)
				lineSecondary.enabled = false;

			lineMain.SetPosition(0, start);
			lineMain.SetPosition(1, end);
		}
		else if (state == 2)
		{
			mainOn = true;
			secOn = true;

			if (lineSecondary)
			{
				lineMain.enabled = false;
				if (lineSecondary)
					lineSecondary.enabled = true;

				if (lineSecondary)
				{
					lineSecondary.SetPosition(0, start);
					lineSecondary.SetPosition(1, end);
				}
			}
		}
		else
		{
			mainOn = false;
			secOn = false;

			lineMain.enabled = false;
			if (lineSecondary)
				lineSecondary.enabled = false;
		}
	} //SetEffectActive

	public void End()
	{
		Destroy(lineMain);
		Destroy(lineSecondary);
		Destroy(gameObject);
	}

	public void SetVisible(bool visible)
	{
		lineMain.gameObject.SetActive(visible);
		if (lineSecondary)
		{
			lineSecondary.gameObject.SetActive(visible);
		}
	}
}
