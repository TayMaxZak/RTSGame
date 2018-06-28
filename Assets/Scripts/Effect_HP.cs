using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_HP : MonoBehaviour
{
	//private AudioSource audioSource;
	[SerializeField]
	private float smokeThresh = 0.667f;
	[SerializeField]
	private ParticleSystem smokePrefab;
	[SerializeField]
	private Transform smokePos;
	[SerializeField]
	private Transform[] smokePositions;

	private float fireThresh = 0.334f; // Overwritten by GameRules
	[SerializeField]
	private ParticleSystem firePrefab;
	[SerializeField]
	private Transform firePos;
	private Transform[] firePositions;


	private List<ParticleSystem> smokePSystems;
	private List<ParticleSystem> firePSystems;

	void Awake()
	{
		//audioSource = GetComponent<AudioSource>();
	}

	void Start()
	{
		fireThresh = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules.HLTHthreshBurn;

		smokePSystems = new List<ParticleSystem>();
		firePSystems = new List<ParticleSystem>();

		if (smokePos)
		{
			Transform[] sPos = smokePos.GetComponentsInChildren<Transform>();
			smokePositions = new Transform[sPos.Length - 1];
			for (int i = 1; i < sPos.Length; i++)
			{
				smokePositions[i - 1] = sPos[i];
			}
		}

		if (firePos)
		{
			Transform[] fPos = firePos.GetComponentsInChildren<Transform>();
			firePositions = new Transform[fPos.Length - 1];
			for (int i = 1; i < fPos.Length; i++)
			{
				firePositions[i - 1] = fPos[i];
			}
		}
	}

	public void UpdateHealthEffects(float healthAmount)
	{
		if (healthAmount < smokeThresh)
			SetSmokeActive(true);
		else
			SetSmokeActive(false);

		if (healthAmount < fireThresh)
			SetFireActive(true);
		else
			SetFireActive(false);
	}

	void SetSmokeActive(bool isActive)
	{
		if (smokePSystems.Count == 0)
			InitSmoke();

		foreach (ParticleSystem smokePS in smokePSystems)
		{
			if (isActive)
			{
				if (!smokePS.isPlaying)
					smokePS.Play();
			}
			else
			{
				if (smokePS.isPlaying)
					smokePS.Stop();
			}
		}
	}

	void InitSmoke()
	{
		foreach (Transform pos in smokePositions)
		{
			GameObject go = Instantiate(smokePrefab.gameObject, pos.position, pos.rotation);
			go.transform.SetParent(pos);
			smokePSystems.Add(go.GetComponent<ParticleSystem>());
		}
	}

	void SetFireActive(bool isActive)
	{
		if (firePSystems.Count == 0)
			InitFire();

		foreach (ParticleSystem firePS in firePSystems)
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
		foreach (Transform pos in firePositions)
		{
			GameObject go = Instantiate(firePrefab.gameObject, pos.position, pos.rotation);
			go.transform.SetParent(pos);
			firePSystems.Add(go.GetComponent<ParticleSystem>());
		}
	}

	// TODO: Bugged, has chance to fail. Possibly bugged on Unit end rather than Effect_HP
	public void End()
	{
		firePos.SetParent(null);
		SetFireActive(false);


		//float duration = Mathf.Max(smokePrefab.main.duration, firePrefab.main.duration);
		//SetSmokeActive(false);

		float duration = 5;
		Destroy(gameObject, duration);
	}
}
