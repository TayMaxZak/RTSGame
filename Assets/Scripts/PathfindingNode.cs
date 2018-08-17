using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathfindingNode : IHeapItem<PathfindingNode>
{
	public bool clear;
	public Vector3 position;

	public int gCost;
	public int hCost;

	public int gridX;
	public int gridY;

	public PathfindingNode parent;

	int heapIndex;

	public PathfindingNode(bool isClear, Vector3 pos, int x, int y)
	{
		clear = isClear;
		position = pos;
		gridX = x;
		gridY = y;
	}

	public int FCost
	{
		get {
			return gCost + hCost;
		}
	}


	public int HeapIndex
	{
		get
		{
			return heapIndex;
		}
		set
		{
			heapIndex = value;
		}
	}

	public int CompareTo(PathfindingNode nodeToCompare)
	{
		int compare = FCost.CompareTo(nodeToCompare.FCost);
		if (compare == 0) // Distance to goal is tiebreaker
		{
			compare = hCost.CompareTo(nodeToCompare.hCost);
		}
		return -compare;
	}
}
