using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioEffect_Loop : MonoBehaviour, IHideable
{
	[SerializeField]
	private AudioSource audioSource;
	private bool audioPreviouslyPlaying;

	private bool ended = false;

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
			if (!audioSource.isPlaying)
			{
				if (audioSource.isActiveAndEnabled)
					audioSource.Play();
				audioPreviouslyPlaying = true;
			}
		}
		else
		{
			if (audioSource.isPlaying)
			{
				if (audioSource.isActiveAndEnabled)
					audioSource.Stop();
				audioPreviouslyPlaying = false;
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

		//float duration = Mathf.Max(mainPS.main.duration, secondaryPS ? secondaryPS.main.duration : 0);

		Destroy(gameObject, 0);
	}

	public void SetVisible(bool visible)
	{
		// Because we lose the playing/stopped state of an audio source when we disable it, we have to store its state
		bool previouslyEnabled = audioSource.gameObject.activeSelf;
		//if (!visible)
		//	audioPreviouslyPlaying = audioSource.isPlaying;
		audioSource.gameObject.SetActive(visible);
		// Recall whether or not we have to restart the audio loop
		if (audioPreviouslyPlaying && !previouslyEnabled)
			audioSource.Play();
	}
}
