﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StatusType
{
	SwarmResist,
	CriticalBurnImmune,
	SpawnSwarmSpeedNerf,
	SuperlaserMark
}

[System.Serializable]
public class Status
{
	public GameObject from;
	public StatusType statusType;
	[System.NonSerialized]
	private float timeLeft = 1;

	public Status(GameObject g, StatusType statType)
	{
		from = g;
		statusType = statType;
		timeLeft = 1;
	}

	public bool UpdateTimeLeft(float delta)
	{
		timeLeft -= delta * (1 / StatusUtils.GetDuration(statusType));
		if (timeLeft < 0)
			return false;
		else
			return true;
	}

	public void RefreshTimeLeft()
	{
		timeLeft = 1;
	}

	public float GetTimeLeft()
	{
		return timeLeft;
	}

	public void SetTimeLeft(float time)
	{
		timeLeft = time;
	}

	public void AddTimeLeft(float time)
	{
		timeLeft += time;
	}
}

public static class StatusUtils
{
	public static float GetDuration(StatusType statType)
	{
		switch (statType)
		{
			case StatusType.SwarmResist:
				return 2;
			default:
				return 1;
		}
	}

	public static bool ShouldCountDownDuration(StatusType statType)
	{
		switch (statType)
		{
			case StatusType.SpawnSwarmSpeedNerf:
				return false;
			case StatusType.SuperlaserMark:
				return false;
			default:
				return true;
		}
	}

	public static bool ShouldStackDuration(StatusType statType)
	{
		switch (statType)
		{
			case StatusType.SuperlaserMark:
				return true;
			default:
				return false;
		}
	}

	public static string GetDisplayName(StatusType statType)
	{
		switch (statType)
		{
			case StatusType.SwarmResist:
				return "Fighter Support";
			case StatusType.CriticalBurnImmune:
				return "Burn Immune";
			case StatusType.SpawnSwarmSpeedNerf:
				return "Hangars open, engines on low power";
			case StatusType.SuperlaserMark:
				return "Hellrazor Mark";
			default:
				return "default";
		}
	}

	public static bool ShouldDisplay(StatusType statType)
	{
		switch (statType)
		{
			case StatusType.SwarmResist:
				return true;
			case StatusType.SpawnSwarmSpeedNerf:
				return true;
			default:
				return false;
		}
	}

	/// <summary>
	/// [0] is bkg color, [1] is icon color
	/// </summary>
	/// <param name="statType"></param>
	/// <returns></returns>
	public static Color[] GetDisplayColors(StatusType statType)
	{
		switch (statType)
		{
			case StatusType.SwarmResist:
				return new Color[] { new Color32(0x90, 0x11, 0x11, 0xFF), new Color32(0xFF, 0x70, 0x88, 0xFF) };
			case StatusType.SpawnSwarmSpeedNerf:
				return new Color[] { new Color32(0x90, 0x11, 0x11, 0xFF), new Color32(0xFF, 0x70, 0x88, 0xFF) };
			default:
				return new Color[0];
		}
	}

	public static Sprite GetDisplayIcon(StatusType type)
	{
		Sprite sprite = Resources.Load<Sprite>("IconStatus_" + type);
		if (sprite)
			return sprite;
		else
			return Resources.Load<Sprite>("IconEmpty");
	}
}