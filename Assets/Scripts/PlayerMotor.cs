using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMotor : MonoBehaviour
{
	[SerializeField]
	private Camera playerCam;
	[SerializeField]
	private float cameraRotationLimit = 87f;

	private Vector3 velocity = Vector3.zero;
	private Vector3 rotation = Vector3.zero;
	private float cameraRotation = 0f;
	private float curCameraRotation = 0f;
	private Vector3 thrusterForce = Vector3.zero;

	[SerializeField]
	private Vector3 gravityForce = new Vector3(0f, -1000, 0f);

	private Rigidbody rigid;

	// Use this for initialization
	void Start ()
	{
		rigid = GetComponent<Rigidbody>();
	}
	
	// Update is called once per frame
	void FixedUpdate ()
	{
		PerformMovement();
		PerformRotation();
	}

	public void Move(Vector3 _velocity)
	{
		velocity = _velocity;
	}

	public void Rotate(Vector3 _rotation)
	{
		rotation = _rotation;
	}

	public void RotateCamera(float _cameraRotation)
	{
		cameraRotation = _cameraRotation;
	}

	void PerformMovement()
	{
		if (velocity != Vector3.zero)
		{
			rigid.MovePosition(rigid.position + velocity * Time.fixedDeltaTime);
		}

		if (thrusterForce != Vector3.zero)
		{
			rigid.AddForce(thrusterForce * Time.fixedDeltaTime, ForceMode.Acceleration);
		}
		else
		{
			rigid.AddForce(gravityForce * Time.fixedDeltaTime, ForceMode.Acceleration);
		}
	}
	
	void PerformRotation()
	{
		rigid.MoveRotation(rigid.rotation * Quaternion.Euler(rotation));

		if (playerCam != null)
		{
			curCameraRotation -= cameraRotation;
			curCameraRotation = Mathf.Clamp(curCameraRotation, -cameraRotationLimit, cameraRotationLimit);

			playerCam.transform.localEulerAngles = new Vector3(curCameraRotation, 0, 0);
		}
	}

	public void ApplyThruster(Vector3 _thrusterForce)
	{
		thrusterForce = _thrusterForce;
	}

	public void Jump(float jumpVelocity)
	{
		rigid.velocity = new Vector3(rigid.velocity.x, rigid.velocity.y + jumpVelocity, rigid.velocity.z);
	}

	public void WallJump(Vector3 jumpVector, float vertical, float horizMult)
	{
		rigid.velocity = new Vector3(jumpVector.x * horizMult, jumpVector.y + vertical, jumpVector.z * horizMult);
	}

	public void Dash(Vector3 dashVelocity, float duration)
	{
		rigid.velocity = new Vector3(rigid.velocity.x + dashVelocity.x, rigid.velocity.y + dashVelocity.y, rigid.velocity.z + dashVelocity.z);
		StartCoroutine(EndDash(dashVelocity, duration));
	}

	IEnumerator EndDash(Vector3 dashVelocity, float duration)
	{
		yield return new WaitForSeconds(duration);

		float xSign = Mathf.Sign(rigid.velocity.x);
		float ySign = Mathf.Sign(rigid.velocity.y);
		float zSign = Mathf.Sign(rigid.velocity.z);

		//Remove the added velocity. However, we dont want to get launched the opposite direction
		//If the difference results in a value less than zero, cap it at zero
		//Then we return back the original sign of the velocity (assuming its not zero)
		float x = xSign * Mathf.Min(0, Mathf.Abs(rigid.velocity.x - dashVelocity.x));
		float y = ySign * Mathf.Min(0, Mathf.Abs(rigid.velocity.y - dashVelocity.y));
		float z = zSign * Mathf.Min(0, Mathf.Abs(rigid.velocity.z - dashVelocity.z));

		rigid.velocity = new Vector3(x, y, z);
	}
}
