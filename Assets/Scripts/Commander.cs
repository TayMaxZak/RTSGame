using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Commander : MonoBehaviour
{
	[Header("GUI")]
	[SerializeField]
	private Text selectedText;

	[Header("Sound")]
	[SerializeField]
	private AudioClip soundMove;

	[Header("Clicking")]
	[SerializeField]
	private Camera cam;
	[SerializeField]
	private GameObject clickEffect;
	[SerializeField]
	private float clickRayLength = 100;
	[SerializeField]
	private LayerMask entityLayerMask;
	[SerializeField]
	private LayerMask gridLayerMask;

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
	}
	
	// Update is called once per frame
	void Update ()
	{
		//UpdateUI(false);

		// Clicking
		bool notOverUI = !EventSystem.current.IsPointerOverGameObject();
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);

		if (Input.GetMouseButtonDown(0) && notOverUI)
		{
			Debug.Log(Raycast());
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, clickRayLength, entityLayerMask))
			{
				Entity ent = hit.collider.GetComponentInParent<Entity>();
				if (ent)
				{
					Select(ent);
				}
				else
				{
					Select(null);
				}
			}
			else
			{
				Select(null);
			}
		} //lmb
		else if (Input.GetMouseButtonDown(1) && notOverUI)
		{
			if (!selected)
				return;

			RaycastHit hit;
			// First check if we clicked on an entity, if not then cast through entities to a point on the grid
			if (Physics.Raycast(ray, out hit, clickRayLength, entityLayerMask))
			{
				Entity ent = hit.collider.gameObject.GetComponentInParent<Entity>();
				if (ent && ent != selected && IsUnit(ent))
					Target(ent);
				// Target persists even if you click off of it
			}
			else if (Physics.Raycast(ray, out hit, clickRayLength, gridLayerMask))
			{
				if (IsUnit(selected))
					Move(hit.point);
			} 
		} //lmb

		// Abilities
		if (Input.GetButtonDown("Ability1"))
		{
			RaycastHit hit;
			if (selected && IsUnit(selected))
			{
				Unit user = (Unit)selected;
				if (Physics.Raycast(ray, out hit, clickRayLength, entityLayerMask))
				{
					Entity ent = hit.collider.gameObject.GetComponentInParent<Entity>();
					Unit targ = (ent && IsUnit(ent)) ? targ = (Unit)ent : null;
					if (targ)
						user.UseAbility(0, new Ability_Target(targ));
				}
				else if (Physics.Raycast(ray, out hit, clickRayLength, gridLayerMask))
				{
					user.UseAbility(0, new Ability_Target(hit.point));
				}
				//Debug.Log(why);
				//Unit unit = (Unit)selected;
				//unit.UseAbility(0);
			}
		}
	}

	void UpdateUI(bool newUnit)
	{
		Unit unit = null;
		if (newUnit && selected && IsUnit(selected))
		{
			unit = (Unit)selected;
			selectedText.text = "Selected: " + selected.DisplayName;

			Flagship flag = unit.gameObject.GetComponent<Flagship>();

			if (flag)
			{
				selectedText.text = "|| FLAGSHIP ||";
			}
		}
	}

	bool IsUnit(Entity ent)
	{
		return ent.GetType() == typeof(Unit) || ent.GetType().IsSubclassOf(typeof(Unit));
	}

	RaycastHit Raycast()
	{
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, clickRayLength, entityLayerMask))
		{

		}
		else if (Physics.Raycast(ray, out hit, clickRayLength, gridLayerMask))
		{

		}
		return hit;
	}

	public void Select(Entity newSel)
	{
		if (selected)
			selected.OnSelect(this, false);
		selected = newSel;
		if(selected)
			selected.OnSelect(this, true);

		if (newSel)
			UpdateUI(true);
		else
			selectedText.text = "";
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
