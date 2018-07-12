using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Controller_Commander : MonoBehaviour
{
	public bool allowTeamSwaps = false;
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

	private int heightState; // 0 = standby, 1 = changing
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

	[Header("Box Select")]
	[SerializeField]
	private Image marquee;
	private Vector3 boxSelectStart;
	private Vector3 boxSelectEnd;
	private Entity startSelectedEntity; // How far apart should the start and end be to count as a box selection?

	[Header("Heights")]
	[SerializeField]
	private Effect_Line lineMouse;
	[SerializeField]
	private Effect_Line lineVert;
	[SerializeField]
	private GameObject movementGrid;
	[SerializeField]
	private int heightSpacing = 10;
	[SerializeField]
	private int heightMinNum = -5; // TODO
	[SerializeField]
	private int heightMaxNum = 5; // TODO

	private List<Entity> selection;
	private Entity currentHoveredEntity;
	private Entity currentStatEntity;
	private AudioSource audioSource;

	private Manager_Game gameManager;
	private GameRules gameRules;

	// Use this for initialization
	void Start()
	{
		//Time.timeScale = 3;

		gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>();
		gameRules = gameManager.GameRules; // Grab copy of Game Rules
		SetCommander(gameManager.GetCommander(team));

		selection = new List<Entity>();
		Select(null, false);

		audioSource = GetComponent<AudioSource>();

		lineMouse.SetEffectActive(0);
		lineVert.SetEffectActive(0);
	}

	void SetCommander(Commander newCommander)
	{
		if (commander)
			commander.SetController(null);
		commander = newCommander;
		commander.SetController(this);
		commander.InitUI();
	}

	void SetTeam(int newTeam)
	{
		team = newTeam; // Set team field
		Select(null, false); // Clear selection

		SetCommander(gameManager.GetCommander(team)); // Set commander
	}

	public void Select(Entity newSel, bool add, bool multiSelectRemoveExisting)
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
					if (multiSelectRemoveExisting)
					{
						e.OnSelect(false);
						selection.RemoveAt(i);
					}
					SelectionChanged();
					return;
				}
			}

			// If we have a list of only Units, and we attempt to add a non-Unit, return
			if (allUnits && !newSelIsUnit)
				return;

			newSel.OnSelect(true);
			selection.Add(newSel);
			SelectionChanged();
		}
		else
		{
			// TODO: Optimize update of EntityStats when we select the same single entity which was already selected before

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
				SelectionChanged();
			}
			else
				SelectionChanged();
		}
	}

	public void Select(Entity newSel, bool add)
	{
		Select(newSel, add, true);
	}

	void SelectionChanged()
	{
		//UpdateMovementGrid();
		ShowHideStatsBuildButtons();
	}

	void UpdateMovementGrid()
	{
		if (buildState < 2)
		{
			float pos = 0;
			int unitCount = 0;

			foreach (Entity e in selection)
			{
				if (IsUnit(e))
				{
					pos += e.transform.position.y;
					unitCount++;
				}
			}

			if (unitCount > 0)
			{
				pos /= unitCount;
				movementGrid.transform.position = new Vector3(0, pos, 0); // TODO: Should grid be centered somewhere other than the origin?
			}
			else
				movementGrid.transform.position = Vector3.zero;
		}
		else
		{
			movementGrid.transform.position = new Vector3(0, buildPreview.transform.position.y, 0); // TODO: Should grid be centered somewhere other than the origin?
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
			ShowHideStatsBuildButtons();
	}

	public void Move(Vector3 newPos)
	{
		if (HasSelection())
		{
			Vector3 avgPos = Vector3.zero;
			int divisor = 0;
			List<Unit> units = new List<Unit>();

			// Calculate average position
			foreach (Entity e in selection)
			{
				if (IsUnit(e))
				{
					units.Add((Unit)e);
					avgPos += e.transform.position;
					divisor++;
					
				}
			}

			// All entities
			if (divisor == 0)
				return;

			avgPos /= divisor;

			// Offset
			Vector3 dif = newPos - avgPos;

			// Relative goal
			foreach (Unit u in units)
			{
				Vector3 newVec = u.transform.position + dif;
				newVec.y = u.transform.position.y;
				u.OrderMove(newVec);
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
				if (newTarg && IsUnit(newTarg) && IsUnit(e) && ((Unit)newTarg).team != team)
				//if (newTarg && IsUnit(newTarg) && newTarg != e && IsUnit(e))
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
		UpdateMovementGrid();
		UpdateInput();
	}

	void UpdateInput()
	{
		// Modifier keys
		bool control = false;
		if (Input.GetButton("Control") || Input.GetButtonUp("Control"))
		{
			control = true;
		}

		if (heightState == 0 && Input.GetButtonDown("ChangeHeight"))
		{
			heightState = 1;
		}

		// Cursor position code
		// Should be done before clicking code
		if (buildState == 0)
		{
			if (heightState == 0)
			{
				RaycastHit hit = RaycastFromCursor(0);
				Entity ent = GetEntityFromHit(hit);
				// Hovering an entity
				if (ent)
				{
					// New hovered entity
					if (currentHoveredEntity != ent)
					{
						if (currentHoveredEntity)
							currentHoveredEntity.OnHover(false);
						ent.OnHover(true);
						currentHoveredEntity = ent;
					}
				}
				// Stopped hovering an entity
				else if (currentHoveredEntity)
				{
					currentHoveredEntity.OnHover(false);
					currentHoveredEntity = null;
				}

				// Box select preview
				if (Input.GetMouseButton(0))
				{
					SetMarqueeActive(true);
					Vector3 mousePosition = Input.mousePosition;
					if (Input.GetMouseButtonDown(0)) // This code will run earlier in the frame than the actual LMB code // TODO: Clean up
						boxSelectStart = mousePosition;

					marquee.rectTransform.position = new Vector2(mousePosition.x < boxSelectStart.x ?mousePosition.x : boxSelectStart.x,
						mousePosition.y < boxSelectStart.y ? mousePosition.y : boxSelectStart.y);
					marquee.rectTransform.sizeDelta = new Vector2(mousePosition.x < boxSelectStart.x ? boxSelectStart.x - mousePosition.x : mousePosition.x - boxSelectStart.x,
						mousePosition.y < boxSelectStart.y ? boxSelectStart.y - mousePosition.y : mousePosition.y - boxSelectStart.y);
				}
				else
					SetMarqueeActive(false);
			}
			else // Changing height
			{
				if (selection.Count == 1 && IsUnit(selection[0]))
				{
					Vector3 mousePos = Input.mousePosition;
					Unit u = (Unit)selection[0];
					float selPos = Camera.main.WorldToScreenPoint(u.transform.position).y;
					int upOrDown = 0;
					if (selPos < mousePos.y) // Greater Y is lower on screen
						upOrDown = 1;
					else
						upOrDown = -1;

					int curGrid = (u.GetCurrentHeight()) / heightSpacing;
					int newGridHeight = (curGrid + upOrDown) * heightSpacing;

					Vector3 worldPoint = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 20));
					lineMouse.SetEffectActive(1, u.transform.position, worldPoint);
					lineVert.SetEffectActive(1, u.transform.position, new Vector3(u.transform.position.x, newGridHeight, u.transform.position.z));

					if (Input.GetButtonUp("ChangeHeight"))
					{
						u.OrderChangeHeight(newGridHeight);
						HeightCancel(); // Finish height change
					}
				}
			} // heightState
		}
		else if (buildState == 1)
		{
			RaycastHit hit = RaycastFromCursor(1);
			if (hit.collider)
			{
				// From the flagship (selection), find a position within the spawning radius which is closest to our preview position
				Vector3 dif = Vector3.ClampMagnitude(hit.point - selection[0].transform.position, gameRules.SPWNflagshipRadius);
				Vector3 pos = selection[0].transform.position + dif;
				pos.y = HeightSnap(pos.y);

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
				Vector3 point = hit.point;
				point.y = buildPreview.transform.position.y;
				buildPreview.transform.LookAt(point);
			}
		}

		// Clicking
		// Left mouse button
		if (Input.GetMouseButtonDown(0))
		{
			if (!EventSystem.current.IsPointerOverGameObject()) // Even a failed raycast would still result in a Select(null) call without this check in place
			{
				if (buildState == 0)
				{
					if (heightState == 0) // Not building or changing height
					{
						RaycastHit hit = RaycastFromCursor(0);
						Entity ent = GetEntityFromHit(hit);

						boxSelectStart = Input.mousePosition; // Remember where we first clicked
						if (control) // Multi select
						{
							if (ent)
							{
								Select(ent, true);
								startSelectedEntity = ent;
							}
							//else
							//	Select(null, true);
						}
						else // Normal select
						{
							if (ent)
							{
								Select(ent, false);
								startSelectedEntity = ent;
							}
							//else
							//	Select(null, false);
						}
					}
					else // Changing height
					{
						HeightCancel();
					}
				}
			}
		} //lmb

		bool previewingBuild = buildState == 1 || buildState == 2;
		if (Input.GetMouseButtonUp(0))
		{
			if (previewingBuild)
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
			}
			else // Box select
			{
				boxSelectEnd = Input.mousePosition;

				// If not holding control, clear current selection first (keeping the entity we selected on mouse down)
				if (!control)
					Select(startSelectedEntity, false);

				// Raycast
				RaycastHit hit = RaycastFromCursor(0);
				Entity ent = GetEntityFromHit(hit);
				boxSelectEnd = Input.mousePosition; // Remember where we let go of the mouse
				if (ent && ent != startSelectedEntity) // Add second entity to selection
					Select(ent, true, false);
				startSelectedEntity = null; // Forget our initial selection for next time

				// Construct a rect for checking screen positions
				Rect boxSelect = new Rect(boxSelectStart.x, boxSelectStart.y, boxSelectEnd.x - boxSelectStart.x, boxSelectEnd.y - boxSelectStart.y);

				List<Unit> selectable = commander.GetSelectableUnits();
				for (int i = 0; i < selectable.Count; i++)
				{
					// For each selectable unit, if its visual center lies in the rect, we add it to our current selection
					Vector3 screenPoint = Camera.main.WorldToScreenPoint(selectable[i].transform.position);
					if (boxSelect.Contains(screenPoint, true))
					{
						Select(selectable[i], true, false);
					}
				}
			}
		} //lmb up

		// Right mouse button
		if (Input.GetMouseButtonDown(1))
		{
			if (!EventSystem.current.IsPointerOverGameObject()) // Right clicks should not do anything if we are over UI, regardless of clicking on grid or not
			{
				if (buildState > 0 || heightState > 0) // Cancel build or eight change without ordering anything to the selection
				{
					if (buildState > 0)
					{
						BuildCancel();
					}
					else if (heightState > 0)
					{
						HeightCancel();
					}
				}
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

		// Command wheel
		if (Input.GetButtonDown("CommandWheel"))
		{
			UseCommandWheel();
		}

		// Control-enabled actions
		if (control)
		{
			if (Input.GetButtonDown("SelectAll"))
			{
				List<Unit> selectable = commander.GetSelectableUnits();
				for (int i = 0; i < selectable.Count; i++)
				{
					if (selectable[i].Type != EntityType.Flagship)
						Select(selectable[i], true, false);
				}
			}
		}

		// Switch teams when a key is pressed
		if (allowTeamSwaps)
		{
			if (Input.GetKeyDown("1"))
			{
				if (team == 0)
					SetTeam(1);
				else
					SetTeam(0);
			}
		}
	}

	void SetMarqueeActive(bool isActive)
	{
		if (isActive && !marquee.gameObject.activeSelf)
			marquee.gameObject.SetActive(true);
		else if (!isActive && marquee.gameObject.activeSelf)
			marquee.gameObject.SetActive(false);
	}

	public int HeightSnap(float org)
	{
		float newVal = org / heightSpacing;
		int newInt = Mathf.RoundToInt(newVal);
		return newInt * heightSpacing;
	}

	void HeightCancel()
	{
		lineMouse.SetEffectActive(0);
		lineVert.SetEffectActive(0);

		heightState = 0;
	}

	void UseAbility(int index)
	{
		if (!HasSelection())
			return;

		// If we are dealing with a mixed group of units, don't use abilities
		EntityType type = EntityType.Default;
		foreach (Entity e in selection)
		{
			// TODO: Check by abilities rather than types
			if (type == EntityType.Default)
				type = e.Type;
			else if (e.Type != type)
				return;
			/*
			if (IsUnit(e))
			{
				Unit unit = (Unit)e;
				if (type == EntityType.Default)
					type = unit.type;
				else if (unit.type != type)
					return;
			}
			*/
		}

		foreach (Entity e in selection)
		{
			if (IsUnit(e))
			{
				Unit unit = (Unit)e;

				if (unit.abilities.Count < index + 1)
					return;

				//AbilityOld current = unit.abilities[index];
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

	void ShowHideStatsBuildButtons()
	{
		// If we updated our selection and only one Entity is left selected,
		if (selection.Count == 1)
		{
			Unit unit = (Unit)selection[0];

			if (unit)
			{
				InitStats(unit);

				// and that entity is a flagship, show build buttons
				Unit_Flagship flag = selection[0].gameObject.GetComponent<Unit_Flagship>();
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
		pendingBuild.Build(buildUnitIndex, team);

		// Resources should already be subtracted at this point
		commander.IncrementUnitCounter(buildUnitIndex);
	}

	public void HideStats()
	{
		entityStats.gameObject.SetActive(false);
	}

	// Called whenever EntityStats is attached to a new unit
	public void InitStats(Unit who)
	{
		// Set active if not already active
		if (!entityStats.gameObject.activeSelf)
			entityStats.gameObject.SetActive(true);

		// Determine number of active abilities
		List<AbilityType> abilityCounter = new List<AbilityType>();
		foreach (Ability a in who.abilities)
		{
			abilityCounter.Add(a.GetAbilityType());
		}
		entityStats.SetAbilityIconsAndInfo(abilityCounter.ToArray());
		for (int i = 0; i < who.abilities.Count; i++)
		{
			UpdateStatsAbility(who, i, true);
			UpdateStatsAbilityIconB(who, i);
		}

		entityStats.SetDisplayEntity(who.Type);

		UpdateStatsHP(who);
		UpdateStatsShields(who);
		UpdateStatsStatuses(who.GetStatuses());

		// Establish connection for realtime health and ability state updates
		if (currentStatEntity)
			currentStatEntity.LinkStats(false, this);
		who.LinkStats(true, this);
		currentStatEntity = who;
	}

	// Update health and armor counters and fills
	public void UpdateStatsHP(Unit who)
	{
		Vector4 healthArmor = who.GetHP();
		entityStats.SetHealthArmor(healthArmor.x, healthArmor.y, healthArmor.z, healthArmor.w, !who.alwaysBurnImmune);
	}

	// Update visibility of shield status and number for shield counter
	public void UpdateStatsShields(Unit who)
	{
		Vector2 shields = who.GetShields();
		entityStats.SetShields(shields.x, shields.y);
	}

	// Update fill indicator and stack counter
	public void UpdateStatsAbility(Unit who, int index, bool updateStacks)
	{
		AbilityDisplayInfo display = who.abilities[index].GetDisplayInfo();
		if (!display.displayInactive)
		{
			if (!display.displayFill)
				entityStats.SetAbilityProgress(index, display.cooldown);
			else
				entityStats.SetAbilityProgress(index, display.fill);
		}
		else
			entityStats.SetAbilityProgress(index, 1);

		if (updateStacks)
		{
			if (display.displayStacks)
				entityStats.SetAbilityStacks(index, display.stacks);
			else
				entityStats.SetAbilityStacks(index, 0);
		}
	}

	public void UpdateStatsAbilityIconB(Unit who, int index)
	{
		AbilityDisplayInfo display = who.abilities[index].GetDisplayInfo();
		if (display.displayIconB)
			entityStats.SetAbilityIconB(index, who.abilities[index].GetAbilityType(), display.iconBState);
		else
			entityStats.ClearAbilityIconB(index);
	}

	public void UpdateStatsStatuses(List<Status> statuses)
	{
		entityStats.SetStatuses(statuses);
	}

	public void UpdateResourceAmounts(int resPoints, int reclaimPoints)
	{
		resPointsCounter.UpdateResCounter(resPoints, reclaimPoints);

		foreach (UI_BuildButton bb in buildButtons)
			bb.UpdateInteractable();
	}

	public void UpdateResourceTime(float reclaimTimer)
	{
		resPointsCounter.UpdateTime(1.0f - (reclaimTimer / gameRules.RESreclaimTime));
	}

	public void UpdateResourceAudio(float timer)
	{
		resPointsCounter.PlayReclaimAudio(timer);
	}

	public void UpdateBuildButtonInteract()
	{
		foreach (UI_BuildButton bb in buildButtons)
			bb.UpdateInteractable();
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
			buildButtons[i].SetTooltip(EntityUtils.GetDisplayDesc(commander.GetBuildUnit(i).type));
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
