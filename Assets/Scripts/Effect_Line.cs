﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Line : MonoBehaviour
{
	[SerializeField]
	private LineRenderer lineMain;
	[SerializeField]
	private LineRenderer lineSecondary;

	public void SetEffectActive(int state)
	{
		SetEffectActive(state, Vector3.zero, Vector3.zero);
	}

		public void SetEffectActive(int state, Vector3 start, Vector3 end)
	{
		if (state == 1)
		{
			lineMain.gameObject.SetActive(true);
			lineSecondary.gameObject.SetActive(false);

			lineMain.SetPosition(0, start);
			lineMain.SetPosition(1, end);
		}
		else if (state == 2)
		{
			if (lineSecondary)
			{
				lineMain.gameObject.SetActive(false);
				lineSecondary.gameObject.SetActive(true);

				lineSecondary.SetPosition(0, start);
				lineSecondary.SetPosition(1, end);
			}
		}
		else
		{
			lineSecondary.gameObject.SetActive(false);
			lineMain.gameObject.SetActive(false);
		}
	} //SetEffectActive

	public void End()
	{
		Destroy(lineMain);
		Destroy(lineSecondary);
	}
}
