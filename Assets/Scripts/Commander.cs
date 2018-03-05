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

	private Entity selected;

	// Use this for initialization
	void Start ()
	{
		Select(null);
	}
	
	// Update is called once per frame
	void Update ()
	{
		bool notOverUI = !EventSystem.current.IsPointerOverGameObject();
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);

		if (Input.GetMouseButtonDown(0) && notOverUI)
		{
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, clickRayLength, entityLayerMask))
			{
				Entity ent = hit.collider.gameObject.GetComponent<Entity>();
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
				Entity ent = hit.collider.gameObject.GetComponent<Entity>();
				if (ent && ent != selected && selected.GetType() == typeof(Unit))
					Target(ent);
				// Target persists even if you click off of it
			}
			else if (Physics.Raycast(ray, out hit, clickRayLength, gridLayerMask))
			{
				if (selected.GetType() == typeof(Unit))
					Move(hit.point);
			} 
		} //lmb
	}

	public void Select(Entity newSel)
	{
		selected = newSel;
		if (newSel)
			selectedText.text = selected.DisplayName;
		else
			selectedText.text = "";
	}

	public void Move(Vector3 newPos)
	{
		((Unit)selected).OrderMove(newPos);
		Instantiate(clickEffect, newPos, Quaternion.identity);
	}

	public void Target(Entity newTarg)
	{
		if (newTarg)
			((Unit)selected).OrderAttack(newTarg);
		else
			Debug.LogError("Null target!");
	}
}
