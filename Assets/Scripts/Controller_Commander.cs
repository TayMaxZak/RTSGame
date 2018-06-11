using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Controller_Commander : MonoBehaviour
{
	public int team;
	private Commander commander;

	[Header("GUI")]
	[SerializeField]
	private UI_ResCounter resPointsCounter;
	[SerializeField]
	private UI_EntityStats entityStats;
	[SerializeField]
	private GameObject buildButtonsRoot;
	[SerializeField]
	private UI_BuildButton[] buildButtons;

	[Header("Building")]
	private int buildUnitIndex;

	private GameObject buildPreview;
	private List<GameObject> pendingBuilds;

	private int buildState; // 0 = standby, 1 = moving preview, 2 = rotating preview

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
	private Entity currentHoveredEntity;
	private Entity currentStatEntity;
	private AudioSource audioSource;

	private Manager_Game gameManager;
	private GameRules gameRules;

	// Use this for initialization
	void Start()
	{
		//Time.timeScale = 1;

		gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>();
		gameRules = gameManager.GameRules; // Grab copy of Game Rules
		ChangeCommander(gameManager.Commanders[team]);

		selection = new List<Entity>();
		Select(null, false);

		curGrid = defaultGrid;
		foreach (GameObject go in grids)
			go.SetActive(false);
		UpdateGrid(curGrid);

		audioSource = GetComponent<AudioSource>();
	}

	void ChangeCommander(Commander newCommander)
	{
		commander = newCommander;
		commander.SetController(this);
	}

	public void Select(Entity newSel, bool add)
	{
		bool newSelIsUnit = IsUnit(newSel);

		// If we try to select a unit which does not belong to our team, return
		if (newSelIsUnit)
			if (((Unit)newSel).team != team)
				return;

		if (add) // Multi-select mode
		{
			// If we are multi-selecting, selecting nothing will not clear existing selection
			if (!newSel)
				return;

			bool allUnits = true;
			for (int i = 0; i < selection.Count; i++)
			{
				Entity e = selection[i];

				if (!IsUnit(e))
					allUnits = false;

				// If we already have this Entity, remove it
				if (e == newSel)
				{
					e.OnSelect(false);
					selection.RemoveAt(i);
					UpdateUI();
					return;
				}
			}

			// If we have a list of only Units, and we attempt to add a non-Unit, return
			if (allUnits && !newSelIsUnit)
				return;

			newSel.OnSelect(true);
			selection.Add(newSel);
			UpdateUI();
		}
		else
		{
			// Deselect all currently selected Entities
			foreach (Entity e in selection)
			{
				if (e != newSel)
				{
					e.OnSelect(false);
				}
			}

			// Clear selection and add the newly selected entity
			selection.Clear();
			if (newSel)
			{
				newSel.OnSelect(true);
				selection.Add(newSel);
				UpdateUI();
			}
			else
				UpdateUI();
		}
	}

	public void CleanSelection()
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

		// Only update UI if selection actually changed
		if (toRemove.Count > 0)
			UpdateUI();
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

	/// <summary>
	/// 0 for entity checking, 1 for grid checking, 2 for either
	/// </summary>
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
				if (currentHoveredEntity != ent)
				{
					if (currentHoveredEntity)
						currentHoveredEntity.OnHover(false);
					ent.OnHover(true);
					currentHoveredEntity = ent;
				}
			}
			else if (currentHoveredEntity)
			{
				currentHoveredEntity.OnHover(false);
				currentHoveredEntity = null;
			}
		}
		else if (buildState == 1)
		{
			RaycastHit hit = RaycastFromCursor(1);
			if (hit.collider)
			{
				// From the flagship (selection), find a position within the spawning radius which is closest to our preview position
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

				if (AbilityUtils.GetTargetRequirement(current.GetAbilityType()) == 0) // Targets nothing
				{
					unit.OrderAbility(index, null);
				}
				else if (AbilityUtils.GetTargetRequirement(current.GetAbilityType()) == 1) // Targets a unit
				{
					Entity ent = GetEntityFromHit(RaycastFromCursor(0));
					if (ent && IsUnit(ent))
					{
						AbilityTarget targ = new AbilityTarget((Unit)ent);
						unit.OrderAbility(index, targ);
					}
				}
				else if (AbilityUtils.GetTargetRequirement(current.GetAbilityType()) == 2) // Targets a position
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

	void UpdateUI()
	{
		// If we updated our selection and only one Entity is left selected,
		if (selection.Count == 1)
		{
			/*
			bool isFlagship = true;
			
			foreach (Entity e in selection)
			{
				Unit unit = (Unit)e;
				//selectedText.text = "Selected: " + selected.DisplayName;

				Flagship flag = unit.gameObject.GetComponent<Flagship>();

				// If any of the selected entities are not Flagships, don't display Flagship UI
				if (!flag)
					isFlagship = false;
			}

			// if the only thing selected is a flagship, display build buttons
			if (isFlagship)
			{
				buildButtonsRoot.SetActive(true);
				//selectedText.text = "|| FLAGSHIP ||";
			}
			else
				buildButtonsRoot.SetActive(false);
			*/

			Unit unit = (Unit)selection[0];

			if (unit)
			{
				InitStats(unit);

				// and that entity is a flagship, show build buttons
				Flagship flag = selection[0].gameObject.GetComponent<Flagship>();
				if (flag)
					buildButtonsRoot.SetActive(true);
				else
					buildButtonsRoot.SetActive(false);
			}
		}
		else
		{
			HideStats(); // Nothing is selected, or too much is selected
			buildButtonsRoot.SetActive(false);

			// If we have nothing selected but we are in the middle of build previewing, our flagship was destroyed, so the build must be cancelled
			if (buildState > 0)
				BuildCancel();
		}
	}

	public void UseBuildButton(int id)
	{
		if (buildState != 0)
			return;

		buildUnitIndex = id;

		// This code is obsolete. Buttons are automatically made uninteractable if these conditions are not met. There is no need to check them here again
		/*
		if (buildUnitCounters[buildUnitIndex] >= buildUnits[buildUnitIndex].unitCap) // Already reached the unit number cap
			return;

		if (!SubtractResources(buildUnits[buildUnitIndex].cost)) // Don't have enough resources to build
			return;
		*/

		commander.TakeResources(commander.GetBuildUnit(buildUnitIndex).cost);

		buildState = 1;
		buildPreview = Instantiate(commander.GetBuildUnit(buildUnitIndex).previewObject, Vector3.zero, Quaternion.identity);
		buildPreview.SetActive(false);
	}

	void PlacePreview()
	{
		buildState = 2;
	}

	void BuildCancel()
	{
		commander.GiveResources(commander.GetBuildUnit(buildUnitIndex).cost);
		buildState = 0;
		Destroy(buildPreview);
	}

	void BuildStart()
	{
		buildState = 0;

		Clone_Build pendingBuild = buildPreview.GetComponent<Clone_Build>();
		pendingBuild.buildUnit = commander.GetBuildUnit(buildUnitIndex);
		pendingBuild.Build(buildUnitIndex);

		// Resources should already be subtracted at this point
		commander.IncrementUnitCounter(buildUnitIndex);
	}

	public void HideStats()
	{
		entityStats.gameObject.SetActive(false);
	}

	public void InitStats(Unit who)
	{
		if (!entityStats.gameObject.activeSelf)
			entityStats.gameObject.SetActive(true);

		// Determine number of active abilities
		List<AbilityType> abilityCounter = new List<AbilityType>();
		foreach (Ability a in who.abilities)
		{
			if (AbilityUtils.GetActivationStyle(a.GetAbilityType()) > 0) // Not a passive
			{
				abilityCounter.Add(a.GetAbilityType());
			}
		}
		entityStats.SetAbilities(abilityCounter.ToArray());

		entityStats.SetDisplayName(who.DisplayName);

		UpdateStatsHealth(who);
		UpdateStatsAbilities(who);

		// Establish connection for realtime health and ability state updates
		if (currentStatEntity)
			currentStatEntity.LinkStats(false, this);
		who.LinkStats(true, this);
		currentStatEntity = who;
	}

	public void UpdateStatsHealth(Unit who)
	{
		Vector4 healthArmor = who.GetHP();
		entityStats.SetHealthArmor(healthArmor.x, healthArmor.y, healthArmor.z, healthArmor.w);
	}

	public void UpdateStatsAbilities(Unit who)
	{
		if (who.abilities.Count > 0)
		{
			Ability a1 = who.abilities[0];
			if (a1.displayAsUnusable)
				entityStats.SetAbilityProgress(0, 1, a1.isActive);
			else if (AbilityUtils.GetActivationStyle(a1.GetAbilityType()) == 1) // Instant
				entityStats.SetAbilityProgress(0, a1.curCooldown, a1.isActive);
			else // Toggle
				entityStats.SetAbilityProgress(0, 1 - a1.curEnergy, a1.isActive);
			entityStats.SetAbilityStacks(0, a1.stacks, a1.GetAbilityType());
		}
		if (who.abilities.Count > 1)
		{
			Ability a2 = who.abilities[1];
			if (a2.displayAsUnusable)
			{
				entityStats.SetAbilityProgress(1, 1, a2.isActive);
			}
			else if (AbilityUtils.GetActivationStyle(a2.GetAbilityType()) == 1) // Instant
				entityStats.SetAbilityProgress(1, a2.curCooldown, a2.isActive);
			else // Toggle
				entityStats.SetAbilityProgress(1, 1 - a2.curEnergy, a2.isActive);
			entityStats.SetAbilityStacks(1, a2.stacks, a2.GetAbilityType());
		}
	}

	public void UpdateResourceAmounts(int resPoints, int reclaimPoints)
	{
		resPointsCounter.UpdateResCounter(resPoints, reclaimPoints);

		foreach (UI_BuildButton bb in buildButtons)
			bb.CheckInteractable();
	}

	public void UpdateResourceTime(float reclaimTimer)
	{
		resPointsCounter.UpdateTime(1.0f - (reclaimTimer / gameRules.RESreclaimTime));
	}

	public void UpdateBuildButtonInteract()
	{
		foreach (UI_BuildButton bb in buildButtons)
			bb.CheckInteractable();
	}

	public void InitBuildButtons(int[] buildUnitCounters)
	{
		if (buildButtons.Length == 0)
			FindBuildButtons();

		for (int i = 0; i < buildButtons.Length; i++)
		{
			buildButtons[i].SetController(this);
			buildButtons[i].SetIndex(i);
			buildButtons[i].SetCost();
			buildButtons[i].SetCounter(buildUnitCounters[i]);
		}
	}

	void FindBuildButtons()
	{
		Transform[] bb = buildButtonsRoot.GetComponentsInChildren<Transform>();
		List<UI_BuildButton> buildButtonList = new List<UI_BuildButton>();
		for (int i = 2; i < bb.Length; i++)
		{
			UI_BuildButton component = bb[i].GetComponent<UI_BuildButton>();

			if (component)
				buildButtonList.Add(component);
		}

		buildButtons = buildButtonList.ToArray();
	}

	public void SetBuildButtonCounter(int index, int[] buildUnitCounters)
	{
		buildButtons[index].SetCounter(buildUnitCounters[index]);
	}

	bool HasSelection()
	{
		return selection.Count > 0;
	}

	bool IsUnit(Entity ent)
	{
		if (!ent)
			return false;

		return ent.GetType() == typeof(Unit) || ent.GetType().IsSubclassOf(typeof(Unit));
	}

	public Commander GetCommander()
	{
		return commander;
	}
}
