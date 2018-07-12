using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Container : MonoBehaviour
{
	[SerializeField]
	private ParticleSystem effectPrefab;
	[SerializeField]
	private Transform effectPosRoot;
	private Transform[] effectPositions;

	private List<ParticleSystem> firePSystems;

	void Start()
	{
		firePSystems = new List<ParticleSystem>();

		Transform[] fPos = effectPosRoot.GetComponentsInChildren<Transform>();
		effectPositions = new Transform[fPos.Length - 1];
		for (int i = 1; i < fPos.Length; i++)
		{
			effectPositions[i - 1] = fPos[i];
		}

		foreach (Transform pos in effectPositions)
		{
			GameObject go = Instantiate(effectPrefab.gameObject, pos.position, pos.rotation);
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
		effectPosRoot.SetParent(null);

		Util_Mover mover = effectPosRoot.GetComponent<Util_Mover>();
		if (mover)
			mover.SetVelocity(releaseVelocity);

		SetFireActive(false);

		Destroy(gameObject, effectPrefab.main.duration);
	}
}
