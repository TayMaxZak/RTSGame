using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_TooltipSource : MonoBehaviour
{
	private string displayText;

	public string GetText()
	{
		return displayText;
	}

	public void SetText(string text)
	{
		displayText = text;
	}
}
