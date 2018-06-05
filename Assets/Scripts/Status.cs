using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum StatusType
{
	SwarmShield,
	CriticalBurnImmune
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
}

public static class StatusUtils
{
	public static float Duration(StatusType statType)
	{
		switch (statType)
		{
			case StatusType.SwarmShield:
				return 2;
			case StatusType.CriticalBurnImmune:
				return 1;
			default:
				return 1;
		}
	}
}