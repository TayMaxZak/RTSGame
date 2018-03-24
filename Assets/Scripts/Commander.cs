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
		audioSource = GetComponent<AudioSource>();

		Select(null);
	}
	
	// Update is called once per frame
	void Update ()
	{
		bool isUnit = selected ? selected.GetType() == typeof(Unit) : false;

		// Clicking
		bool notOverUI = !EventSystem.current.IsPointerOverGameObject();
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);

		if (Input.GetMouseButtonDown(0) && notOverUI)
		{
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
				if (ent && ent != selected && isUnit)
					Target(ent);
				// Target persists even if you click off of it
			}
			else if (Physics.Raycast(ray, out hit, clickRayLength, gridLayerMask))
			{
				if (selected.GetType() == typeof(Unit))
					Move(hit.point);
			} 
		} //lmb

		// Abilities
		if (Input.GetButtonDown("Ability1"))
		{
			if (!selected || !isUnit)
				return;

			Debug.Log(why);
			((Unit)selected).Damage(26.68f, 10);
		}

		UpdateUI();
	}

	public void UpdateUI()
	{
		Unit unit = null;
		if (selected && selected.GetType() == typeof(Unit))
		{
			unit = (Unit)selected;
			selectedText.text = selected.DisplayName + (unit ? " " + (int)unit.GetHP().x + "|" + (int)unit.GetHP().z : "");
		}
	}

	public void Select(Entity newSel)
	{
		selected = newSel;

		Unit unit = null;
		if (selected && selected.GetType() == typeof(Unit))
			unit = (Unit)selected;

		if (newSel)
			selectedText.text = selected.DisplayName + (unit ? " " + (int)unit.GetHP().x : "");
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
		if (newTarg && newTarg.GetType() == typeof(Unit))
			((Unit)selected).OrderAttack((Unit)newTarg);
	}
}
