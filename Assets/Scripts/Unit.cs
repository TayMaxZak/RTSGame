using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : Entity
{

	private Entity target;
	[SerializeField]
	private float moveSpeed = 5;
	[SerializeField]
	private float moveAccel = 1; // Time in seconds to reach full speed
	[SerializeField]
	private float rotSpeed = 90;
	[SerializeField]
	private float rotAccel = 1; // Time in seconds to reach full speed
	//[SerializeField]
	//private float bankAngle = 30;
	//[SerializeField]
	//private Transform banker;

	[Header("Pathing")]
	private bool isPathing;
	private Vector3 goal;
	[SerializeField]
	private float reachGoalThresh = 1;

	private Quaternion lookRotation;
	private Vector3 direction;
	[SerializeField]
	private float allowMoveThresh = 0.1f;

	[Header("Combat")]
	[SerializeField]
	private GameObject tempExplosion;
	private int tempCounter = 0;
	private bool isAttacking;
	

	// Use this for initialization
	new void Start()
	{
		base.Start();
		goal = transform.position;
	}
	
	// Update is called once per frame
	void Update ()
	{
		Vector3 dif = goal - transform.position;
		
		if (dif.magnitude <= reachGoalThresh)
			isPathing = false;
		else
			isPathing = true;

		if (isPathing)
		{
			direction = dif.normalized;
			lookRotation = Quaternion.LookRotation(direction);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, Time.deltaTime * rotSpeed);

			//float bank = bankAngle * -Vector3.Dot(transform.right, direction);
			//banker.localRotation = Quaternion.AngleAxis(bankAngle, Vector3.forward);

			//Debug.Log("Old rot - new rot = " + (oldRot - newRot));

			float dot = Mathf.Max((Vector3.Dot(direction, transform.forward) - (1 - allowMoveThresh)) / (allowMoveThresh), 0);
			//Debug.Log("dot is " + dot);
			transform.position += transform.forward * moveSpeed * dot * Time.deltaTime;
		}
		else
		{

		}

		if (target)
			isAttacking = true;
		else
			isAttacking = false;

		if (isAttacking)
		{
			tempCounter++;
			if (tempCounter == 50)
			{
				Instantiate(tempExplosion, target.transform.position, Quaternion.identity);
				tempCounter = 0;
			}
		}
	}

	public void OrderMove(Vector3 newGoal)
	{
		goal = newGoal;
	}

	public void OrderAttack(Entity newTarg)
	{
		target = newTarg;
		Debug.Log("I, " + DisplayName + ", am going after " + target.DisplayName);
	}
}
