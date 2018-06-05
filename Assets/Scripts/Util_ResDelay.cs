using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util_ResDelay : MonoBehaviour
{
	public void GiveResAfterDelay(int amount, float delay, int team)
	{
		StartCoroutine(ResDelayCoroutine(amount, delay, team));
	}

	IEnumerator ResDelayCoroutine(int a, float d, int t)
	{
		yield return new WaitForSeconds(d);
		GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().Commanders[t].GiveRes(a);
	}

	public void GiveRecAfterDelay(int amount, float delay, int team)
	{
		StartCoroutine(RecDelayCoroutine(amount, delay, team));
	}

	IEnumerator RecDelayCoroutine(int a, float d, int t)
	{
		yield return new WaitForSeconds(d);
		GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().Commanders[t].GiveRec(a);
	}
}
