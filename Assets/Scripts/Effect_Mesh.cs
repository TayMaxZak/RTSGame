using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Mesh : MonoBehaviour, IHideable
{
	[SerializeField]
	private AudioSource audioSource;
	private bool audioPreviouslyPlaying;

	[SerializeField]
	private MeshRenderer mesh;

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
			mesh.enabled = true;

			if (audioSource)
			{
				if (!audioSource.isPlaying)
					audioSource.Play();
			}
		}
		else
		{
			mesh.enabled = false;

			if (audioSource)
			{
				if (audioSource.isPlaying)
					audioSource.Stop();
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

		//float duration = Mathf.Max(mainPS.main.duration, secondaryPS ? secondaryPS.main.duration : 0); ;

		Destroy(gameObject, 0);
	}

	public void SetVisible(bool visible)
	{
		mesh.gameObject.SetActive(visible);
		// Because we lose the playing/stopped state of an audio source when we disable it, we have to store its state
		bool previouslyEnabled = audioSource.gameObject.activeSelf;
		if (!visible)
			audioPreviouslyPlaying = audioSource.isPlaying;
		audioSource.gameObject.SetActive(visible);
		// Recall whether or not we have to restart the audio loop
		if (audioPreviouslyPlaying && !previouslyEnabled)
			audioSource.Play();
	}
}
