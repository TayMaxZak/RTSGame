﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity_Objective : Entity
{
	private float captureProgress; // Ranges from -1 to 1; -1 is for the first team, 1 is for the second team, 0 is neutral

	[SerializeField]
	private UI_ProgBar progBarPrefab;
	private UI_ProgBar progBar;
	private Vector2 progBarOffset;

	private Commander recipient;

	private Manager_Game gameManager;
	private GameRules gameRules;

	// Use this for initialization
	new void Start()
	{
		gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>();
		gameRules = gameManager.GameRules; // Grab copy of Game Rules

		SetSelCircleSize(gameRules.OBJV_captureRange / 2);

		base.Start(); // Initialize selection circle

		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>(); // Grab copy of UI Manager
		progBar = Instantiate(progBarPrefab);
		progBar.transform.SetParent(uiManager.Canvas.transform, false);
		progBarOffset = uiManager.UIRules.BPBoffset;

		captureProgress = 0;

		UpdateUI();
	}

	// Update is called once per frame
	new void Update()
	{
		base.Update();

		Collider[] cols = Physics.OverlapSphere(transform.position, gameRules.OBJV_captureRange, gameRules.entityLayerMask);
		List<Unit> units = new List<Unit>();
		for (int i = 0; i < cols.Length; i++)
		{
			Unit unit = GetUnitFromCol(cols[i]);

			if (!unit) // Only Units can capture
				continue;

			if (units.Contains(unit)) // Ignore multiple colliders for one Unit
				continue;

			units.Add(unit);
		}

		float progressOffset = 0;

		foreach (Unit u in units)
		{
			if (u.team == 0)
				progressOffset -= u.GetObjectiveWeight() * gameRules.OBJV_captureAddPerUnitMult;
			else if (u.team == 1)
				progressOffset += u.GetObjectiveWeight() * gameRules.OBJV_captureAddPerUnitMult;
		}

		UpdateProgress(Mathf.Clamp(progressOffset, -gameRules.OBJV_captureAddMax, gameRules.OBJV_captureAddMax) * Time.deltaTime);
	}

	void UpdateProgress(float delta)
	{
		captureProgress = captureProgress + delta / gameRules.OBJV_captureTime;

		Commander newRecipient = null;
		if (captureProgress <= -1)
		{
			newRecipient = gameManager.GetCommander(0);
			captureProgress = -1;
		}
		else if (captureProgress >= 1)
		{
			newRecipient = gameManager.GetCommander(1);
			captureProgress = 1;
		}

		// Objective has changed hands
		if (newRecipient && (recipient == null || newRecipient != recipient))
		{
			if (recipient) // Take resources from previous recipient
			{
				recipient.TakeResources(5, true);
			}
			recipient = newRecipient;
			recipient.GiveResources(5); // Add resources to new recipient
		}

		if (progBar)
			UpdateUI();
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
			progBar.UpdateProgBar((captureProgress + 1) / 2);
		}
	}

	Unit GetUnitFromCol(Collider col)
	{
		Entity ent = col.GetComponentInParent<Entity>();
		if (ent)
		{
			if (ent.GetType() == typeof(Unit) || ent.GetType().IsSubclassOf(typeof(Unit)))
				return (Unit)ent;
			else
				return null;
		}
		else
		{
			return null;
		}
	}

	// Visualize range of turrets in editor
	void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.magenta;
		Gizmos.DrawWireSphere(transform.position, GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules.OBJV_captureRange);
	}
}