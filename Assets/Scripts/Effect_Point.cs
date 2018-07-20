using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Point : MonoBehaviour
{
	private AudioSource audioSource;
	[SerializeField]
	private ParticleSystem mainPS;
	[SerializeField]
	private bool mainPSInstant;
	[SerializeField]
	private ParticleSystem secondaryPS;

	private bool ended = false;

	void Awake()
	{
		audioSource = GetComponent<AudioSource>();
	}

	public void SetEffectActive(bool state)
	{
		SetEffectActive(state, state);
	}

	public void SetEffectActive(bool state, bool secondaryState)
	{
		if (ended)
			return;

		if (state)
		{
			if (audioSource)
			{
				if (!audioSource.isPlaying)
					audioSource.Play();
			}

			if (!mainPSInstant)
			{
				if (!mainPS.isPlaying)
					mainPS.Play();
			}
			else
			{
				mainPS.gameObject.SetActive(true);
			}
		}
		else
		{
			if (audioSource)
			{;
				if (audioSource.isPlaying)
					audioSource.Stop();
			}

			if (!mainPSInstant)
			{
				if (mainPS.isPlaying)
					mainPS.Stop();
			}
			else
			{
				mainPS.gameObject.SetActive(false);
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

	public AudioSource GetAudioSource()
	{
		return audioSource;
	}

	public void End()
	{
		SetEffectActive(false);
		ended = true;

		float duration = Mathf.Max(mainPS.main.duration, secondaryPS ? secondaryPS.main.duration : 0);;
		
		Destroy(gameObject, duration);
	}
}
