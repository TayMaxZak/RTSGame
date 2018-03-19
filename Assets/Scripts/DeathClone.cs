using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathClone : MonoBehaviour
{
	[SerializeField]
	private GameObject startEffect;
	[SerializeField]
	private float randomSpinFactor;
	private Rigidbody rigid;

	void Start ()
	{
		Instantiate(startEffect, transform.position, Quaternion.identity);
		rigid = GetComponent<Rigidbody>();
		rigid.angularVelocity = new Vector3(RandomValue(), RandomValue(), RandomValue()) * randomSpinFactor;
	}

	float RandomValue()
	{
		return Random.value * 2 - 1;
		//return 1;
	}
}
