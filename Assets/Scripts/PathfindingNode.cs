using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PathfindingNode
{
	public bool clear;
	public Vector3 position;

	public int gCost;
	public int hCost;

	public int gridX;
	public int gridY;

	public PathfindingNode parent;

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
}
