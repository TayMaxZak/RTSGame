using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_AbilBar_Superlaser : UI_Bar
{
	[SerializeField]
	private Image[] markIcons;
	private int stacks = -1;
	private int maxStacks = -1;

	private bool critical;
	private Color critCur = Color.red;
	private float critT = 0;
	private bool critUp = true;

	[SerializeField]
	private Color markBlinkColor1 = Color.magenta;
	[SerializeField]
	private Color markBlinkColor2 = Color.blue;
	private Color markOrigColor = Color.white;

	private UIRules uiRules;
	private GameRules gameRules;

	void Awake()
	{
		uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
		maxStacks = gameRules.ABLYsuperlaserDmgByStacks.Length - 2;

		markOrigColor = markIcons[0].color;
	}

	void Update()
	{
		// Update times
		UpdateDisplay();
	}

	void UpdateDisplay()
	{
		// If we are burning,
		if (critical)
		{
			// animate healthbar color between 2 burn colors
			if (critUp)
			{
				critT += Time.deltaTime / uiRules.AB_SPblinkTime;
				critCur = Color.Lerp(critCur, markBlinkColor1, critT);
				if (critT > 1)
					critUp = false;
			}
			else
			{
				critT -= Time.deltaTime / uiRules.AB_SPblinkTime;
				critCur = Color.Lerp(critCur, markBlinkColor2, critT);
				if (critT < 0)
					critUp = true;
			}
			markIcons[maxStacks].color = critCur;
		}
		else
			markIcons[maxStacks].color = markOrigColor;
	}

	void UpdateStacks()
	{
		for (int i = 0; i < markIcons.Length; i++)
		{
			if (i < stacks)
				markIcons[i].gameObject.SetActive(true);
			else
				markIcons[i].gameObject.SetActive(false);
		}
	}

	public void SetStacks(int value, bool isCritical)
	{
		critical = isCritical;

		if (stacks != value)
		{
			stacks = value;
			UpdateStacks();
		}
	}
}
