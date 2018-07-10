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
	private UI_TooltipSource nameTooltip;
	[SerializeField]
	private RectTransform root;
	private float rootWidth;

	[Header("Health")]
	[SerializeField]
	private Text[] healthText;
	[SerializeField]
	private Image healthFill;
	[SerializeField]
	private UI_TooltipSource healthTooltip;

	[Header("Armor")]
	[SerializeField]
	private Text[] armorText;
	[SerializeField]
	private Image armorFill;
	[SerializeField]
	private UI_TooltipSource armorTooltip;

	[Header("Shields")]
	[SerializeField]
	private Text[] shieldsText;
	[SerializeField]
	private Image shieldBkg;
	[SerializeField]
	private UI_TooltipSource shieldTooltip;

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
	private int ability1IconBstate = 0;

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
	private int ability2IconBstate = 0;

	[Header("Statuses")]
	[SerializeField]
	private Image[] statusBkgs;

	//[SerializeField]
	//private Image borderFill;

	private UIRules uiRules;
	private GameRules gameRules;

	void Awake()
	{
		uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;
		gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
	}

	void Start()
	{
		rootWidth = root.sizeDelta.x;
	}

	void Update()
	{
		// Animate secondary icons according to their state
		if (ability1Bkg.gameObject.activeSelf)
		{
			if (ability1IconBstate == 1)
				ability1IconB.transform.Rotate(-Vector3.forward * Time.deltaTime * uiRules.ESiconBState1Speed);
			else if (ability1IconBstate == 2)
				ability1IconB.transform.Rotate(-Vector3.forward * Time.deltaTime * uiRules.ESiconBState2Speed);
		}
		if (ability2Bkg.gameObject.activeSelf)
		{
			
			if (ability2IconBstate == 1)
				ability2IconB.transform.Rotate(-Vector3.forward * Time.deltaTime * uiRules.ESiconBState1Speed);
			else if (ability2IconBstate == 2)
				ability2IconB.transform.Rotate(-Vector3.forward * Time.deltaTime * uiRules.ESiconBState2Speed);
		}
	}

	void ResetIconBTransform(int index)
	{
		if (index == 0)
			ability1IconB.transform.rotation = Quaternion.identity;
		else
			ability2IconB.transform.rotation = Quaternion.identity;
	}

	public void SetHealthArmor(float healthCur, float healthMax, float armorCur, float armorMax)
	{
		SetHealthArmor(healthCur, healthMax, armorCur, armorMax, true);
	}
	// Set health and armor values, which are translated into fill amounts and text numbers
	public void SetHealthArmor(float healthCur, float healthMax, float armorCur, float armorMax, bool burnPossible)
	{
		float healthRatio = healthCur / (Mathf.RoundToInt(healthMax) != 0 ? healthMax : 1);
		healthFill.rectTransform.sizeDelta = new Vector2((1 - healthRatio) * -rootWidth, healthFill.rectTransform.sizeDelta.y);

		foreach (Text text in healthText)
			text.text = StringFromFloat(healthCur) + "/" + StringFromFloat(healthMax);

		// Tooltip
		healthTooltip.SetText(!burnPossible ? "Burn Immune\nEven if this unit's health is below the burn threshold, it will not take burn damage over time." : 
			string.Format("Burn threshold: {0:0}\nIf health drops below this threshold, it will start taking {1:0} damage over every 10 seconds.", healthMax * gameRules.HLTHburnThresh, (gameRules.HLTHburnMin + gameRules.HLTHburnMax) * 5));


		float armorRatio = armorCur / (Mathf.RoundToInt(armorMax) != 0 ? armorMax : 1);
		armorFill.rectTransform.sizeDelta = new Vector2((1 - armorRatio) * -rootWidth, armorFill.rectTransform.sizeDelta.y);

		foreach (Text text in armorText)
			text.text = StringFromFloat(armorCur) + "/" + StringFromFloat(armorMax);

		string armorToolotipText = Mathf.RoundToInt(armorMax) == 0 ? "This unit has no armor." : 
			Mathf.FloorToInt(armorCur) < gameRules.ABLYarmorDrainGPS * 2 ? "Armor has been destroyed." : 
			string.Format("Absorption limit: {0:0.0}\nCan take up to {0:0.0} damage in one shot before letting excess damage through to health.", (armorCur / armorMax) * gameRules.ARMabsorbMax + gameRules.ARMabsorbFlat);

		//Tooltip
		armorTooltip.SetText(armorToolotipText);
	}

	// Set visibility of the shield icon and set text number to be displayed next to the shield icon
	public void SetShields(float shieldsCur, float shieldsMax)
	{
		if (Mathf.RoundToInt(shieldsMax) == 0)
			shieldBkg.gameObject.SetActive(false);
		else
			shieldBkg.gameObject.SetActive(true);

		foreach (Text text in shieldsText)
			text.text = StringFromFloat(shieldsCur) + "/" + StringFromFloat(shieldsMax);

		shieldTooltip.SetText(Mathf.RoundToInt(shieldsMax) != 0 ?
			 string.Format("Current shield pool: {0:0.0} / {0:0.0}\nShields take damage before this unit's health pool and regenerate over time.", shieldsCur, shieldsMax) :
			"This unit has no shields.");
	}

	// Set name to be displayed as the title of the EntityStats panel
	public void SetDisplayEntity(EntityType type)
	{
		nameText.text = EntityUtils.GetDisplayName(type).ToUpper();
		nameTooltip.SetText(EntityUtils.GetDisplayDesc(type));
	}

	// Set what image is displayed for each ability slot
	public void SetAbilityIconsAndInfo(AbilityType[] abilities)
	{	
		if (abilities.Length == 0)
		{
			ability1Bkg.gameObject.SetActive(false);
			ability2Bkg.gameObject.SetActive(false);
		}
		else if (abilities.Length == 1)
		{
			ability1Bkg.gameObject.SetActive(true);
			ability1Icon.sprite = AbilityUtils.GetDisplayIcon(abilities[0]);
			ability1Bkg.GetComponent<UI_TooltipSource>().SetText(AbilityUtils.GetDisplayName(abilities[0]) + "\n" + AbilityUtils.GetDisplayDesc(abilities[0]));

			ability2Bkg.gameObject.SetActive(false);
		}
		else if (abilities.Length == 2)
		{
			ability1Bkg.gameObject.SetActive(true);
			ability1Icon.sprite = AbilityUtils.GetDisplayIcon(abilities[0]);
			ability1Bkg.GetComponent<UI_TooltipSource>().SetText(AbilityUtils.GetDisplayName(abilities[0]) + "\n" + AbilityUtils.GetDisplayDesc(abilities[0]));

			ability2Bkg.gameObject.SetActive(true);
			ability2Icon.sprite = AbilityUtils.GetDisplayIcon(abilities[1]);
			ability2Bkg.GetComponent<UI_TooltipSource>().SetText(AbilityUtils.GetDisplayName(abilities[1]) + "\n" + AbilityUtils.GetDisplayDesc(abilities[1]));
		}
	}

	// Set secondary image which is overlayed over the specified ability slot and set how it should be animated
	/// <summary>
	/// 0 = no behaviour, 1 = rotate clockwise, 2 = rotate counterclockwise slowly
	/// </summary>
	/// <param name="index"></param>
	/// <param name="ability"></param>
	/// <param name="animState"></param>
	public void SetAbilityIconB(int index, AbilityType ability, int animState)
	{
		// If it is any state which should not rotate, reset rotation
		if (animState == 0)
			ResetIconBTransform(index);

		if (index == 0)
		{
			ability1IconB.sprite = AbilityUtils.GetDisplayIconB(ability);
			ability1IconBstate = animState;
		}
		else
		{
			ability2IconB.sprite = AbilityUtils.GetDisplayIconB(ability);
			ability2IconBstate = animState;
		}
	}

	// Clear secondary image from the specified ability slot
	public void ClearAbilityIconB(int index)
	{
		ResetIconBTransform(index);

		if (index == 0)
		{
			ability1IconB.sprite = AbilityUtils.GetDisplayIcon(AbilityType.Default); // This ability type has an empty icon
			ability1IconBstate = 0; // Reset anim state
		}
		else
		{
			ability2IconB.sprite = AbilityUtils.GetDisplayIcon(AbilityType.Default); // This ability type has an empty icon
			ability2IconBstate = 0; // Reset anim state
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
				text.text = stacks > 0 ? StringFromInt(stacks) : "";
		}
		else
		{
			//ability2Text.text = IntToNumerals(amount);
			foreach (Text text in ability2Text)
				text.text = stacks > 0 ? StringFromInt(stacks) : "";
		}
	}

	public void SetStatuses(List<Status> statuses)
	{
		// Loop through all statuses
		List<StatusType> displayedStatuses = new List<StatusType>();

		int swarmResistCount = 0;
		foreach (Status s in statuses)
		{
			// Only display a relevant number of SwarmResist icons
			if (s.statusType == StatusType.SwarmResist)
			{
				swarmResistCount++;
				if (swarmResistCount > gameRules.STATswarmResistMaxStacks)
					continue;
			}

			if (StatusUtils.ShouldDisplay(s.statusType))
				displayedStatuses.Add(s.statusType);
		}

		// For each status icon, enable or disable, apply colors, and set inner icon
		for (int i = 0; i < statusBkgs.Length; i++)
		{
			if (i < displayedStatuses.Count)
			{
				statusBkgs[i].gameObject.SetActive(true);

				Color[] colors = StatusUtils.GetDisplayColors(displayedStatuses[i]);
				statusBkgs[i].color = colors[0];
				statusBkgs[i].GetComponent<UI_TooltipSource>().SetText(StatusUtils.GetDisplayName(displayedStatuses[i]) + "\n" + StatusUtils.GetDisplayDesc(displayedStatuses[i]));

				Image icon = statusBkgs[i].GetComponentsInChildren<Image>()[1];
				icon.color = colors[1];
				icon.sprite = StatusUtils.GetDisplayIcon(displayedStatuses[i]);
			}
			else
				statusBkgs[i].gameObject.SetActive(false);
		}
	}

	string StringFromFloat(float number)
	{
		int val = Mathf.CeilToInt(number);
		return val.ToString();
	}

	string StringFromInt(int number)
	{
		if (number <= 9)
			return number.ToString();
		else
			return "9+";
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
