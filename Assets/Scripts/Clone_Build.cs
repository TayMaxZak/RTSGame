using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone_Build : MonoBehaviour
{
	public BuildUnit buildUnit;
	[SerializeField]
	private GameObject previewEffect;
	[SerializeField]
	private GameObject warpEffect;

	private float warpingTime;
	private float warpingAmount;
	private GameRules gameRules;

	void Start()
	{
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
		//warpingTime = gameRules.SPWNwarpTime;
		warpingAmount = warpEffect.transform.localScale.z;
		warpEffect.SetActive(false);
	}

	public void Build()
	{
		StartCoroutine(Building());
	}

	IEnumerator Building()
	{
		yield return new WaitForSeconds(buildUnit.buildTime - gameRules.SPWNwarpTime);
		StartCoroutine(Warp());
		yield return new WaitForSeconds(gameRules.SPWNwarpTime);
		Finish();
	}

	IEnumerator Warp()
	{
		previewEffect.SetActive(false);
		warpEffect.SetActive(true);
		yield return new WaitForSeconds(gameRules.SPWNwarpTime);
		warpEffect.SetActive(false);
	}

	void Update()
	{
		if (warpEffect.activeSelf)
		{
			warpingTime += Time.deltaTime / gameRules.SPWNwarpTime;
			warpEffect.transform.localScale = new Vector3(1, 1, Mathf.Lerp(warpingAmount, 0, warpingTime));
		}
	}

	void Finish()
	{
		Instantiate(buildUnit.spawnObject, transform.position, transform.rotation);
		Destroy(gameObject);
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
