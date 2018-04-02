﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_HPBar : MonoBehaviour
{
	[SerializeField]
	private Image healthFill;
	[SerializeField]
	private Image healthBkg;
	private float healthCur = 0;
	private float healthTarg = 1;
	private float healthT = 0;
	[SerializeField]
	private float healthWidth = 98;
	[SerializeField]
	private Color healthBurnColor1 = Color.red;
	[SerializeField]
	private Color healthBurnColor2 = Color.white;
	private Color healthOrigColor = Color.green;

	[SerializeField]
	private Image armorFill;
	[SerializeField]
	private Image armorBkg;
	private float armorCur = 0;
	private float armorTarg = 1;
	private float armorT = 0;
	[SerializeField]
	private float armorWidth = 98;
	[SerializeField]
	private Color armorBurnColor1 = Color.magenta;
	[SerializeField]
	private Color armorBurnColor2 = Color.white;
	private Color armorOrigColor = Color.blue;

	[SerializeField]
	private Image borderFill;

	private UIRules uiRules;
	private GameRules gameRules;

	void Start()
	{
		uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;

		healthOrigColor = healthFill.color;
		armorOrigColor = armorFill.color;
	}

	void Update()
	{
		healthT += Time.deltaTime / uiRules.HPBvalUpdateTime;
		healthCur = Mathf.Lerp(healthCur, healthTarg, healthT);

		armorT += Time.deltaTime / uiRules.HPBvalUpdateTime;
		armorCur = Mathf.Lerp(armorCur, armorTarg, armorT);



		healthFill.rectTransform.sizeDelta = new Vector2(healthWidth * healthCur, healthFill.rectTransform.sizeDelta.y);
		armorFill.rectTransform.sizeDelta = new Vector2(armorWidth * armorCur, armorFill.rectTransform.sizeDelta.y);

		borderFill.color = armorCur > uiRules.HPBbordColorThresh ? armorOrigColor : healthOrigColor;

		if (healthCur < gameRules.HLTHthreshBurn)
		{
			// Animate between 2 burn colors
			if (Time.frameCount % 2 == 0)
				healthFill.color = healthBurnColor1;
			else
				healthFill.color = healthBurnColor2;
		}
		else
			healthFill.color = healthOrigColor;
	}

	public void UpdateHPBar(float health, float armor)
	{
		healthTarg = health;
		healthT = 0;

		armorTarg = armor;
		armorT = 0;
	}
}
