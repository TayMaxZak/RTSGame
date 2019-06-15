using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FluentBehaviourTree;

public class AI_GrandAdmiral : Commander
{
	private List<Entity_Objective> objectives;
	private List<Entity_Objective> uncapturedZones;
	private IBehaviourTreeNode tree;
	private AI_GrandAdmiral admiral;
	int numRequiredToFail = 2;
	int numRequiredToSucceed = 2;
	Entity_Objective targetZone;	
	
	new void Awake ()
	{
		base.Awake();
		uncapturedZones = new List<Entity_Objective>();
		UpdateUncapturedZones();

	}
	
	new void Update () {
		base.Update();
		this.tree.Tick(new TimeData(Time.deltaTime));
	}
	
	// BEHAVIOURS ////////////////////////////////////////////////////////////////////////////////

	private void BuildMainBehavior()
	{
				
		var builder = new BehaviourTreeBuilder();
		tree = builder
			.Parallel("main-parallel", numRequiredToFail, numRequiredToSucceed)
				.Sequence("CaptureZones")
					.Do("If there are uncaptured zones", t =>
					{
						if (uncapturedZones.Count > 0)
							return BehaviourTreeStatus.Success;
						return BehaviourTreeStatus.Failure;
					})
					.Do("Get zone", t => GetClosetZone())
					.Do("CaptureZone", t => CaptureZone())
				.End()
			.End()
		.Build();
	}

	BehaviourTreeStatus GetClosetZone()
	{
		var fs_pos = base.GetFlagship().GetComponent<Transform>().position;
		var shortestDis = -1f;

		foreach (var zone in uncapturedZones)
		{
			var z_pos = zone.GetComponent<Transform>().position;
			var dis = Vector3.Distance(fs_pos, z_pos);
			if (dis < 0 || dis < shortestDis)
			{
				shortestDis = dis;
				targetZone = zone;
			}	
		}

		return BehaviourTreeStatus.Success;
	}
	
	BehaviourTreeStatus CaptureZone()
	{
		
		
		return BehaviourTreeStatus.Success;
	}
	
	// FUNCTIONS ////////////////////////////////////////////////////////////////////////////////
	
	// Get zones that haven't been captured by this team
	// Update uncapturedZones by clearing it and adding new ones
	private void UpdateUncapturedZones()
	{
		var zones = GameObject.FindObjectsOfType<Entity_Objective>();
		uncapturedZones.Clear();
		var team = base.GetController().team;
		foreach (var zone in zones)
		{
			if (zone.GetCapturedProgress() == team)
				uncapturedZones.Add(zone);
		}

	}

}
