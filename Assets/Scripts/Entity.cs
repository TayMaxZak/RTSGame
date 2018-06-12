using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EntityType
{
	Default,
	Destroyer,
	Corvette,
	Flagship,
	Frigate,
	Apollo,
	Bulkhead
}

public class Entity : MonoBehaviour
{
	[Header("Entity Properties")]
	[SerializeField]
	private EntityType type;
	[SerializeField]
	protected Transform swarmTarget;
	[SerializeField]
	private float selCircleSize = 1;

	protected GameObject selCircle;
	private float selCircleSpeed;
	protected bool isSelected;
	protected bool isHovered;

	protected Controller_Commander controller;

	public EntityType Type
	{
		get
		{
			return type;
		}
	}

	// Use this for initialization
	protected void Start ()
	{
		Manager_UI uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>();
		selCircle = Instantiate(uiManager.UnitSelCircle, transform.position, Quaternion.identity);
		//selCircle.transform.SetParent(transform);
		selCircle.SetActive(false);
		selCircle.transform.localScale = new Vector3(selCircleSize, selCircleSize, selCircleSize);
		selCircleSpeed = uiManager.UIRules.SELrotateSpeed;
	}
	
	// Update is called once per frame
	protected void Update ()
	{
		selCircle.transform.position = transform.position;
		selCircle.transform.Rotate(Vector3.up * selCircleSpeed * Time.deltaTime);
		//for (int i = 1; i <= selCircle.transform.childCount; i++)
		//{
		//	Transform tran = selCircle.GetComponentsInChildren<Transform>()[i];
		//	float posOrNeg = (i % selCircle.transform.childCount + 1);
		//	Debug.Log(selCircle.transform.childCount);
		//	tran.eulerAngles = new Vector3(0, tran.eulerAngles.y + selCircleSpeed * Time.deltaTime * posOrNeg, 0);
		//	tran.position = transform.position;
		//}
	}

	public virtual void OnHover(bool hovered)
	{
		isHovered = hovered;
	}

	public virtual void OnSelect(bool selected)
	{
		selCircle.SetActive(selected);
		isSelected = selected;
	}

	public virtual void LinkStats(bool detailed, Controller_Commander newController)
	{
		if (detailed)
			controller = newController;
		else
			controller = null;
	}
}

public static class EntityUtils
{
	public static string GetDisplayName(EntityType type)
	{
		switch (type)
		{
			default:
				{
					return "no name";
				}
			case EntityType.Destroyer:
				{
					return "Destroyer";
				}
			case EntityType.Corvette:
				{
					return "Corvette";
				}
			case EntityType.Flagship:
				{
					return "Flagship";
				}
			case EntityType.Frigate:
				{
					return "Frigate";
				}
			case EntityType.Apollo:
				{
					return "Apollo-class";
				}
			case EntityType.Bulkhead:
				{
					return "Bulkhead Cruiser";
				}
		}
	}
}