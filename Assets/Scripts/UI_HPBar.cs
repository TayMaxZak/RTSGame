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
	private bool enemy = false;

	[SerializeField]
	private Image healthFill;
	[SerializeField]
	private Image healthBkg;
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
	private Color healthAllyColor = Color.green;
	[SerializeField]
	private Color healthEnemyColor = Color.red;
	[SerializeField]
	private Color healthBurnAllyColor = Color.black;
	[SerializeField]
	private Color healthBurnEnemyColor = Color.black;
	[SerializeField]
	private Color healthBkgAllyColor = Color.black;
	[SerializeField]
	private Color healthBkgEnemyColor = Color.black;

	[SerializeField]
	private Image fragileFill;
	private float fragileCur = 0;
	private float fragileTarg = 1;
	private float fragileT = 0;


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
	private Color armorAllyColor = Color.blue;
	[SerializeField]
	private Color armorEnemyColor = Color.blue;
	[SerializeField]
	private Color armorBkgAllyColor = Color.black;
	[SerializeField]
	private Color armorBkgEnemyColor = Color.black;

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
	}

	// TODO: Investigate why this code has to be in Start and not Awake
	void Start()
	{
		transform.SetSiblingIndex(0); // Draw behind other UI elements
	}

	void Update()
	{
		// Update times
		healthT += Time.deltaTime / uiRules.HPBupdateTime;
		armorT += Time.deltaTime / uiRules.HPBupdateTime;
		shieldT += Time.deltaTime / uiRules.HPBupdateTime;
		fragileT += Time.deltaTime / uiRules.HPBupdateTime;

		UpdateDisplay();

		//if (Random.value > 0.99f)
		//	SetIsAlly(enemy);
	}

	void UpdateDisplay()
	{
		// Update current values
		healthCur = Mathf.Lerp(healthCur, healthTarg, healthT);
		armorCur = Mathf.Lerp(armorCur, armorTarg, armorT);
		shieldCur = Mathf.Lerp(shieldCur, shieldTarg, shieldT);
		fragileCur = Mathf.Lerp(fragileCur, fragileTarg, fragileT);

		// Update sizes of bars
		healthFill.rectTransform.sizeDelta = new Vector2(healthWidth * healthCur, healthFill.rectTransform.sizeDelta.y);
		armorFill.rectTransform.sizeDelta = new Vector2(armorWidth * armorCur, armorFill.rectTransform.sizeDelta.y);
		shieldFill.rectTransform.sizeDelta = new Vector2(shieldWidth * shieldCur, shieldFill.rectTransform.sizeDelta.y);
		// Update sizes and positions of modifier bars
		fragileFill.rectTransform.sizeDelta = new Vector2(Mathf.Min(fragileCur, 1 - healthCur) * healthWidth, fragileFill.rectTransform.sizeDelta.y);
		if (gameObject.activeSelf) // Update rect transform only when active to stop Unity visual bug from happening
			fragileFill.rectTransform.position = new Vector2(healthWidth * healthCur + healthFill.rectTransform.position.x, healthFill.rectTransform.position.y);

		// What color should the border be?
		borderFill.color = armorTarg > uiRules.HPBbordColorThresh ? (enemy ? armorEnemyColor : armorAllyColor) : (enemy ? healthEnemyColor : healthAllyColor);

		// If we are burning,
		if (burning)
		{
			// animate healthbar color between 2 burn colors
			if (burnUp)
			{
				burnT += Random.value * Time.deltaTime / uiRules.HPBblinkTime;
				burnCur = Color.Lerp((enemy ? healthBurnEnemyColor : healthBurnAllyColor), (enemy ? healthEnemyColor : healthAllyColor), burnT);
				if (burnT > 1)
					burnUp = false;
			}
			else
			{
				burnT -= Random.value * Time.deltaTime / uiRules.HPBblinkTime;
				burnCur = Color.Lerp((enemy ? healthBurnEnemyColor : healthBurnAllyColor), (enemy ? healthEnemyColor : healthAllyColor), burnT);
				if (burnT < 0)
					burnUp = true;
			}
			healthFill.color = burnCur;
		}
		else
			healthFill.color = (enemy ? healthEnemyColor : healthAllyColor);
	}

	public override void FastUpdate()
	{
		healthT = 1;
		armorT = 1;
		shieldT = 1;
		fragileT = 1;
		UpdateDisplay();

		foreach (UI_Bar bar in addons)
			bar.FastUpdate();
	}

	public bool SetHealthArmorShield(Vector3 values, bool isBurning)
	{
		burning = isBurning;

		float Tinitial = 0;

		bool newValues = false;

		if (healthTarg != values.x)
		{
			newValues = true;
			healthTarg = values.x;
			healthT = Tinitial;
		}
		if (armorTarg != values.y)
		{
			newValues = true;
			armorTarg = values.y;
			armorT = Tinitial;
		}
		if (shieldTarg != values.z)
		{
			newValues = true;
			shieldTarg = values.z;
			shieldT = Tinitial;
		}

		return newValues;
	}

	public void SetFragileHealth(float frag)
	{
		float Tinitial = 0;

		if (fragileTarg != frag)
		{
			fragileTarg = frag;
			fragileT = Tinitial;
		}
	}

	public void SetIsAlly(bool isAlly)
	{
		// Only update colors if a new value was assigned
		if (enemy != !isAlly)
			enemy = !isAlly;
		else
			return;

		healthFill.color = (enemy ? healthEnemyColor : healthAllyColor);
		healthBkg.color = (enemy ? healthBkgEnemyColor : healthBkgAllyColor);

		armorFill.color = (enemy ? armorEnemyColor : armorAllyColor);
		armorBkg.color = (enemy ? armorBkgEnemyColor : armorBkgAllyColor);
	}


}
