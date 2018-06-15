using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Bar : MonoBehaviour
{
	public virtual void FastUpdate()
	{ }
}

public class UI_HPBar : UI_Bar
{
	[SerializeField]
	private Image healthFill;
	private float healthCur = 0;
	private float healthTarg = 1;
	private float healthT = 0;
	[SerializeField]
	private float healthWidth = 98;

	private bool burning = false;
	private Color burnCur = Color.red;
	private float burnT = 0;
	private bool burnUp = true;
	[SerializeField]
	private Color healthBurnColor1 = Color.red;
	[SerializeField]
	private Color healthBurnColor2 = Color.white;
	private Color healthOrigColor = Color.green;

	[SerializeField]
	private Image armorFill;
	private float armorCur = 0;
	private float armorTarg = 1;
	private float armorT = 0;
	[SerializeField]
	private float armorWidth = 98;
	private Color armorOrigColor = Color.blue;

	[SerializeField]
	private Image shieldFill;
	private float shieldCur = 0;
	private float shieldTarg = 1;
	private float shieldT = 0;
	[SerializeField]
	private float shieldWidth = 98;
	//private Color shieldOrigColor = Color.blue;

	[SerializeField]
	private Image borderFill;

	[SerializeField]
	private List<UI_Bar> addons;

	private UIRules uiRules;
	//private GameRules gameRules;

	void Awake()
	{
		uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;
		//gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;

		healthOrigColor = healthFill.color;
		armorOrigColor = armorFill.color;
		//shieldOrigColor = shieldFill.color;

		transform.SetSiblingIndex(0); // Draw behind other UI elements
	}

	void Update()
	{
		// Update times
		healthT += Time.deltaTime / uiRules.HPBupdateTime;
		armorT += Time.deltaTime / uiRules.HPBupdateTime;
		shieldT += Time.deltaTime / uiRules.HPBupdateTime;

		UpdateDisplay();
	}

	void UpdateDisplay()
	{
		// Update current values
		healthCur = Mathf.Lerp(healthCur, healthTarg, healthT);
		armorCur = Mathf.Lerp(armorCur, armorTarg, armorT);
		shieldCur = Mathf.Lerp(shieldCur, shieldTarg, shieldT);

		// Update sizes of bars
		healthFill.rectTransform.sizeDelta = new Vector2(healthWidth * healthCur, healthFill.rectTransform.sizeDelta.y);
		armorFill.rectTransform.sizeDelta = new Vector2(armorWidth * armorCur, armorFill.rectTransform.sizeDelta.y);
		shieldFill.rectTransform.sizeDelta = new Vector2(shieldWidth * shieldCur, shieldFill.rectTransform.sizeDelta.y);

		// What color should the border be?
		borderFill.color = armorTarg > uiRules.HPBbordColorThresh ? armorOrigColor : healthOrigColor;

		// If we are burning,
		if (burning)
		{
			// animate healthbar color between 2 burn colors
			if (burnUp)
			{
				burnT += Random.value * Time.deltaTime / uiRules.HPBblinkTime;
				burnCur = Color.Lerp(burnCur, healthBurnColor1, burnT);
				if (burnT > 1)
					burnUp = false;
			}
			else
			{
				burnT -= Random.value * Time.deltaTime / uiRules.HPBblinkTime;
				burnCur = Color.Lerp(burnCur, healthBurnColor2, burnT);
				if (burnT < 0)
					burnUp = true;
			}
			healthFill.color = burnCur;
		}
		else
			healthFill.color = healthOrigColor;
	}

	public override void FastUpdate()
	{
		healthT = 1;
		armorT = 1;
		shieldT = 1;
		UpdateDisplay();

		foreach (UI_Bar bar in addons)
			bar.FastUpdate();
	}

	public void SetHealthArmorShield(Vector3 values, bool isBurning)
	{
		burning = isBurning;

		float Tinitial = 0;

		if (healthTarg != values.x)
		{
			healthTarg = values.x;
			healthT = Tinitial;
		}
		if (armorTarg != values.y)
		{
			armorTarg = values.y;
			armorT = Tinitial;
		}
		if (shieldTarg != values.z)
		{
			shieldTarg = values.z;
			shieldT = Tinitial;
		}
	}
}
