using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Manager_UI : MonoBehaviour
{
	[SerializeField]
	public UIRules UIRules;

	[SerializeField]
	public Canvas Canvas;

	[SerializeField]
	public UI_HPBar UnitHPBar;

	[SerializeField]
	public GameObject UnitSelCircle;

	void Update()
	{
		Cursor.lockState = CursorLockMode.Confined;
	}
}
