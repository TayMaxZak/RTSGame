using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System.Diagnostics;
using System;

public class Manager_Pathfinding : MonoBehaviour
{
	[SerializeField]
	private bool printInfo = false;

	[SerializeField]
	private LayerMask obstacleMask;
	[SerializeField]
	private Vector2 gridWorldSize; // Area in world coordinates that the grid is going to cover
	[SerializeField]
	private float nodeRadius; // How much space each node covers
	[SerializeField]
	private PathNode[,] grid;

	private PathSolver solver;
	private PathRequestHandler requestHandler;

	private int gridSizeX;
	private int gridSizeY;

	//private GameRules gameRules;

	public int MaxSize
	{
		get
		{
			return gridSizeX * gridSizeY;
		}
	}

	//void Awake()
	//{
		//gameRules = GameObject.FindGameObjectWithTag("GameManager").GetComponent<Manager_Game>().GameRules;
	//}

	void Awake()
	{
		gridSizeX = Mathf.RoundToInt(gridWorldSize.x / (nodeRadius * 2));
		gridSizeY = Mathf.RoundToInt(gridWorldSize.y / (nodeRadius * 2));
		CreateGrid();

		solver = new PathSolver();
		requestHandler = new PathRequestHandler();
		solver.Init(this, requestHandler);
		requestHandler.Init(solver);
	}

	void Update()
	{
		// Update our solver
	}

	void CreateGrid()
	{
		grid = new PathNode[gridSizeX, gridSizeY];
		Vector3 worldBottomLeft = transform.position - new Vector3(gridWorldSize.x, 0, gridWorldSize.y) * 0.5f;

		for (int x = 0; x < gridSizeX; x++)
		{
			for (int y = 0; y < gridSizeY; y++)
			{
				Vector3 worldPoint = worldBottomLeft + new Vector3(nodeRadius + x * nodeRadius * 2, 0, nodeRadius + y * nodeRadius * 2);
				bool clear = !(Physics.CheckSphere(worldPoint, nodeRadius, obstacleMask));
				grid[x, y] = new PathNode(clear, worldPoint, x, y);
			}
		}
	}

	// Finds a node that is located closest to the given world position
	public PathNode NodeFromWorldPoint(Vector3 worldPosition)
	{
		float posX = ((worldPosition.x - transform.position.x) + gridWorldSize.x * 0.5f) / (nodeRadius * 2);
		float posY = ((worldPosition.z - transform.position.z) + gridWorldSize.y * 0.5f) / (nodeRadius * 2);

		posX = Mathf.Clamp(posX, 0, (gridWorldSize.x / (nodeRadius * 2)) - 1);
		posY = Mathf.Clamp(posY, 0, (gridWorldSize.y / (nodeRadius * 2)) - 1);

		int x = Mathf.FloorToInt(posX);
		int y = Mathf.FloorToInt(posY);

		return grid[x, y];
	}

	public List<PathNode> FindNeighbors(PathNode node)
	{
		List<PathNode> neighbors = new List<PathNode>();

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
	void OnDrawGizmos()
	{
		if (!printInfo)
			return;

		Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 1, gridWorldSize.y));
		
		if (grid != null)
		{
			foreach (PathNode n in grid)
			{
				Gizmos.color = n.clear ? Color.white : Color.red;
				if (!n.clear)
				{
					Gizmos.DrawCube(n.position, new Vector3(1, 0.2f, 1) * nodeRadius * 1.8f);
				}
			} // foreach node
		} // has nodes
	}
}

public class PathSolver
{
	private PathRequestHandler requestHandler;
	private Manager_Pathfinding grid;

	public void Init(Manager_Pathfinding pathfinding, PathRequestHandler handler)
	{
		grid = pathfinding;
		requestHandler = handler;
	}

	public void StartFindPath(Vector3 startPos, Vector3 endPos)
	{
		grid.StartCoroutine(FindPath(startPos, endPos));
	}

	IEnumerator FindPath(Vector3 startPos, Vector3 endPos)
	{
		//Stopwatch sw = new Stopwatch();
		//sw.Start();

		Vector3[] waypoints = new Vector3[0];
		bool pathFound = false;

		PathNode startNode = grid.NodeFromWorldPoint(startPos);
		PathNode endNode = grid.NodeFromWorldPoint(endPos);

		if (endNode.clear)
		{
			Heap<PathNode> openSet = new Heap<PathNode>(grid.MaxSize);
			HashSet<PathNode> closedSet = new HashSet<PathNode>();
			openSet.Add(startNode);

			while (openSet.Count > 0)
			{
				PathNode currentNode = openSet.RemoveFirst();
				closedSet.Add(currentNode);

				// Found path
				if (currentNode == endNode)
				{
					//sw.Stop();
					//UnityEngine.Debug.Log(sw.ElapsedMilliseconds + " ms");
					pathFound = true;
					break; // Exit out of while
				}

				// Check neighbors
				foreach (PathNode neighbor in grid.FindNeighbors(currentNode))
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

						// Add to open set
						if (!openSet.Contains(neighbor))
						{
							openSet.Add(neighbor);
							// Updates automatically
						}
						else // Must manually update because costs changed
							openSet.UpdateItem(neighbor);
					}
				}
			} // while nodes in openset
		} // endpoints clear

		yield return null;

		if (pathFound)
		{
			waypoints = RetracePath(startNode, endNode);
		}
		requestHandler.FinishedProcessingPath(waypoints, true);

	} // FindPath()

	Vector3[] RetracePath(PathNode startNode, PathNode endNode)
	{
		List<PathNode> path = new List<PathNode>();
		PathNode currentNode = endNode;

		while (currentNode != startNode)
		{
			path.Add(currentNode);
			currentNode = currentNode.parent;
		}
		Vector3[] waypoints = SimplifyPath(path);
		Array.Reverse(waypoints);
		return waypoints;
	}

	// Remove superfluous nodes
	Vector3[] SimplifyPath(List<PathNode> path)
	{
		List<Vector3> waypoints = new List<Vector3>();
		Vector2 directionOld = Vector2.zero;

		for (int i = 1; i < path.Count; i++)
		{
			// Calculated normalized direction
			Vector2 directionNew = new Vector2(path[i - 1].gridX - path[i].gridX, path[i - 1].gridY - path[i].gridY);
			if (directionNew != directionOld) // Turn in path
			{
				waypoints.Add(path[i].position); // Add relevant waypoint
			}
			directionOld = directionNew;
		}
		return waypoints.ToArray();
	}

	int GetDistance(PathNode nodeA, PathNode nodeB)
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

public class PathRequestHandler
{
	private Queue<PathRequest> pathRequestQueue = new Queue<PathRequest>();
	private PathRequest currentPathRequest;

	static PathRequestHandler instance;
	private PathSolver solver;

	private bool isProcessingPath;

	public void Init(PathSolver pathfinding)
	{
		solver = pathfinding;
		instance = this;
	}

	public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
	{
		PathRequest newRequest = new PathRequest(pathStart, pathEnd, callback);
		instance.pathRequestQueue.Enqueue(newRequest);
		instance.TryProcessNext();
	}

	void TryProcessNext()
	{
		// If we are not processing a path, and if the queue isn't empty
		if (!isProcessingPath && pathRequestQueue.Count > 0)
		{
			// Get first item from queue and remove it
			currentPathRequest = pathRequestQueue.Dequeue();
			isProcessingPath = true;
			solver.StartFindPath(currentPathRequest.pathStart, currentPathRequest.pathEnd);
		}
	}

	public void FinishedProcessingPath(Vector3[] path, bool success)
	{
		currentPathRequest.callback(path, success);
		isProcessingPath = false;
		TryProcessNext();
	}

	struct PathRequest
	{

		public Vector3 pathStart;
		public Vector3 pathEnd;
		public Action<Vector3[], bool> callback;

		public PathRequest(Vector3 start, Vector3 end, Action<Vector3[], bool> call)
		{
			pathStart = start;
			pathEnd = end;
			callback = call;
		}
	}
}
