using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UI_Tooltip : MonoBehaviour
{
	[Header("Main")]
	[SerializeField]
	private Text text;
	[SerializeField]
	private RectTransform root; // Used to hide
	//private float rootWidth;

	//[SerializeField]
	//private Image borderFill;

	private UIRules uiRules;

	private UI_TooltipSource current;

	private float timer = 0;

	private Vector3 initialPosition;

	void Awake()
	{
		uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;

		initialPosition = root.position;
	}

	//void Start()
	//{
	//	rootWidth = root.sizeDelta.x;
	//}

	void Update()
	{
		if (timer > uiRules.TTappearTime)
		{
			SetActive(true);
			text.text = current.GetText();
		}
		else
		{
			SetActive(false);
		}

		if (true) // TODO: Remove condition
		{
			PointerEventData pointer = new PointerEventData(EventSystem.current);
			pointer.position = Input.mousePosition;

			List<RaycastResult> raycastResults = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointer, raycastResults);

			if (raycastResults.Count > 0)
			{
				foreach (RaycastResult go in raycastResults)
				{
					UI_TooltipSource src = go.gameObject.GetComponent<UI_TooltipSource>();
					if (src)
					{
						if (src == current)
						{
							timer += Time.deltaTime;
						}
						else
						{
							timer = 0;
							current = src;
						}
					}
				}
			} // raycastResults
			else
			{
				timer = 0;
				current = null;
			}
		}
	}

	void SetActive(bool state)
	{
		if (state && !root.gameObject.activeSelf)
			root.gameObject.SetActive(true);
		else if (!state && root.gameObject.activeSelf)
			root.gameObject.SetActive(false);
	}

	public void SetPosition(Vector3 pos)
	{
		root.position = pos;
	}

	public void ResetPosition()
	{
		root.position = initialPosition;
	}
}
