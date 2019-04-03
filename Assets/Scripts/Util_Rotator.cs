using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util_Rotator : MonoBehaviour
{
	[SerializeField]
	private Vector3 velocity;

	public void SetVelocity(Vector3 vel)
	{
		velocity = vel;
	}

	void Update()
	{
		transform.Rotate(velocity * Time.deltaTime, Space.World);
	}
}
