﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Commander : MonoBehaviour
{
	public int team;

	[Header("GUI")]
	[SerializeField]
	private UI_ResCounter resPointsCounter;
	[SerializeField]
	private GameObject buildButtonRoot;
	[SerializeField]
	private UI_BuildButton[] buildButtons;

	[Header("Building")]
	[SerializeField]
	private BuildUnit[] buildUnits;
	private int[] buildUnitCounters; // Tracks number of times corresponding build unit was built
	private int buildUnitIndex;

	private GameObject buildPreview;
	private List<GameObject> pendingBuilds;

	private int buildState; // 0 = standby, 1 = moving preview, 2 = rotating preview
	private Coroutine buildHappening;

	[Header("Resources")]
	[SerializeField]
	private int resPoints;
	[SerializeField]
	private int resPointsToReclaim; // Transferred over time into resPoints
	private float resReclaimTimer;

	[Header("Sound")]
	[SerializeField]
	private AudioClip soundMove;

	[Header("Clicking")]
	[SerializeField]
	private Camera cam;
	[SerializeField]
	private GameObject clickEffect;
	private float clickRayLength = 1000;

	[Header("Grids")]
	[SerializeField]
	private GameObject[] grids;
	[SerializeField]
	private int defaultGrid = 1;
	private int curGrid;

	private List<Entity> selection;
	private Entity hoverion;
	private AudioSource audioSource;
	private GameRules gameRules;

	// Use this for initialization
	void Start ()
	{
		//resPoints = resPointsMax;

		//Time.timeScale = 1;
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules
		audioSource = GetComponent<AudioSource>();

		selection = new List<Entity>();
		Select(null, false);
		UpdateUI(false);

		curGrid = defaultGrid;
		foreach (GameObject go in grids)
			go.SetActive(false);
		UpdateGrid(curGrid);

		buildUnitCounters = new int[buildUnits.Length];
		for (int i = 0; i < buildButtons.Length; i++)
		{
			buildButtons[i].SetTeam(team);
			buildButtons[i].SetIndex(i);
			buildButtons[i].SetCost();
			buildButtons[i].SetCounter(buildUnitCounters[i]);
		}

		resReclaimTimer = gameRules.RESreclaimTime;
		UpdateResourceUIAmounts();
}

	/// <summary>
	/// 0 for entity checking, 1 for grid checking, 2 for either
	/// </summary>
	/// <param name="targetLayer"></param>
	/// <returns></returns>
	RaycastHit RaycastFromCursor(int targetLayer) // 0 = entity, 1 = grid, 2 = anything
	{
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			RaycastHit hit;
			if ((targetLayer == 2 || targetLayer == 0) && Physics.Raycast(ray, out hit, clickRayLength, gameRules.entityLayerMask) && GetEntityFromHit(hit)) {}
			else if ((targetLayer == 2 || targetLayer == 1) && Physics.Raycast(ray, out hit, clickRayLength, gameRules.gridLayerMask)) {}
			else hit = new RaycastHit();
			return hit;
		}
		return new RaycastHit();
	}

	Entity GetEntityFromHit(RaycastHit hit)
	{
		if (hit.collider)
		{
			return hit.collider.GetComponentInParent<Entity>();
		}
		else
		{
			return null;
		}
	}

	// Update is called once per frame
	void Update ()
	{
		//UpdateUI(false);
		UpdateReclaim();
		UpdateInput();
	}

	void UpdateInput()
	{
		bool control = false;
		if (Input.GetButton("Control"))
		{
			control = true;
		}

		// Cursor position code
		// Should be done before clicking code
		if (buildState == 0)
		{
			RaycastHit hit = RaycastFromCursor(0);
			Entity ent = GetEntityFromHit(hit);
			if (ent)
			{
				if (hoverion)
					hoverion.OnHover(this, false);
				ent.OnHover(this, true);
				hoverion = ent;
			}
			else if (hoverion)
				hoverion.OnHover(this, false);
		}
		else if (buildState == 1)
		{
			RaycastHit hit = RaycastFromCursor(1);
			if (hit.collider)
			{
				// From the flagship (selected), find a position within the spawning radius which is closest to our preview position
				Vector3 dif = Vector3.ClampMagnitude(hit.point - selection[0].transform.position, gameRules.SPWNflagshipRadius);
				Vector3 pos = selection[0].transform.position + dif;

				buildPreview.transform.position = pos;
				if (!buildPreview.activeSelf)
				{
					buildPreview.SetActive(true);
				}
			}
			else
			{
				// Deactivate preview if not on a valid position
				if (buildPreview.activeSelf)
				{
					//buildPreview.SetActive(false);
				}
			}
		}
		else if (buildState == 2)
		{
			buildPreview.SetActive(true);
			RaycastHit hit = RaycastFromCursor(1);
			if (hit.collider && !GetEntityFromHit(hit))
			{
				buildPreview.transform.LookAt(hit.point);
			}
		}

		// Clicking
		if (Input.GetMouseButtonDown(0))
		{
			if (!EventSystem.current.IsPointerOverGameObject()) // Even a failed raycast would still result in a Select(null) call without this check in place
			{
				RaycastHit hit = RaycastFromCursor(0);
				Entity ent = GetEntityFromHit(hit);
				if (buildState == 0) // Normal select mode
				{
					if (control)
					{
						if (ent)
								Select(ent, true);
						else
							Select(null, true);
					}
					else
					{
						if (ent)
							Select(ent, false);
						else
							Select(null, false);
					}
				}
			}
		} //lmb

		bool previewingBuild = buildState == 1 || buildState == 2;
		if (previewingBuild && Input.GetMouseButtonUp(0))
		{
			RaycastHit hit = RaycastFromCursor(1);
			//Entity ent = GetEntityFromHit(hit);

			if (hit.collider)
			{
				if (buildState == 1) // Currently moving around preview, left clicking now will place the preview and let us start rotating it
				{
					PlacePreview();
				}
				else if (buildState == 2) // Currently rotating preview, left clicking now will finalize preview
				{
					BuildStart();
				}
			}
		} //lmb up

		if (Input.GetMouseButtonDown(1))
		{
			if (!EventSystem.current.IsPointerOverGameObject()) // Right clicks should not do anything if we are over UI, regardless of clicking on grid or not
			{
				if (buildState > 0)
					BuildCancel();
				else if (HasSelection())
				{
					RaycastHit hit = RaycastFromCursor(2);
					Entity ent = GetEntityFromHit(hit);
					if (ent)
					{
						if (IsUnit(ent))
							Target(ent);
					}
					else if (hit.collider)
						Move(hit.point);
				} //selected
			}
		} //rmb

		// Raise/Lower Grid
		if (Input.GetButtonDown("RaiseGrid"))
		{
			UpdateGrid(Mathf.Clamp(curGrid + 1, 0, grids.Length - 1));
		}
		else if (Input.GetButtonDown("LowerGrid"))
		{
			UpdateGrid(Mathf.Clamp(curGrid - 1, 0, grids.Length - 1));
		}
		else if (Input.GetButtonDown("DefaultGrid"))
		{
			UpdateGrid(defaultGrid);
		}

		// Abilities
		if (Input.GetButtonDown("Ability1"))
		{
			UseAbility(0);
		}
		if (Input.GetButtonDown("Ability2"))
		{
			UseAbility(1);
		}
		if (Input.GetButtonDown("Ability3"))
		{
			UseAbility(2);
		}

		if (Input.GetButtonDown("CommandWheel"))
		{
			UseCommandWheel();
		}
	}

	void UseAbility(int index)
	{
		if (!HasSelection())
			return;

		UnitType type = UnitType.Default;
		foreach (Entity e in selection)
		{
			if (IsUnit(e))
			{
				Unit unit = (Unit)e;
				if (type == UnitType.Default)
					type = unit.type;
				else if (unit.type != type)
					return;
			}
		}

		foreach (Entity e in selection)
		{
			if (IsUnit(e))
			{
				Unit unit = (Unit)e;

				if (unit.abilities.Count < index + 1)
					return;

				Ability current = unit.abilities[index];

				if (AbilityUtils.RequiresTarget(current.GetAbilityType()) == 0) // Targets nothing
				{
					unit.OrderAbility(index, null);
				}
				else if (AbilityUtils.RequiresTarget(current.GetAbilityType()) == 1) // Targets a unit
				{
					Entity ent = GetEntityFromHit(RaycastFromCursor(0));
					if (ent && IsUnit(ent))
					{
						AbilityTarget targ = new AbilityTarget((Unit)ent);
						unit.OrderAbility(index, targ);
					}
				}
				else if (AbilityUtils.RequiresTarget(current.GetAbilityType()) == 2) // Targets a position
				{
					RaycastHit hit = RaycastFromCursor(1);
					if (hit.collider)
					{
						AbilityTarget targ = new AbilityTarget(hit.point);
						unit.OrderAbility(index, targ);
					}
				}
			} //if IsUnit
		} //foreach
	} //UseAbility()

	void UseCommandWheel()
	{
		if (!HasSelection())
			return;

		foreach (Entity e in selection)
		{
			if (IsUnit(e))
			{
				Unit unit = (Unit)e;

				RaycastHit hit = RaycastFromCursor(1);
				if (hit.collider)
				{
					AbilityTarget targ = new AbilityTarget(hit.point);
					unit.OrderCommandWheel(2, targ);
				}
				else
					unit.OrderCommandWheel(2, null);
			}
		}
	}

	void UpdateGrid(int newGrid)
	{
		grids[curGrid].SetActive(false);
		curGrid = newGrid;
		grids[curGrid].SetActive(true);
	}

	void UpdateUI(bool selectingNewUnit)
	{
		if (selectingNewUnit && HasSelection())
		{
			bool isFlagship = false;

			foreach (Entity e in selection)
			{
				Unit unit = (Unit)e;
				//selectedText.text = "Selected: " + selected.DisplayName;

				Flagship flag = unit.gameObject.GetComponent<Flagship>();

				if (flag)
					isFlagship = true;
			}

			if (isFlagship)
			{
				buildButtonRoot.SetActive(true);
				//selectedText.text = "|| FLAGSHIP ||";
			}
			else
				buildButtonRoot.SetActive(false);
		}
		else
			buildButtonRoot.SetActive(false);
	}

	public void UseBuildButton(int id)
	{
		if (buildState != 0)
			return;

		buildUnitIndex = id;

		if (buildUnitCounters[buildUnitIndex] >= buildUnits[buildUnitIndex].unitCap) // Already reached the unit number cap
			return;

		if (!SubtractResources(buildUnits[buildUnitIndex].cost)) // Don't have enough resources to build
			return;

		buildState = 1;
		buildPreview = Instantiate(buildUnits[buildUnitIndex].previewObject, Vector3.zero, Quaternion.identity);
		buildPreview.SetActive(false);
	}

	public BuildUnit GetBuildUnit(int id)
	{
		return buildUnits[id];
	}

	void PlacePreview()
	{
		buildState = 2;
	}

	void BuildCancel()
	{
		AddResources(buildUnits[buildUnitIndex].cost);
		buildState = 0;
		Destroy(buildPreview);
	}

	void BuildStart()
	{
		buildState = 0;

		Clone_Build pendingBuild = buildPreview.GetComponent<Clone_Build>();
		pendingBuild.buildUnit = buildUnits[buildUnitIndex];
		pendingBuild.Build(buildUnitIndex);

		buildUnitCounters[buildUnitIndex]++;

		buildButtons[buildUnitIndex].SetCounter(buildUnitCounters[buildUnitIndex]);
	}

	void UpdateReclaim()
	{
		if (resPointsToReclaim > 0)
		{
			resReclaimTimer -= Time.deltaTime;
			UpdateResourceUITime();

			if (resReclaimTimer <= 0)
			{
				resReclaimTimer = gameRules.RESreclaimTime;
				resPointsToReclaim--;
				resPoints++;
				UpdateResourceUIAmounts();
			}
		}
	}

	void UpdateResourceUIAmounts()
	{
		resPointsCounter.UpdateResCounter(resPoints, resPointsToReclaim);

		foreach (UI_BuildButton bb in buildButtons)
			bb.CheckInteractable();
	}

	void UpdateResourceUITime()
	{
		resPointsCounter.UpdateTime(1.0f - (resReclaimTimer / gameRules.RESreclaimTime));
	}

	bool SubtractResources(int amount)
	{
		int newResPoints = resPoints - amount;

		if (CheckResources(amount))
		{
			resPoints = newResPoints;
			UpdateResourceUIAmounts();
			return true;
		}
		else
		{
			return false;
		}
	}

	bool CheckResources(int cost)
	{
		int newResPoints = resPoints - cost;

		if (newResPoints >= 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	void RefundBuildCap(int index)
	{
		buildUnitCounters[index]--;
	}

	public void RefundUnitCounter(int index)
	{
		if (index < 0) // Not initialized with a build index
			return;

		RefundBuildCap(index);

		buildButtons[index].SetCounter(buildUnitCounters[index]);

		CheckSelection();
	}

	public float GetResources()
	{
		return resPoints;
	}

	public bool TakeResources(int amount)
	{
		return SubtractResources(amount);
	}

	public void GiveRes(int amount)
	{
		AddResources(amount);
	}

	void AddResources(int amount)
	{
		resPoints = resPoints + amount;
		UpdateResourceUIAmounts();
	}

	public void GiveRec(int amount)
	{
		AddResourcesToReclaim(amount);
	}

	void AddResourcesToReclaim(int amount)
	{
		resPointsToReclaim = resPointsToReclaim + amount;
		UpdateResourceUIAmounts();
	}

	bool IsUnit(Entity ent)
	{
		if (!ent)
			return false;

		return ent.GetType() == typeof(Unit) || ent.GetType().IsSubclassOf(typeof(Unit));
	}

	public void Select(Entity newSel, bool add)
	{
		bool newSelIsUnit = IsUnit(newSel);

		if (newSelIsUnit)
			if (((Unit)newSel).team != team)
				return;


		
		if (add)
		{
			if (!newSel)
			{
				return;
			}

			// If we have a list of only Units, and we attempt to add a non-Unit, return
			// If we already have this Entity, return
			bool allUnits = true;

			for (int i = 0; i < selection.Count; i++)
			{
				Entity e = selection[i];

				if (!IsUnit(e))
					allUnits = false;

				if (e == newSel)
				{
					e.OnSelect(this, false);
					selection.RemoveAt(i);
					UpdateUI(true);
					return;
				}
			}

			if (allUnits && !newSelIsUnit)
			{
				return;
			}

			newSel.OnSelect(this, true);
			selection.Add(newSel);
		}
		else
		{
			if (HasSelection())
			{
				foreach (Entity e in selection)
				{
					if (e != newSel)
					{
						e.OnSelect(this, false);
					}
				}
			}

			selection = new List<Entity>();
			if (newSel)
			{
				newSel.OnSelect(this, true);
				selection.Add(newSel);
			}
		}

		UpdateUI(true);

		//if (newSel)
		//else
		//selectedText.text = "";
	}

	void CheckSelection()
	{
		List<Entity> toRemove = new List<Entity>();
		foreach (Entity e in selection)
		{
			if (IsUnit(e))
			{
				if (((Unit)e).IsDead())
					toRemove.Add(e);
			}
		}

		foreach (Entity e in toRemove)
		{
			selection.Remove(e);
		}
	}

	public void Move(Vector3 newPos)
	{
		if (HasSelection())
		{
			foreach (Entity e in selection)
			{
				if (IsUnit(e))
					((Unit)e).OrderMove(newPos);
			}

			AudioUtils.PlayClipAt(soundMove, transform.position, audioSource);
			Instantiate(clickEffect, newPos, Quaternion.identity);
		}
	}

	public void Target(Entity newTarg)
	{
		if (HasSelection())
		{
			foreach (Entity e in selection)
			{
				//if (newTarg && IsUnit(newTarg) && IsUnit(e) && ((Unit)newTarg).team != team)
				if (newTarg && IsUnit(newTarg) && newTarg != e && IsUnit(e))
					((Unit)e).OrderAttack((Unit)newTarg);
			}
		}
	}

	bool HasSelection()
	{
		return selection.Count > 0;
	}
}
