using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ability_Effect : MonoBehaviour
{
	private AudioSource audioSource;
	[SerializeField]
	private ParticleSystem pS;
	[SerializeField]
	private ParticleSystem secondaryPS;

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
		//Debug.Log("STATE IS " + state);
		if (state)
		{
			if (!audioSource.isPlaying)
				audioSource.Play();
			if (!pS.isPlaying)
				pS.Play();
		}
		else
		{
			if (audioSource.isPlaying)
				audioSource.Stop();
			if (pS.isPlaying)
				pS.Stop();
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
}
