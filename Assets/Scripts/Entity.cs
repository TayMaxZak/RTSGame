using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
	[Header("Entity Properties")]
	[SerializeField]
	private string displayName = "Default Name";
	[SerializeField]
	protected Transform swarmTarget;
	[SerializeField]
	private float selCircleSize = 1;

	protected GameObject selCircle;
	private float selCircleSpeed;
	protected bool isSelected;

	public string DisplayName
	{
		get
		{
			return displayName;
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

	public void OnSelect(Commander selector, bool selectOrDeselect)
	{
		selCircle.SetActive(selectOrDeselect);
		isSelected = selectOrDeselect;
		//Debug.Log(DisplayName + " BTW");
	}
}
