using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_BuildButton : MonoBehaviour
{
	//[SerializeField]
	//private int team;
	private int index;

	[SerializeField]
	private Text costText;
	private int costCur;
	[SerializeField]
	private Text countText;
	private int countCur;
	[SerializeField]
	private Button button;
	[SerializeField]
	private Image buttonIcon;

	private Color textInitColor;
	[SerializeField]
	private Color textInactiveColor = Color.black;
	private Color buttonIconInitColor;
	[SerializeField]
	private Color buttonIconInactiveColor = Color.black;

	private bool[] interactable; // [0] false = not enough resources, [1] false = not enough counter
	private bool usable = false; // Internally tracks whether or not the build action can be used

	//[SerializeField]
	//private Image borderFill;

	//private UIRules uiRules;
	//private Manager_Game gameManager;

	private Controller_Commander controller;
	private Commander commander;

	void Start()
	{
		//uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;
		//gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>();

		interactable = new bool[2];

		textInitColor = costText.color;
		buttonIconInitColor = buttonIcon.color;
	}

	public void SetController(Controller_Commander newController)
	{
		controller = newController;
		commander = controller.GetCommander();
	}

	public void SetIndex(int i)
	{
		index = i;
	}

	public void Build()
	{
		if (!usable)
			return;

		controller.UseBuildButton(index);
	}

	public void UpdateInteractable()
	{
		if (costCur > commander.GetResources())
			interactable[0] = false;
		else
			interactable[0] = true;

		if (countCur >= commander.GetBuildUnit(index).unitCap)
			interactable[1] = false;
		else
			interactable[1] = true;

		bool canInteract = ButtonInteractable();
		button.interactable = canInteract;
		usable = canInteract;

		costText.color = canInteract ? textInitColor: textInactiveColor;
		countText.color = canInteract ? textInitColor : textInactiveColor;
		buttonIcon.color = canInteract ? buttonIconInitColor : buttonIconInactiveColor;
	}

	public void SetCost()
	{
		costCur = commander.GetBuildUnit(index).cost;
		costText.text = costCur.ToString();
		UpdateInteractable();
	}

	public void SetCounter(int cur)
	{
		countCur = cur;
		countText.text = countCur + "/" + commander.GetBuildUnit(index).unitCap;
		UpdateInteractable();
	}

	bool ButtonInteractable()
	{
		for (int i = 0; i < interactable.Length; i++)
		{
			if (interactable[i] == false)
				return false;
		}
		return true;
	}
}
