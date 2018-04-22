using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Commander : MonoBehaviour
{
	[Header("GUI")]
	[SerializeField]
	private Text resPointCounter;
	[SerializeField]
	private GameObject buildButtons;
	[SerializeField]
	private bool buildModeActive;
	private int buildIndex;
	[SerializeField]
	private Unit[] buildUnits;

	[Header("Objectives")]
	[SerializeField]
	private int resPoints = 20;

	[Header("Sound")]
	[SerializeField]
	private AudioClip soundMove;

	[Header("Clicking")]
	[SerializeField]
	private Camera cam;
	[SerializeField]
	private GameObject clickEffect;
	private float clickRayLength = 200;
	[SerializeField]
	private LayerMask entityLayerMask;
	[SerializeField]
	private LayerMask gridLayerMask;

	[Header("Grids")]
	[SerializeField]
	private GameObject[] grids;
	[SerializeField]
	private int defaultGrid = 1;
	private int curGrid;

	[Header("Using Abilities")]
	[SerializeField]
	private string why;

	private Entity selected;
	private AudioSource audioSource;

	// Use this for initialization
	void Start ()
	{
		//Time.timeScale = 0.51f;

		audioSource = GetComponent<AudioSource>();

		Select(null);
		UpdateUI(false);

		curGrid = defaultGrid;
		foreach (GameObject go in grids)
			go.SetActive(false);
		UpdateGrid(curGrid);
	}

	RaycastHit RaycastFromCursor()
	{
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, clickRayLength, entityLayerMask)) {}
			else if (Physics.Raycast(ray, out hit, clickRayLength, gridLayerMask)) {}
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

		// Clicking
		if (Input.GetMouseButtonDown(0))
		{
			//Debug.Log(RayFromMouse());
			RaycastHit hit = RaycastFromCursor();

			
			if (!EventSystem.current.IsPointerOverGameObject()) // Even a failed raycast would still result in a Select(null) call without this check in place
			{
				Entity ent = GetEntityFromHit(hit);
				if (!buildModeActive) // Normal select mode
				{
					if (ent)
						Select(ent);
					else
						Select(null);
				}
				else if (hit.collider) // Building mode
				{
					if (!ent) // Make sure we didn't click on top of an existing entity
						Build(hit);
				}
			}
			
		} //lmb
		else if (selected && Input.GetMouseButtonDown(1))
		{
			RaycastHit hit;

			hit = RaycastFromCursor();
			Entity ent = GetEntityFromHit(hit);
			if (ent)
			{
				if (ent != selected && IsUnit(ent))
					Target(ent);
			}
			else if (hit.collider && IsUnit(selected))
				Move(hit.point);
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
			unit.UseAbility(index, null);
		}
		else if(AbilityUtils.RequiresTarget(current.GetAbilityType()) == 1) // Targets a unit
		{
			Entity ent = GetEntityFromHit(RaycastFromCursor());
			if (IsUnit(ent))
			{
				Ability_Target targ = new Ability_Target((Unit)ent);
				unit.UseAbility(index, targ);
			}
		}
		else if(AbilityUtils.RequiresTarget(current.GetAbilityType()) == 2) // Targets a position
		{
			RaycastHit hit = RaycastFromCursor();
			if (hit.collider)
			{
				Ability_Target targ = new Ability_Target(hit.point);
				unit.UseAbility(index, targ);
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
		resPointCounter.text = resPoints.ToString();

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
		buildModeActive = true;
		buildIndex = id;
		
	}

	void Build(RaycastHit hit)
	{
		Debug.Log("my id is " + buildIndex);
		Instantiate(buildUnits[buildIndex], hit.point, Quaternion.identity);
		buildModeActive = false;
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
