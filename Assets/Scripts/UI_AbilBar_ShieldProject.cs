using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_AbilBar_ShieldProject : UI_Bar
{
	[SerializeField]
	private Image shieldFill;
	private float shieldCur = 0;
	private float shieldTarg = 1;
	private float shieldT = 0;
	[SerializeField]
	private float shieldWidth = 98;

	private bool critical;
	private Color critCur = Color.red;
	private float critT = 0;
	private bool critUp = true;

	[SerializeField]
	private Color shieldCriticalColor1 = Color.magenta;
	[SerializeField]
	private Color shieldCriticalColor2 = Color.blue;
	private Color shieldOrigColor = Color.white;

	private UIRules uiRules;
	//private GameRules gameRules;

	void Awake()
	{
		uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;
		//gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;

		shieldOrigColor = shieldFill.color;
	}

	void Update()
	{
		// Update times
		shieldT += Time.deltaTime / uiRules.AB_SP_updateTime;

		UpdateDisplay();
	}

	void UpdateDisplay()
	{
		// Update current values
		shieldCur = Mathf.Lerp(shieldCur, shieldTarg, shieldT);

		// Update sizes of bar, showing negative values as positive
		shieldFill.rectTransform.sizeDelta = new Vector2(shieldWidth * Mathf.Abs(shieldCur), shieldFill.rectTransform.sizeDelta.y);

		// If we are burning,
		if (critical)
		{
			// animate healthbar color between 2 burn colors
			if (critUp)
			{
				critT += Time.deltaTime / uiRules.AB_SP_blinkTime;
				critCur = Color.Lerp(critCur, shieldCriticalColor1, critT);
				if (critT > 1)
					critUp = false;
			}
			else
			{
				critT -= Time.deltaTime / uiRules.AB_SP_blinkTime;
				critCur = Color.Lerp(critCur, shieldCriticalColor2, critT);
				if (critT < 0)
					critUp = true;
			}
			shieldFill.color = critCur;
		}
		else
			shieldFill.color = shieldOrigColor;
	}

	public override void FastUpdate()
	{
		shieldT = 1;
		UpdateDisplay();
	}

	public void SetShield(float value, bool isCritical)
	{
		critical = isCritical;

		float Tinitial = 0;

		if (shieldTarg != value)
		{
			shieldTarg = value;
			shieldT = Tinitial;
		}
	}
}
