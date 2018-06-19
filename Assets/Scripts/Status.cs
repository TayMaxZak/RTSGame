using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StatusType
{
	SwarmShield,
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
		timeLeft -= delta * (1 / StatusUtils.Duration(statusType));
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
	public static float Duration(StatusType statType)
	{
		switch (statType)
		{
			case StatusType.SwarmShield:
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
}