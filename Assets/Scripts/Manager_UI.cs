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

	//[SerializeField]
	//public UI_HPBar UnitHPBar;

	[SerializeField]
	public GameObject UnitSelCircle;

	private int cursorState = 0; // 0 = confined, 1 = middle of screen

	void Update()
	{
		Cursor.lockState = CursorLockMode.Confined;
		if (cursorState == 1)
			Cursor.visible = false;
		else
			Cursor.visible = true;
	}

	public void SetCursorState(int newState)
	{
		cursorState = newState;
	}
}
