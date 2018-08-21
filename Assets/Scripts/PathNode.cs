using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathNode : IHeapItem<PathNode>
{
	public Passability clear;
	public Vector3 position;

	public int gCost;
	public int hCost;

	public int gridX;
	public int gridY;
	public int gridH;

	public PathNode parent;

	int heapIndex;

	public PathNode(Passability isClear, Vector3 pos, int x, int y, int h)
	{
		clear = isClear;
		position = pos;
		gridX = x;
		gridY = y;
		gridH = h;
	}

	public int FCost
	{
		get {
			return gCost + hCost;
		}
	}

	public enum Passability
	{
		Clear,
		NearObstacle,
		Obstacle
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

	public int CompareTo(PathNode nodeToCompare)
	{
		int compare = FCost.CompareTo(nodeToCompare.FCost);
		if (compare == 0) // Distance to goal is tiebreaker
		{
			compare = hCost.CompareTo(nodeToCompare.hCost);
		}
		return -compare;
	}
}
