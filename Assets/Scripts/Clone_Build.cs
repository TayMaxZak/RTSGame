using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Clone_Build : MonoBehaviour
{
	[HideInInspector]
	public BuildUnit buildUnit;
	[SerializeField]
	private GameObject previewModel;
	[SerializeField]
	private GameObject warpModel;
	[SerializeField]
	private GameObject spawnEffect;

	[SerializeField]
	private float warpTime = 0.05f;
	[SerializeField]
	private float spawnEffectTime = 1;

	[SerializeField]
	private UI_ProgBar progBarPrefab;
	private UI_ProgBar progBar;
	private Vector2 progBarOffset;

	private float warpingTime;
	private float warpingAmount;

	

	private int buildUnitIndex;
	private int unitTeam;

	private float startTime;
	private float finishTime;

	private GameRules gameRules;

	void Start()
	{
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
		warpingAmount = warpModel.transform.localScale.z;
		warpModel.SetActive(false);
	}

	public void Build(int index, int team)
	{
		unitTeam = team;
		buildUnitIndex = index;

		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>(); // Grab copy of UI Manager
		progBar = Instantiate(progBarPrefab);
		progBar.transform.SetParent(uiManager.Canvas.transform, false);
		progBarOffset = uiManager.UIRules.BPBoffset;

		startTime = Time.time;
		finishTime = buildUnit.buildTime;

		if (gameRules.useTestValues)
			finishTime = finishTime * gameRules.TEST_timeMultBuild;

		UpdateUI();

		StartCoroutine(Building());
	}

	IEnumerator Building()
	{
		yield return new WaitForSeconds(finishTime - spawnEffectTime);
		Effect();
		yield return new WaitForSeconds(spawnEffectTime - warpTime);
		StartCoroutine(Warp());
		yield return new WaitForSeconds(warpTime);
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
		yield return new WaitForSeconds(warpTime);
		warpModel.SetActive(false);
	}

	void Update()
	{
		if (progBar)
			UpdateUI();

		if (warpModel.activeSelf)
		{
			warpingTime += Time.deltaTime / warpTime;
			warpModel.transform.localScale = new Vector3(1, 1, Mathf.Lerp(warpingAmount, 1, warpingTime));
		}
	}

	void UpdateUI()
	{
		Vector3 barPosition = new Vector3(transform.position.x + progBarOffset.x, transform.position.y + progBarOffset.y, transform.position.z + progBarOffset.x);
		Vector3 screenPoint = Camera.main.WorldToScreenPoint(barPosition);

		float dot = Vector3.Dot((barPosition - Camera.main.transform.position).normalized, Camera.main.transform.forward);
		if (dot < 0)
		{
			if (progBar.gameObject.activeSelf)
				progBar.gameObject.SetActive(false);
		}
		else
		{
			if (!progBar.gameObject.activeSelf)
				progBar.gameObject.SetActive(true);

			RectTransform rect = progBar.GetComponent<RectTransform>();
			rect.position = new Vector2(screenPoint.x, screenPoint.y);
			progBar.UpdateProgBar((Time.time - startTime) / finishTime);
		}
	}

	void Finish()
	{
		Unit unit = Instantiate(buildUnit.spawnObject, transform.position, transform.rotation).GetComponent<Unit>();
		if (unit)
		{
			unit.buildIndex = buildUnitIndex;
			unit.team = unitTeam;

		}

		Destroy(progBar.gameObject);
		Destroy(gameObject);
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
