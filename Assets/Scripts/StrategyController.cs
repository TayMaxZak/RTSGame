using UnityEngine;
using System.Collections;

public class StrategyController : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField]
	private float rotSpeed = 3;
	private Quaternion initRot;
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
	void Start ()
	{
		initRot = cam.transform.rotation;
	}
	
	// Update is called once per frame
	void Update ()
	{
		float xAxis = Input.GetAxis("Horizontal");
		camRoot.transform.Translate(new Vector3(xAxis * speed, 0, 0), Space.Self);
		
		float yAxis = Input.GetAxis("Vertical");
		camRoot.transform.Translate(new Vector3(0, 0, yAxis * speed), Space.Self);

		float alt = Input.GetAxis("Altitude");
		camRoot.transform.Translate(new Vector3(0, alt * speed, 0), Space.Self);

		cam.transform.position = camRoot.transform.position;

		/*// Zooming
		float zoom = Input.GetAxis("Altitude");
		cam.transform.Translate(new Vector3(0, 0, zoom * zoomSpeed), Space.Self);
		*/

		float rotAxis = Input.GetAxis("Rotate");
		cam.transform.Rotate(new Vector3(0, rotAxis * rotSpeed, 0), Space.World);
		Transform camPam = cam.transform.parent;
		cam.transform.SetParent(null);
		camRoot.transform.Rotate(new Vector3(0, rotAxis * rotSpeed, 0), Space.World);

		if (Input.GetButtonDown("ResetRotate"))
		{
			cam.transform.rotation = initRot;
			camRoot.transform.rotation = Quaternion.identity;
		}
	}
}
