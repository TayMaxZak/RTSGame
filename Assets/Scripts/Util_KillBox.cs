using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util_KillBox : MonoBehaviour
{
	[SerializeField]
	private float mass = 200;
	[SerializeField]
	private Vector3 size = new Vector3(-1, -1, -1);

	//[SerializeField]
	//private Vector3 velocity;

	void OnTriggerEnter(Collider other)
	{
		Unit unit = other.GetComponentInParent<Unit>();
		if (unit)
		{
			Vector3 newPos = unit.transform.position;
			if (size.x > 0)
			{
				newPos.x = transform.position.x + (unit.transform.position - transform.position).normalized.x * size.x;
			}
			if (size.y > 0)
			{
				newPos.y = transform.position.y + (unit.transform.position - transform.position).normalized.y * size.y;
			}
			if (size.z > 0)
			{
				newPos.z = transform.position.z + (unit.transform.position - transform.position).normalized.z * size.z;
			}
			unit.transform.position = newPos;

			// TODO: Causes Multiplayer_Manager to not find the unit
			//unit.Damage(mass, 0, DamageType.Wreck);
		}
	}
}
