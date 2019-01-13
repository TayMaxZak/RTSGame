using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Point : MonoBehaviour, IHideable
{
	[SerializeField]
	private ParticleSystem mainPS;
	[SerializeField]
	private bool mainPSInstant;
	[SerializeField]
	private ParticleSystem secondaryPS;

	private bool ended = false;
	private bool mainOn = false;
	private bool secOn = false;

	public void SetEffectActive(bool state)
	{
		SetEffectActive(state, state);
	}

	public void SetEffectActive(bool state, bool secondaryState)
	{
		if (ended)
			return;

		mainOn = state;
		secOn = secondaryState;

		if (state)
		{
			if (!mainPS.isPlaying)
				mainPS.Play();
		}
		else
		{
			if (mainPS.isPlaying)
			{
				mainPS.Stop();
				if (mainPSInstant)
					mainPS.Clear();
			}
		}

		if (secondaryPS)
		{
			if (secondaryState)
			{
				if (!secondaryPS.isPlaying)
					secondaryPS.Play();
			}
			else if (secondaryPS.isPlaying)
			{
				secondaryPS.Stop();
			}
		}
	} //SetEffectActive

	public ParticleSystem GetMainPS()
	{
		return mainPS;
	}

	public void End()
	{
		SetEffectActive(false);
		ended = true;

		float duration = Mathf.Max(mainPS.main.duration, secondaryPS ? secondaryPS.main.duration : 0);
		
		Destroy(gameObject, duration);
	}

	public void SetVisible(bool visible)
	{
		mainPS.gameObject.SetActive(visible);
		if (secondaryPS)
			secondaryPS.gameObject.SetActive(visible);
		if (visible)
		{
			if (mainOn)
			{
				if (!mainPS.isPlaying)
					mainPS.Play();
			}
			if (secondaryPS && secOn)
			{
				if (!secondaryPS.isPlaying)
					secondaryPS.Play();
			}
		} // new state visible
	}
}
