using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_EntityStats : MonoBehaviour
{
	[Header("Main")]
	[SerializeField]
	private Text nameText;
	[SerializeField]
	private RectTransform root;
	private float rootWidth;

	[Header("Armor")]
	[SerializeField]
	private Text[] armorText;
	[SerializeField]
	private Image armorFill;
	[Header("Health")]
	[SerializeField]
	private Text[] healthText;
	[SerializeField]
	private Image healthFill;

	[Header("Ability 1")]
	[SerializeField]
	private Text[] ability1Text;
	[SerializeField]
	private Image ability1Fill;
	[SerializeField]
	private Image ability1Bkg;
	[SerializeField]
	private Image ability1Icon;
	[SerializeField]
	private Image ability1IconB;

	[Header("Ability 2")]
	[SerializeField]
	private Text[] ability2Text;
	[SerializeField]
	private Image ability2Fill;
	[SerializeField]
	private Image ability2Bkg;
	[SerializeField]
	private Image ability2Icon;
	[SerializeField]
	private Image ability2IconB;

	//[SerializeField]
	//private Image borderFill;

	//private UIRules uiRules;

	void Start()
	{
		rootWidth = root.sizeDelta.x;
	}

	public void SetHealthArmor(float healthCur, float healthMax, float armorCur, float armorMax)
	{
		float healthRatio = healthCur / (healthMax != 0 ? healthMax : 1);
		healthFill.rectTransform.sizeDelta = new Vector2((1 - healthRatio) * -rootWidth, healthFill.rectTransform.sizeDelta.y);
		//healthText.text = (int)healthCur + "/" + (int)healthMax;
		foreach (Text text in healthText)
			text.text = (int)healthCur + "/" + (int)healthMax;

		float armorRatio = armorCur / (armorMax != 0 ? armorMax : 1);
		armorFill.rectTransform.sizeDelta = new Vector2((1 - armorRatio) * -rootWidth, armorFill.rectTransform.sizeDelta.y);
		//armorText.text = (int)armorCur + "/" + (int)armorMax;
		foreach (Text text in armorText)
			text.text = (int)armorCur + "/" + (int)armorMax;
	}

	public void SetDisplayName(string name)
	{
		nameText.text = name.ToUpper();
	}

	public void SetAbilityIcons(AbilityType[] abilities)
	{
		
		if (abilities.Length == 0)
		{
			ability1Bkg.gameObject.SetActive(false);
			ability2Bkg.gameObject.SetActive(false);
		}
		else if (abilities.Length == 1)
		{
			
			ability1Bkg.gameObject.SetActive(true);
			ability1Icon.sprite = AbilityUtils.GetAbilityIcon(abilities[0]);

			ability2Bkg.gameObject.SetActive(false);
		}
		else if (abilities.Length == 2)
		{
			ability1Bkg.gameObject.SetActive(true);
			ability1Icon.sprite = AbilityUtils.GetAbilityIcon(abilities[0]);

			ability2Bkg.gameObject.SetActive(true);
			ability2Icon.sprite = AbilityUtils.GetAbilityIcon(abilities[1]);
		}
	}

	public void SetAbilityProgress(int index, float amount)
	{
		if (index == 0)
		{
			ability1Fill.fillAmount = amount;
		}
		else
		{
			ability2Fill.fillAmount = amount;
		}
	}

	public void SetAbilityStacks(int index, int stacks)
	{
		if (index == 0)
		{
			//ability1Text.text = IntToNumerals(amount);
			foreach (Text text in ability1Text)
				text.text = stacks > 0 ? stacks.ToString() : "";
		}
		else
		{
			//ability2Text.text = IntToNumerals(amount);
			foreach (Text text in ability2Text)
				text.text = stacks > 0 ? stacks.ToString() : "";
		}
	}

	/*
	string IntToNumerals(int number)
	{
		switch (number)
		{
			default:
				return "";
			case 1:
				return "I";
			case 2:
				return "II";
			case 3:
				return "III";
			case 4:
				return "IV";
			case 5:
				return "V";
		}
	}
	*/
}
