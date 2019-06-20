using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Build_Incoming : NetworkBehaviour
{
	//[HideInInspector]
	private BuildUnit buildUnit;
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

	
	[SyncVar]
	private int buildUnitIndex = -1;
	[SyncVar]
	private int unitTeam = -1;

	private float startTime;
	private float finishTime;

	private GameRules gameRules;
	private Multiplayer_Manager multManager;

	void Awake()
	{
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
		warpingAmount = warpModel.transform.localScale.z;
		warpModel.SetActive(false);

		multManager = GameObject.FindGameObjectWithTag("MultiplayerManager").GetComponent<Multiplayer_Manager>();
	}

	public void Init(int index, int team)
	{
		Debug.Log("Build fields initialized");

		unitTeam = team;
		buildUnitIndex = index;
	}

	void Start()
	{
		if (buildUnitIndex >= 0)
		{
			Setup();
			StartCoroutine(Building());
		}
		else
			Debug.LogWarning("Could not start build");
	}

	void Setup()
	{
		Debug.Log("Build set up");

		// Build unit and time
		Manager_Game gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>(); // Grab copy of UI Manager
		buildUnit = gameManager.GetCommander(unitTeam).GetBuildUnit(buildUnitIndex);

		startTime = Time.time;
		finishTime = buildUnit.buildTime;

		if (gameRules.useTestValues)
			finishTime = finishTime * gameRules.TEST_timeMultBuild;

		// UI
		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>(); // Grab copy of UI Manager
		progBar = Instantiate(progBarPrefab);
		progBar.transform.SetParent(uiManager.Canvas.transform, false);
		progBarOffset = uiManager.UIRules.BPB_offset;

		UpdateUI();
	}

	IEnumerator Building()
	{
		yield return new WaitForSeconds(finishTime - spawnEffectTime);
		CreateSpawnEffect();
		yield return new WaitForSeconds(spawnEffectTime - warpTime);
		StartCoroutine(Warp());
		yield return new WaitForSeconds(warpTime);
		Finish();
	}

	void CreateSpawnEffect()
	{
		Debug.Log("Creating spawn effect");

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
		Debug.Log("Build finished");

		if (isServer)
		{
			CmdSpawnUnit();
			Destroy(gameObject);
		}
	}

	void OnDestroy()
	{
		Destroy(progBar.gameObject); // Clean up
	}

	[Command]
	void CmdSpawnUnit()
	{
		GameObject go = Instantiate(buildUnit.spawnObject, transform.position, transform.rotation);
		Unit unit = go.GetComponent<Unit>();
		unit.buildIndex = buildUnitIndex;
		unit.Team = unitTeam;
		NetworkServer.Spawn(go);
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
