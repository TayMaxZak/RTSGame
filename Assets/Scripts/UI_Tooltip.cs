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
	private RectTransform tooltipRoot; // Used to hide
	[SerializeField]
	private RectTransform resetPosition; // Used to reset position after moving tooltip
	//private float rootWidth;

	[Header("Audio")]
	[SerializeField]
	private AudioSource tooltipAudio;

	//[SerializeField]
	//private Image borderFill;

	private UIRules uiRules;

	private UI_TooltipSource current;

	private float timer = 0;

	void Awake()
	{
		uiRules = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>().UIRules;
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
			RectTransform rect = current.GetRect();
			if (rect)
				SetPosition(rect);
			else
				ResetPosition();
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
							tooltipAudio.pitch = 1 + RandomValue() * uiRules.TTaudioPitchVariance;
							tooltipAudio.Play();
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
		if (state && !tooltipRoot.gameObject.activeSelf)
		{
			tooltipRoot.gameObject.SetActive(true);
		}
		else if (!state && tooltipRoot.gameObject.activeSelf)
			tooltipRoot.gameObject.SetActive(false);
	}

	public void SetPosition(RectTransform rect)
	{
		tooltipRoot.position = rect.position;
		//tooltipRoot.SetParent(rect);
		//tooltipRoot.anchoredPosition = Vector2.zero;
		//tooltipRoot.SetParent(transform); // Reset parent
	}

	public void ResetPosition()
	{
		tooltipRoot.position = resetPosition.position;
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
	}
}
