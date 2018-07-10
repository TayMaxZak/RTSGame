using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_TooltipSource : MonoBehaviour
{
	//[SerializeField]
	//private string defaultText = "";
	[SerializeField]
	private RectTransform position;
	
	private string displayText;

	//void Awake()
	//{
	//	if (defaultText.Length > 0)
	//		displayText = defaultText;
	//}

	public RectTransform GetRect()
	{
		return position;
	}

	public string GetText()
	{
		return displayText;
	}

	public void SetText(string text)
	{
		displayText = text;
	}
}
