using UnityEngine;
using System.Collections;

public class StrategyController : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField]
	private float rotSpeed = 3;
	[SerializeField]
	private float speed = 1;
	[SerializeField]
	private float zoomSpeed = 1;

	[Header("Objects")]
	[SerializeField]
	private Camera cam; // Guess
	[SerializeField]
	private GameObject camRoot; // Cam parent

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update ()
	{
		float xAxis = Input.GetAxis("Horizontal");
		camRoot.transform.Translate(new Vector3(xAxis * speed, 0, 0), Space.Self);
		
		float yAxis = Input.GetAxis("Vertical");
		camRoot.transform.Translate(new Vector3(0, 0, yAxis * speed), Space.Self);

		float zoom = Input.GetAxis("Zoom");
		cam.transform.Translate(new Vector3(0, 0, zoom * zoomSpeed), Space.Self);
		cam.transform.position = camRoot.transform.position;

		float rotAxis = Input.GetAxis("Rotate");
		cam.transform.Rotate(new Vector3(0, rotAxis * rotSpeed, 0), Space.World);
		Transform camPam = cam.transform.parent;
		cam.transform.SetParent(null);
		camRoot.transform.Rotate(new Vector3(0, rotAxis * rotSpeed, 0), Space.World);

		if (Input.GetButtonDown("StopRotate"))
		{
			cam.transform.rotation = Quaternion.identity;
		}
	}
}
