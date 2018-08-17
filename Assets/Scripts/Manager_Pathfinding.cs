using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manager_Pathfinding : MonoBehaviour
{
	[SerializeField]
	private LayerMask obstacleMask;
	[SerializeField]
	private Vector2 gridWorldSize; // Area in world coordinates that the grid is going to cover
	[SerializeField]
	private float nodeRadius; // How much space each node covers
	[SerializeField]
	private PathfindingNode[,] grid;

	[SerializeField]
	private PathfindingSolver solver;

	private int gridSizeX;
	private int gridSizeY;

	void Start()
	{
		gridSizeX = Mathf.RoundToInt(gridWorldSize.x / (nodeRadius * 2));
		gridSizeY = Mathf.RoundToInt(gridWorldSize.y / (nodeRadius * 2));
		CreateGrid();

		solver.Init(this);
	}

	void Update()
	{
		// Update our solver
		solver.Tick();
	}

	void CreateGrid()
	{
		grid = new PathfindingNode[gridSizeX, gridSizeY];
		Vector3 worldBottomLeft = transform.position - new Vector3(gridWorldSize.x, 0, gridWorldSize.y) * 0.5f;

		for (int x = 0; x < gridSizeX; x++)
		{
			for (int y = 0; y < gridSizeY; y++)
			{
				Vector3 worldPoint = worldBottomLeft + new Vector3(nodeRadius + x * nodeRadius * 2, 0, nodeRadius + y * nodeRadius * 2);
				bool clear = !(Physics.CheckSphere(worldPoint, nodeRadius, obstacleMask));
				grid[x, y] = new PathfindingNode(clear, worldPoint, x, y);
			}
		}
	}

	// Finds a node that is located closest to the given world position
	public PathfindingNode NodeFromWorldPoint(Vector3 worldPosition)
	{
		float posX = ((worldPosition.x - transform.position.x) + gridWorldSize.x * 0.5f) / (nodeRadius * 2);
		float posY = ((worldPosition.z - transform.position.z) + gridWorldSize.y * 0.5f) / (nodeRadius * 2);

		posX = Mathf.Clamp(posX, 0, (gridWorldSize.x / (nodeRadius * 2)) - 1);
		posY = Mathf.Clamp(posY, 0, (gridWorldSize.y / (nodeRadius * 2)) - 1);

		int x = Mathf.FloorToInt(posX);
		int y = Mathf.FloorToInt(posY);

		return grid[x, y];
	}

	public List<PathfindingNode> FindNeighbors(PathfindingNode node)
	{
		List<PathfindingNode> neighbors = new List<PathfindingNode>();

		// Where is this node in our grid
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				if (x == 0 & y == 0)
					continue;

				int checkX = node.gridX + x;
				int checkY = node.gridY + y;

				if (checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY)
					continue;

				neighbors.Add(grid[checkX, checkY]);
			}
		} // for

		return neighbors;
	}

	// Debug visuals
	public List<PathfindingNode> path;
	void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));

		if (grid != null)
		{
			foreach (PathfindingNode n in grid)
			{
				Gizmos.color = n.clear ? Color.white : Color.red;
				if (path != null)
				{
					if (path.Contains(n))
						Gizmos.color = Color.black;
				}
				Gizmos.DrawCube(n.position, new Vector3(1, 0.2f, 1) * nodeRadius * 1.8f);
			}
		}
	}
}

[System.Serializable]
public class PathfindingSolver
{
	[SerializeField]
	private Transform seeker;
	[SerializeField]
	private Transform target;

	private Manager_Pathfinding manager;

	public void Init(Manager_Pathfinding pathfinding)
	{
		manager = pathfinding;
	}

	// Update is called once per frame
	public void Tick()
	{
		FindPath(seeker.position, target.position);
	}

	void FindPath(Vector3 startPos, Vector3 endPos)
	{
		PathfindingNode startNode = manager.NodeFromWorldPoint(startPos);
		PathfindingNode endNode = manager.NodeFromWorldPoint(endPos);

		List<PathfindingNode> openSet = new List<PathfindingNode>();
		HashSet<PathfindingNode> closedSet = new HashSet<PathfindingNode>();
		openSet.Add(startNode);

		while (openSet.Count > 0)
		{
			PathfindingNode currentNode = openSet[0];
			for (int i = 1; i < openSet.Count; i++)
			{
				// Next node is either the most optimal one, or, if it is tied with another node, the closest to the end
				if (openSet[i].FCost < currentNode.FCost || 
					openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost)
				{
					currentNode = openSet[i];
				}
			} // for each node in openset

			// Move from open set to closed set
			openSet.Remove(currentNode);
			closedSet.Add(currentNode);

			// Found path
			if (currentNode == endNode)
			{
				RetracePath(startNode, endNode);
				return;
			}

			// Check neighbors
			foreach (PathfindingNode neighbor in manager.FindNeighbors(currentNode))
			{
				// Obstacle or already in our set
				if (!neighbor.clear || closedSet.Contains(neighbor))
				{
					continue;
				}

				// Lower cost found or an unitialized node
				int newMoveCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);
				if (newMoveCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
				{
					// Initialize movement costs
					neighbor.gCost = newMoveCostToNeighbor;
					neighbor.hCost = GetDistance(neighbor, endNode);
					neighbor.parent = currentNode;

					if (!openSet.Contains(neighbor))
					{
						openSet.Add(neighbor);
					}
				}
			}
		} // while nodes in openset
	} // FindPath()

	void RetracePath(PathfindingNode startNode, PathfindingNode endNode)
	{
		List<PathfindingNode> path = new List<PathfindingNode>();
		PathfindingNode currentNode = endNode;

		while (currentNode != startNode)
		{
			path.Add(currentNode);
			currentNode = currentNode.parent;
		}
		path.Reverse();

		manager.path = path;
	}

	int GetDistance(PathfindingNode nodeA, PathfindingNode nodeB)
	{
		int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
		int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

		// 14 is cost of diagonal, 10 is cost of perpindicular
		if (distX > distY)
			return 14 * distY + 10 * (distX - distY);
		else
			return 14 * distX + 10 * (distY - distX);
	}
}
