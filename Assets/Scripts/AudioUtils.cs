using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Audio;

public static class AudioUtils
{
	public static AudioSource PlayClipAt(AudioClip clip, Vector3 pos, AudioSource reference)
	{
		if (clip == null)
		{
			//Debug.Log("No AudioClip was passed in!");
			return null;
		}

		GameObject tempGO = new GameObject("TempAudio"); // create the temp object
		tempGO.transform.position = pos; // set its position
		AudioSource aSource = tempGO.AddComponent<AudioSource>();
		//var copy = aSource.GetCopyOf(reference);
		aSource.clip = clip; // define the clip

		// TODO: TEMP
		aSource.spatialBlend = reference.spatialBlend;
		aSource.minDistance = reference.minDistance;
		aSource.maxDistance = reference.maxDistance;
		aSource.rolloffMode = reference.rolloffMode;
		aSource.dopplerLevel = reference.dopplerLevel;
		aSource.spread = reference.spread;
		aSource.pitch = reference.pitch;
		aSource.volume = reference.volume;

		aSource.Play(); // start the sound
		MonoBehaviour.Destroy(tempGO, clip.length); // destroy object after clip duration
		return aSource; // return the AudioSource reference
	}

	public static AudioSource PlayClipAt(AudioClip clip, Vector3 pos)
	{
		if (clip == null)
		{
			//Debug.Log("No AudioClip was passed in!");
			return null;
		}

		GameObject tempGO = new GameObject("TempAudio"); // create the temp object
		tempGO.transform.position = pos; // set its position
		AudioSource aSource = tempGO.AddComponent<AudioSource>(); // add an audio source
		aSource.clip = clip; // define the clip
		aSource.Play(); // start the sound
		MonoBehaviour.Destroy(tempGO, clip.length); // destroy object after clip duration
		return aSource; // return the AudioSource reference
	}

	// Credit to @vexe over on Unity3D forums
	public static T GetCopyOf<T>(this Component comp, T other) where T : Component
	{
		Type type = comp.GetType();
		if (type != other.GetType()) return null; // type mis-match
		// TODO: FIX THIS
		////BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Default | BindingFlags.DeclaredOnly;
		BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
		PropertyInfo[] pinfos = type.GetProperties(flags);
		foreach (var pinfo in pinfos)
		{
			if (pinfo.CanWrite)
			{
				try
				{
					pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
				}
				catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
			}
		}
		FieldInfo[] finfos = type.GetFields(flags);
		foreach (var finfo in finfos)
		{
			finfo.SetValue(comp, finfo.GetValue(other));
		}
		return comp as T;
	}
}
