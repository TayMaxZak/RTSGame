using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_CommandWheel : MonoBehaviour
{
	[SerializeField]
	private RectTransform root;
	[SerializeField]
	private RectTransform spinner;

	[SerializeField]
	private Image[] itemBkgs;

	[SerializeField]
	private int deadRadius = 30;

	private bool open = false;

	private Controller_Commander control;
	private int toUse;
	
	private Manager_UI uiManager;

	void Awake()
	{
		uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<Manager_UI>();
	}

	public void SetController(Controller_Commander newControl)
	{
		control = newControl;
	}

	void Update()
	{
		//Debug.Log(Input.mousePosition);
		if (open)
		{
			Vector3 mousePosition = Input.mousePosition;
			Vector3 rootPosition = RectTransformUtility.WorldToScreenPoint(null, root.position);

			mousePosition -= rootPosition;

			if (mousePosition.sqrMagnitude > deadRadius * deadRadius)
			{
				if (Mathf.Abs(mousePosition.x) > Mathf.Abs(mousePosition.y))
				{
					if (mousePosition.x > 0)
					{ // RIGHT
						toUse = 1;
					}
					else
					{ // LEFT
						toUse = 3;
					}
				}
				else
				{
					if (mousePosition.y > 0)
					{ // TOP
						toUse = 4;
					}
					else
					{ // BOTTOM
						toUse = 2;
					}
				}
			} // outside dead radius
			else
				toUse = 0;

			// Rotate spinner
			if (toUse != 0)
			{
				spinner.gameObject.SetActive(true);
				spinner.up = (Input.mousePosition - spinner.position).normalized;
			}
			else
			{
				spinner.gameObject.SetActive(false);
			}

		} // open
		else
		{
			
		}
	}

	public void SetCommandWheelActive(bool isActive)
	{
		
		if (isActive && !root.gameObject.activeSelf)
		{
			open = true; // Now active

			root.gameObject.SetActive(true); // Show UI
			root.transform.position = Input.mousePosition; // Set position of UI. NOTE: This MUST be after the root is set active

			uiManager.SetCursorState(1); // Hide cursor
		}
		else if (!isActive && root.gameObject.activeSelf)
		{
			open = false; // No longer active

			root.gameObject.SetActive(false); // Hide UI

			control.UseCommandWheel(toUse); // Do appropriate action
			toUse = 0; // Reset toUse to default value

			uiManager.SetCursorState(0); // Show cursor
		}
	}
}
