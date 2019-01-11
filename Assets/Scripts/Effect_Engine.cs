using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Engine : MonoBehaviour, IHideable
{
	//private AudioSource audioSource;
	private float fireThresh = 0.334f; // Overwritten by GameRules
	[SerializeField]
	private ParticleSystem enginePrefab;
	[SerializeField]
	private Transform enginePos;
	private Transform[] enginePositions;

	private List<ParticleSystem> smokePSystems;
	private List<ParticleSystem> enginePSystems;

	private bool ended = false;

	void Awake()
	{
		//audioSource = GetComponent<AudioSource>();
		fireThresh = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules.HLTH_burnThresh;

		enginePSystems = new List<ParticleSystem>();

		if (enginePos)
		{
			Transform[] ePos = enginePos.GetComponentsInChildren<Transform>();
			enginePositions = new Transform[ePos.Length - 1];
			for (int i = 1; i < ePos.Length; i++)
			{
				enginePositions[i - 1] = ePos[i];
			}
		}
	}

	// Find all engine positions in children of enginePos root object
	void Start()
	{

	}


	public void UpdateEngineEffects(float engineStrength)
	{
		// TODO: Vary strength of effect
	}

	public void SetEngineActive(bool isActive)
	{
		if (ended)
			return;

		if (enginePSystems.Count == 0)
		{
			InitFire();
		}

		foreach (ParticleSystem firePS in enginePSystems)
		{
			if (isActive)
			{
				if (!firePS.isPlaying)
					firePS.Play();
			}
			else
			{
				if (firePS.isPlaying)
					firePS.Stop();
			}
		}
	}

	void InitFire()
	{
		foreach (Transform pos in enginePositions)
		{
			GameObject go = Instantiate(enginePrefab.gameObject, pos.position, pos.rotation);
			go.transform.SetParent(pos);
			enginePSystems.Add(go.GetComponent<ParticleSystem>());
		}
	}

	public void End()
	{
		enginePos.SetParent(null);
		SetEngineActive(false);
		ended = true;

		float duration = enginePrefab.main.duration;

		Destroy(enginePos.gameObject, duration);
		Destroy(gameObject, duration);
	}


	public void SetVisible(bool visible)
	{
		enginePos.gameObject.SetActive(visible);
	}
}

public interface IHideable
{
	void SetVisible(bool visible);
}