using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_ResCounter : MonoBehaviour
{
	[SerializeField]
	private Text resText;
	[SerializeField]
	private Text recText;

	[SerializeField]
	private Image timeFill;
	[SerializeField]
	private Vector2 minMaxFill = new Vector2(0, 1);
	private float timeCur = 0;

	//[SerializeField]
	//private Image borderFill;

	//private UIRules uiRules;

	void Start()
	{
		//uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;

		transform.SetSiblingIndex(0); // Draw behind other UI elements
	}

	public void UpdateResCounter(int res, int rec)
	{
		resText.text = res.ToString();
		recText.text = rec.ToString();
	}

	public void UpdateTime(float time)
	{
		timeCur = time;
		timeFill.fillAmount = minMaxFill.x + timeCur * (minMaxFill.y - minMaxFill.x);
	}
}
