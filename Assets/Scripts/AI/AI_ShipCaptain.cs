using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluentBehaviourTree;

public class AI_ShipCaptain : MonoBehaviour {

	private IBehaviourTreeNode tree;
	int numRequiredToFail = 2;
	int numRequiredToSucceed = 2;
	
	// Use this for initialization
	void Start () {
		
		var builder = new BehaviourTreeBuilder();
		tree = builder
			.Parallel("main-parallel", numRequiredToFail, numRequiredToSucceed)
				.Do("task1", t => Task1())
				.Do("task2", t => Task2())
				.Do("task3", t =>
				{
					print("task3 running");
					return BehaviourTreeStatus.Success;
				})
			.End()
			.Build();
	}

	private void Update()
	{
		this.tree.Tick(new TimeData(Time.deltaTime));
	}

	BehaviourTreeStatus Task1()
	{
		print("Task1 running");
		return BehaviourTreeStatus.Success;
	}

	BehaviourTreeStatus Task2()
	{
		print("Task2 running");
		return BehaviourTreeStatus.Success;
	}
	
}
