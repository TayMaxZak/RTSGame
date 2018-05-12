using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone_Build : MonoBehaviour
{
	public BuildUnit buildUnit;
	[SerializeField]
	private GameObject previewModel;
	[SerializeField]
	private GameObject warpModel;
	[SerializeField]
	private GameObject spawnEffect;

	private float warpingTime;
	private float warpingAmount;
	private GameRules gameRules;

	void Start()
	{
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
		//warpingTime = gameRules.SPWNwarpTime;
		warpingAmount = warpModel.transform.localScale.z;
		warpModel.SetActive(false);
	}

	public void Build()
	{
		StartCoroutine(Building());
	}

	IEnumerator Building()
	{
		yield return new WaitForSeconds(buildUnit.buildTime - gameRules.SPWNeffectTime);
		Effect();
		yield return new WaitForSeconds(gameRules.SPWNeffectTime - gameRules.SPWNwarpTime);
		StartCoroutine(Warp());
		yield return new WaitForSeconds(gameRules.SPWNwarpTime);
		Finish();
	}

	void Effect()
	{
		previewModel.SetActive(false);
		if (spawnEffect)
			Instantiate(spawnEffect, transform.position, transform.rotation);
	}

	IEnumerator Warp()
	{
		
		warpModel.SetActive(true);
		yield return new WaitForSeconds(gameRules.SPWNwarpTime);
		warpModel.SetActive(false);
	}

	void Update()
	{
		if (warpModel.activeSelf)
		{
			warpingTime += Time.deltaTime / gameRules.SPWNwarpTime;
			warpModel.transform.localScale = new Vector3(1, 1, Mathf.Lerp(warpingAmount, 1, warpingTime));
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
