﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_AbilBar_SpawnSwarm : UI_Bar
{
	[SerializeField]
	private Image[] fighterGroupIcons;

	void UpdateFighterGroup(int index, int number)
	{
		Image currentIcon = fighterGroupIcons[index];

		Transform[] fighterIcons = currentIcon.GetComponentsInChildren<Transform>(true);

		int offset = 2;
		for (int i = offset; i < fighterIcons.Length - 1; i++)
		{
			if (i - offset < number)
				fighterIcons[i].gameObject.SetActive(true);
			else
				fighterIcons[i].gameObject.SetActive(false);
		}


		Transform deadIcon = fighterIcons[fighterIcons.Length - 1];
		if (number <= 0)
		{
			deadIcon.gameObject.SetActive(true);
			return;
		}
		else
			deadIcon.gameObject.SetActive(false);
	}

	public void SetFighterCounts(List<int> fighterCounts)
	{
		for (int i = 0; i < fighterGroupIcons.Length; i++)
		{
			if (i < fighterCounts.Count)
			{
				fighterGroupIcons[i].gameObject.SetActive(true);
				UpdateFighterGroup(i, fighterCounts[i]);
			}
			else
				fighterGroupIcons[i].gameObject.SetActive(false);
		}
	}
}
