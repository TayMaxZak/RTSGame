using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util_Mover : MonoBehaviour
{
	private Vector3 velocity;

	public void SetVelocity(Vector3 vel)
	{
		velocity = vel;
	}

	void Update()
	{
		transform.Translate(velocity * Time.deltaTime, Space.World);
	}
}
