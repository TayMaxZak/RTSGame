using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System.Diagnostics;
using System;
using System.Threading;

public class Manager_Pathfinding : MonoBehaviour
{
	[SerializeField]
	private bool printInfo = false;

	[SerializeField]
	private LayerMask obstacleMask;
	[SerializeField]
	private int scaleFactor = 1;
	[SerializeField]
	private int gridSizeX = 40;
	[SerializeField]
	private int gridSizeY = 40;
	[SerializeField]
	private float nodeCheckRadius = 1; // How much space each node covers
	[SerializeField]
	private PathNode[,] grid;

	private float radiusMod = 1.1284f;

	private PathSolver solver;
	private PathRequestHandler requestHandler;

	public int MaxSize
	{
		get
		{
			return gridSizeX * gridSizeY;
		}
	}

	void Awake()
	{
		CreateGrid();

		solver = new PathSolver();
		requestHandler = new PathRequestHandler();
		solver.Init(this);
		requestHandler.Init(solver);
	}

	void Update()
	{
		// Update our solver/handler
		requestHandler.Tick();
	}

	void CreateGrid()
	{
		grid = new PathNode[gridSizeX, gridSizeY];
		float nodeWidth = scaleFactor * 2;
		Vector3 worldBottomLeft = transform.position - new Vector3(gridSizeX * nodeWidth / 2, 0, gridSizeY * nodeWidth / 2);
		List<PathNode> obstacleNodes = new List<PathNode>();

		for (int x = 0; x < gridSizeX; x++)
		{
			for (int y = 0; y < gridSizeY; y++)
			{
				Vector3 worldPoint = worldBottomLeft + new Vector3(x * nodeWidth + nodeWidth / 2, 0, y * nodeWidth + nodeWidth / 2);
				
				bool obs = Physics.CheckSphere(worldPoint, nodeCheckRadius * scaleFactor * radiusMod, obstacleMask);
				PathNode.Passability pass = obs ? PathNode.Passability.Obstacle : PathNode.Passability.Clear;

				grid[x, y] = new PathNode(pass, worldPoint, x, y);

				if (obs)
					obstacleNodes.Add(grid[x, y]);
			}
		}

		for (int i = 0; i < obstacleNodes.Count; i++)
		{
			List<PathNode> neighbors = FindNeighbors(obstacleNodes[i]);
			for (int j = 0; j < neighbors.Count; j++)
			{
				if (neighbors[j].clear == PathNode.Passability.Clear)
					neighbors[j].clear = PathNode.Passability.NearObstacle;
			}
		}
	}

	// Finds a node that is located closest to the given world position
	public PathNode NodeFromWorldPoint(Vector3 worldPosition)
	{
		// Move to 0,0, shift over by 50% to have origin at 0,0, then divide position by node diamater
		float posX = ((worldPosition.x - transform.position.x) + gridSizeX * scaleFactor) / (scaleFactor * 2);
		float posY = ((worldPosition.z - transform.position.z) + gridSizeY * scaleFactor) / (scaleFactor * 2);

		posX = Mathf.Clamp(posX, 0, gridSizeX - 1);
		posY = Mathf.Clamp(posY, 0, gridSizeY - 1);

		int x = Mathf.FloorToInt(posX * 2) / 2;
		int y = Mathf.FloorToInt(posY * 2) / 2;

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

		Gizmos.DrawWireCube(transform.position, new Vector3(gridSizeX * scaleFactor * 2, 1, gridSizeY * scaleFactor * 2));
		
		if (grid != null)
		{
			foreach (PathNode n in grid)
			{
				bool clear = n.clear == PathNode.Passability.Clear;
				if (n.clear == PathNode.Passability.Clear)
				{
					Gizmos.color = Color.white;
					Gizmos.DrawWireCube(n.position, Vector3.one * scaleFactor * 2);
					//Gizmos.DrawSphere(n.position, nodeCheckRadius * (nodeCheckRadius * scaleFactor * radiusMod + scaleFactor * outerRadiusMod));
				}
				else if (n.clear == PathNode.Passability.NearObstacle)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawWireCube(n.position, Vector3.one * scaleFactor * 2);
					//Gizmos.DrawSphere(n.position, nodeCheckRadius * (nodeCheckRadius * scaleFactor * radiusMod + scaleFactor * outerRadiusMod));
				}
				else if (n.clear == PathNode.Passability.Obstacle)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawSphere(n.position, nodeCheckRadius * nodeCheckRadius * scaleFactor * radiusMod);

				}
			} // foreach node
		} // has nodes
	}
}

public class PathSolver
{
	private Manager_Pathfinding grid;

	public void Init(Manager_Pathfinding pathfinding)
	{
		grid = pathfinding;
	}

	public void FindPath(PathRequest request, Action<PathResult> callback)
	{
		//Stopwatch sw = new Stopwatch();
		//sw.Start();

		Vector3[] waypoints = new Vector3[0];
		bool pathFound = false;

		PathNode startNode = grid.NodeFromWorldPoint(request.pathStart);
		PathNode endNode = grid.NodeFromWorldPoint(request.pathEnd);

		if (endNode.clear != PathNode.Passability.Obstacle)
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
					pathFound = true;
					break; // Exit out of while
				}

				// Check neighbors
				foreach (PathNode neighbor in grid.FindNeighbors(currentNode))
				{
					// Obstacle or already in our set
					if (neighbor.clear == PathNode.Passability.Obstacle || closedSet.Contains(neighbor))
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

		if (pathFound)
		{
			waypoints = RetracePath(startNode, endNode);
			pathFound = waypoints.Length > 0; // Waypoints could be empty
		}
		callback(new PathResult(waypoints, pathFound, request.callback));

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

		Vector3[] waypoints = new Vector3[0];
		if (path.Count > 0) // There must be waypoints there in order to simplify them
		{
			waypoints = SimplifyPath(path);
			Array.Reverse(waypoints);
		}
		return waypoints;
	}

	// Remove superfluous nodes
	Vector3[] SimplifyPath(List<PathNode> path)
	{
		List<Vector3> waypoints = new List<Vector3>();
		List<int> setIndices = new List<int>();
		Vector2 directionOld = Vector2.zero;

		waypoints.Add(path[0].position); // Add end point
		setIndices.Add(0);

		for (int i = 0; i < path.Count - 1; i++)
		{
			// Calculated normalized direction
			Vector2 directionNew = new Vector2(path[i].gridX - path[i + 1].gridX, path[i].gridY - path[i + 1].gridY);
			if (directionNew != directionOld) // Is this a turn in the path?
			{
				// "i" is for strict pathing, "i + 1" is for loose pathing 
				int indexToSet = path[i + 1].clear == PathNode.Passability.NearObstacle ? i : i + 1;

				if (!setIndices.Contains(indexToSet)) // Did we already add this point?
				{
					setIndices.Add(indexToSet); // Don't add the same point several times
					waypoints.Add(path[indexToSet].position); // Add relevant waypoint
				}
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
	Queue<PathResult> results = new Queue<PathResult>();

	static PathRequestHandler instance;
	private PathSolver solver;

	public void Init(PathSolver pathfinding)
	{
		solver = pathfinding;
		instance = this;
	}

	public void Tick()
	{
		// Items are in queue
		if (results.Count > 0)
		{
			int itemsInQueue = results.Count;
			lock (results)
			{
				for (int i = 0; i < itemsInQueue; i++)
				{
					PathResult result = results.Dequeue();
					result.callback(result.path, result.success);
				}
			}
		}
	}

	public static void RequestPath(PathRequest request)
	{
		ThreadStart threadStart = delegate
		{
			instance.solver.FindPath(request, instance.FinishedProcessingPath);
		};
		threadStart.Invoke();
	}

	public void FinishedProcessingPath(PathResult result)
	{
		// Prevent strange behaviour from multiple threads enqueueing simultaneously
		lock (results)
		{
			results.Enqueue(result);
		}
	}
}

public struct PathResult
{
	public Vector3[] path;
	public bool success;
	public Action<Vector3[], bool> callback;

	public PathResult(Vector3[] path, bool success, Action<Vector3[], bool> callback)
	{
		this.path = path;
		this.success = success;
		this.callback = callback;
	}
}

public struct PathRequest
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
