using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util_ResDelay : MonoBehaviour
{
	void Awake()
	{
		name = "Util_ResDelay"; // This object was created without a name in code
	}

	public void GiveResAfterDelay(int amount, float delay, int team)
	{
		StartCoroutine(ResDelayCoroutine(amount, delay, team));
	}

	IEnumerator ResDelayCoroutine(int a, float d, int t)
	{
		yield return new WaitForSeconds(d);
		GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GetCommander(t).GiveResources(a);
		Remove();
	}

	public void GiveRecAfterDelay(int amount, float delay, int team)
	{
		StartCoroutine(RecDelayCoroutine(amount, delay, team));
	}

	IEnumerator RecDelayCoroutine(int a, float d, int t)
	{
		yield return new WaitForSeconds(d);
		GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GetCommander(t).GiveReclaims(a);
		Remove();
	}

	void Remove()
	{
		Destroy(gameObject);
	}
}
