using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Container : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem firePrefab;
	[SerializeField]
	private Transform firePos;
	private Transform[] firePositions;

	private List<ParticleSystem> firePSystems;

	void Start()
	{
		firePSystems = new List<ParticleSystem>();

		Transform[] fPos = firePos.GetComponentsInChildren<Transform>();
		firePositions = new Transform[fPos.Length - 1];
		for (int i = 1; i < fPos.Length; i++)
		{
			firePositions[i - 1] = fPos[i];
		}

		foreach (Transform pos in firePositions)
		{
			GameObject go = Instantiate(firePrefab.gameObject, pos.position, pos.rotation);
			go.transform.SetParent(pos);
			firePSystems.Add(go.GetComponent<ParticleSystem>());
		}
	}

	void SetFireActive(bool isActive)
	{
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

	public void End()
	{
		End(Vector3.zero);
	}

	public void End(Vector3 releaseVelocity)
	{
		firePos.SetParent(null);

		Util_Mover mover = firePos.GetComponent<Util_Mover>();
		if (mover)
			mover.SetVelocity(releaseVelocity);

		SetFireActive(false);

		Destroy(gameObject, firePrefab.main.duration);
	}
}
