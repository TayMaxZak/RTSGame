using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ProgBar : MonoBehaviour
{
	[SerializeField]
	private Image progFill;
	private float progCur = 0;
	[SerializeField]
	private float progFillWidth = 98;

	[SerializeField]
	private Image borderFill;

	//private UIRules uiRules;

	void Start()
	{
		//uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;

		transform.SetSiblingIndex(0); // Draw behind other UI elements
	}

	public void UpdateProgBar(float amount)
	{
		progCur = amount;
		progFill.rectTransform.sizeDelta = new Vector2(progFillWidth * (Mathf.Clamp01(progCur)), progFill.rectTransform.sizeDelta.y);
	}
}
