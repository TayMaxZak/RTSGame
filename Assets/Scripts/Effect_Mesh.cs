using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Mesh : MonoBehaviour
{
	private AudioSource audioSource;
	[SerializeField]
	private MeshRenderer mesh;

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
}
