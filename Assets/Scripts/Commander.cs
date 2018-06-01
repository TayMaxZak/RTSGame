using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Commander : MonoBehaviour
{
	public int team;

	[Header("GUI")]
	[SerializeField]
	private Text resPointsCounter;
	[SerializeField]
	private GameObject buildButtons;

	// Building //
	[SerializeField]
	private BuildUnit[] buildUnits;
	private int[] buildUnitCounters; // Tracks number of times corresponding build unit was built
	private int buildUnitIndex;

	private GameObject buildPreview;
	private List<GameObject> pendingBuilds;

	private int buildState; // 0 = standby, 1 = moving preview, 2 = rotating preview
	private Coroutine buildHappening;

	[Header("Objectives")]
	[SerializeField]
	private float resPoints = 20;

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

	private Entity selected;
	private AudioSource audioSource;
	private GameRules gameRules;

	// Use this for initialization
	void Start ()
	{
		//Time.timeScale = 1;
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules; // Grab copy of Game Rules
		audioSource = GetComponent<AudioSource>();

		Select(null);
		UpdateUI(false);

		curGrid = defaultGrid;
		foreach (GameObject go in grids)
			go.SetActive(false);
		UpdateGrid(curGrid);

		buildUnitCounters = new int[buildUnits.Length];
	}

	RaycastHit RaycastFromCursor(int targetLayer) // 0 = entity, 1 = grid, 2 = anything else
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


		// Cursor position code
		// Should be done before clicking code
		if (buildState == 1)
		{
			RaycastHit hit = RaycastFromCursor(1);
			if (hit.collider)
			{
				Vector3 dif = Vector3.ClampMagnitude(hit.point - selected.transform.position, gameRules.SPWNflagshipRadius);
				Vector3 pos = selected.transform.position + dif;

				buildPreview.transform.position = pos;
				if (!buildPreview.activeSelf)
				{
					buildPreview.SetActive(true);
				}
			}
			else
			{
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
					if (ent)
						Select(ent);
					else
						Select(null);
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
				else if (selected)
				{
					RaycastHit hit = RaycastFromCursor(2);
					Entity ent = GetEntityFromHit(hit);
					if (ent)
					{
						if (ent != selected && IsUnit(ent))
							Target(ent);
					}
					else if (hit.collider && IsUnit(selected))
						Move(hit.point);
				} //selected
			}
		} //rmb

		if (Input.GetButton("Control"))
		{
			if (Input.GetButtonDown("Select All"))
			{

			}
		}

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
		if (!selected || !IsUnit(selected))
			return;

		Unit unit = (Unit)selected;

		if (unit.abilities.Count < index + 1)
			return;

		Ability current = unit.abilities[index];

		if (AbilityUtils.RequiresTarget(current.GetAbilityType()) == 0) // Targets nothing
		{
			unit.OrderAbility(index, null);
		}
		else if(AbilityUtils.RequiresTarget(current.GetAbilityType()) == 1) // Targets a unit
		{
			Entity ent = GetEntityFromHit(RaycastFromCursor(0));
			if (ent && IsUnit(ent))
			{
				AbilityTarget targ = new AbilityTarget((Unit)ent);
				unit.OrderAbility(index, targ);
			}
		}
		else if(AbilityUtils.RequiresTarget(current.GetAbilityType()) == 2) // Targets a position
		{
			RaycastHit hit = RaycastFromCursor(1);
			if (hit.collider)
			{
				AbilityTarget targ = new AbilityTarget(hit.point);
				unit.OrderAbility(index, targ);
			}
		}

	}

	void UseCommandWheel()
	{
		if (!selected || !IsUnit(selected))
			return;

		Unit unit = (Unit)selected;

		RaycastHit hit = RaycastFromCursor(1);
		if (hit.collider)
		{
			AbilityTarget targ = new AbilityTarget(hit.point);
			unit.OrderCommandWheel(2, targ);
		}
		else
			unit.OrderCommandWheel(2, null);
	}

	void UpdateGrid(int newGrid)
	{
		grids[curGrid].SetActive(false);
		curGrid = newGrid;
		grids[curGrid].SetActive(true);
	}

	void UpdateUI(bool selectingNewUnit)
	{
		resPointsCounter.text = resPoints.ToString();

		if (selectingNewUnit && selected && IsUnit(selected))
		{
			Unit unit = (Unit)selected;
			//selectedText.text = "Selected: " + selected.DisplayName;

			Flagship flag = unit.gameObject.GetComponent<Flagship>();

			if (flag)
			{
				buildButtons.SetActive(true);
				//selectedText.text = "|| FLAGSHIP ||";
			}
			else
				buildButtons.SetActive(false);
		}
		else
			buildButtons.SetActive(false);
	}

	public void BuildButton(int id)
	{
		if (buildState != 0)
			return;

		buildUnitIndex = id;

		if (buildUnitCounters[buildUnitIndex] >= buildUnits[buildUnitIndex].unitCap)
			return;

		if (!SubtractResources(buildUnits[buildUnitIndex].cost))
			return;

		buildState = 1;
		buildPreview = Instantiate(buildUnits[buildUnitIndex].previewObject, Vector3.zero, Quaternion.identity);
		buildPreview.SetActive(false);
	}

	void PlacePreview()
	{
		buildState = 2;
	}

	void BuildCancel()
	{
		RefundResources(buildUnits[buildUnitIndex].cost);
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
	}

	void UpdateResources()
	{
		resPointsCounter.text = "" + (int)resPoints;
	}

	bool SubtractResources(float cost)
	{
		float newResPoints = resPoints - cost;

		if (CheckResources(cost))
		{
			resPoints = newResPoints;
			UpdateResources();
			return true;
		}
		else
		{
			return false;
		}
	}

	bool CheckResources(float cost)
	{
		float newResPoints = resPoints - cost;

		if (newResPoints >= 0)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	void RefundResources(float resources)
	{
		resPoints += resources;
		UpdateResources();
	}

	void RefundBuildCap(int index)
	{
		buildUnitCounters[index]--;
		Debug.Log(buildUnitCounters[index]);
	}

	public void RefundUnit(Unit unit)
	{
		if (unit.buildUnitIndex < 0)
			return;
		RefundResources(buildUnits[unit.buildUnitIndex].cost);
		RefundBuildCap(unit.buildUnitIndex);
	}

	bool IsUnit(Entity ent)
	{
		return ent.GetType() == typeof(Unit) || ent.GetType().IsSubclassOf(typeof(Unit));
	}

	public void Select(Entity newSel)
	{
		if (selected)
			selected.OnSelect(this, false);
		selected = newSel;
		if(selected)
			selected.OnSelect(this, true);

		//if (newSel)
		UpdateUI(true);
		//else
			//selectedText.text = "";
	}

	public void Move(Vector3 newPos)
	{
		((Unit)selected).OrderMove(newPos);
		AudioUtils.PlayClipAt(soundMove, transform.position, audioSource);
		Instantiate(clickEffect, newPos, Quaternion.identity);
	}

	public void Target(Entity newTarg)
	{
		if (newTarg && IsUnit(newTarg))
			((Unit)selected).OrderAttack((Unit)newTarg);
	}
}
