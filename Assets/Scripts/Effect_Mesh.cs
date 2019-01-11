using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Effect_Mesh : MonoBehaviour, IHideable
{
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
		}
		else
		{
			mesh.enabled = false;
		}
	} //SetEffectActive

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
	}
}
