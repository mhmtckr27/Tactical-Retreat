using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Collections;

public class Map : NetworkBehaviour
{
	public static float blockHeight = 1.73f;
	public static float blockWidth = 2f;
	public static float blockOffsetZ = 0.75f * blockWidth;

	[SerializeField][SyncVar] public int mapWidth;
	[SerializeField] private BlockPrefabsWithCreationProbability[] terrainPrefabs;
	[SerializeField] private GameObject undiscoveredBlock;

	[SyncVar] public int totalTerrainHexagonCount;
	[SyncVar] public int currentlyInstantiatedTerrainHexagonCount;

	private int[] neighbourOffset_N = { 0, 1, -1 };
	private int[] neighbourOffset_NE = { 1, 0, -1 };
	private int[] neighbourOffset_SE = { 1, -1, 0 };
	private int[] neighbourOffset_S = { 0, -1, 1 };
	private int[] neighbourOffset_SW = { -1, 0, 1 };
	private int[] neighbourOffset_NW = { -1, 1, 0 };

	[System.Serializable]
	public struct BlockPrefabsWithCreationProbability
	{
		public GameObject blockPrefab;
		public float creationProbability;
	}

	public Dictionary<string, TerrainHexagon> mapDictionary = new Dictionary<string, TerrainHexagon>();
	public List<GameObject> unexploredsBlockObjects = new List<GameObject>();

	private static Map instance;
	public static Map Instance
	{
		get
		{
			return instance;
		}
	}

	private UnitBase unitToMove;
	public UnitBase UnitToMove
	{
		get => unitToMove;
		set
		{
			unitToMove = value;
		}
	}

	public TerrainHexagon selectedHexagon;

	public State currentState = State.None;

	public void OnInstanceChange(Map oldInstance, Map newInstance)
	{
		instance = newInstance;
	}

	public System.Action<float> OnMapGenerationProgressChange;

	public void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(gameObject);
		}
	}
	
	[Server]
	public void SetMapWidth(int newValue)
	{
		mapWidth = newValue;
		totalTerrainHexagonCount = 1 + 3 * mapWidth * (mapWidth - 1);
	}

	[Server]
	public TerrainHexagon CreateTerrainHexagon(string neighbourKey, int[] neighbourOffsets, TerrainHexagon currentHex, Vector3 blockInfo, GameObject hexBlock)
	{
		if (!mapDictionary.ContainsKey(neighbourKey))
		{
			mapDictionary.Add(neighbourKey, Instantiate(hexBlock, currentHex.transform.position + blockInfo, Quaternion.identity/*, transform*/).GetComponent<TerrainHexagon>());
			mapDictionary[neighbourKey].SetCoordinates(currentHex.Coordinates[0] + neighbourOffsets[0], currentHex.Coordinates[1] + neighbourOffsets[1], currentHex.Coordinates[2] + neighbourOffsets[2]);
			NetworkServer.Spawn(mapDictionary[neighbourKey].gameObject);
			currentlyInstantiatedTerrainHexagonCount++;
			return mapDictionary[neighbourKey];
		}
		else
		{
			return currentHex;
		}
	}

	[Server]
	public void GenerateMap()
	{
		string coordinate_key = 0 + "_" + 0 + "_" + 0;
		mapDictionary.Add(coordinate_key, Instantiate(GetRandomBlock(), Vector3.zero, Quaternion.identity).GetComponent<TerrainHexagon>());
		mapDictionary["0_0_0"].SetCoordinates(0, 0, 0);
		TerrainHexagon initialHex = mapDictionary["0_0_0"];
		TerrainHexagon currentHex = initialHex;
		NetworkServer.Spawn(initialHex.gameObject);
		currentlyInstantiatedTerrainHexagonCount++;

		//CreateUndiscoveredBlocks(currentHex);

		int k = 0;
		while (k < mapWidth - 1)
		{
			//north neighbour
			//if (!mapDictionary.ContainsKey(currentHex.Neighbour_N))
			//{
				/*
				mapDictionary.Add(currentHex.Neighbour_N, Instantiate(GetRandomBlock(), currentHex.transform.position + new Vector3(blockHeight, 0, 0), Quaternion.identity).GetComponent<TerrainHexagon>());
				mapDictionary[currentHex.Neighbour_N].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_N[0], currentHex.Coordinates[1] + neighbourOffset_N[1], currentHex.Coordinates[2] + neighbourOffset_N[2]);
				NetworkServer.Spawn(mapDictionary[currentHex.Neighbour_N].gameObject);
				currentHex = mapDictionary[currentHex.Neighbour_N];
				*/
				currentHex = CreateTerrainHexagon(currentHex.Neighbour_N, neighbourOffset_N, currentHex, new Vector3(blockHeight, 0, 0), GetRandomBlock());
				//CreateUndiscoveredBlocks(currentHex);
			//}
			for (int j = 0; j < k + 1; j++)
			{
				//south-east neighbour		
				/*if (!mapDictionary.ContainsKey(currentHex.Neighbour_SE))
				{
					mapDictionary.Add(currentHex.Neighbour_SE, Instantiate(GetRandomBlock(), currentHex.transform.position + new Vector3(-blockHeight / 2, 0, -blockOffsetZ), Quaternion.identity).GetComponent<TerrainHexagon>());
					mapDictionary[currentHex.Neighbour_SE].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_SE[0], currentHex.Coordinates[1] + neighbourOffset_SE[1], currentHex.Coordinates[2] + neighbourOffset_SE[2]);
					NetworkServer.Spawn(mapDictionary[currentHex.Neighbour_SE].gameObject);
					currentHex = mapDictionary[currentHex.Neighbour_SE];

					//CreateUndiscoveredBlocks(currentHex);
				}*/
				currentHex = CreateTerrainHexagon(currentHex.Neighbour_SE, neighbourOffset_SE, currentHex, new Vector3(-blockHeight / 2, 0, -blockOffsetZ), GetRandomBlock());
			}
			for (int j = 0; j < k + 1; j++)
			{
				//south neighbour
				/*if (!mapDictionary.ContainsKey(currentHex.Neighbour_S))
				{
					mapDictionary.Add(currentHex.Neighbour_S, Instantiate(GetRandomBlock(), currentHex.transform.position + new Vector3(-blockHeight, 0, 0), Quaternion.identity).GetComponent<TerrainHexagon>());
					mapDictionary[currentHex.Neighbour_S].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_S[0], currentHex.Coordinates[1] + neighbourOffset_S[1], currentHex.Coordinates[2] + neighbourOffset_S[2]);
					NetworkServer.Spawn(mapDictionary[currentHex.Neighbour_S].gameObject);
					currentHex = mapDictionary[currentHex.Neighbour_S];

					//CreateUndiscoveredBlocks(currentHex);
				}
				*/
				currentHex = CreateTerrainHexagon(currentHex.Neighbour_S, neighbourOffset_S, currentHex, new Vector3(-blockHeight, 0, 0), GetRandomBlock());
			}
			for (int j = 0; j < k + 1; j++)
			{
				//south-west neighbour
				/*if (!mapDictionary.ContainsKey(currentHex.Neighbour_SW))
				{
					mapDictionary.Add(currentHex.Neighbour_SW, Instantiate(GetRandomBlock(), currentHex.transform.position + new Vector3(-blockHeight / 2, 0, blockOffsetZ), Quaternion.identity).GetComponent<TerrainHexagon>());
					mapDictionary[currentHex.Neighbour_SW].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_SW[0], currentHex.Coordinates[1] + neighbourOffset_SW[1], currentHex.Coordinates[2] + neighbourOffset_SW[2]);
					NetworkServer.Spawn(mapDictionary[currentHex.Neighbour_SW].gameObject);
					currentHex = mapDictionary[currentHex.Neighbour_SW];

					//CreateUndiscoveredBlocks(currentHex);
				}
				*/
				currentHex = CreateTerrainHexagon(currentHex.Neighbour_SW, neighbourOffset_SW, currentHex, new Vector3(-blockHeight / 2, 0, blockOffsetZ), GetRandomBlock());
			}
			for (int j = 0; j < k + 1; j++)
			{
				//north-west neighbour
				/*if (!mapDictionary.ContainsKey(currentHex.Neighbour_NW))
				{
					mapDictionary.Add(currentHex.Neighbour_NW, Instantiate(GetRandomBlock(), currentHex.transform.position + new Vector3(blockHeight / 2, 0, blockOffsetZ), Quaternion.identity).GetComponent<TerrainHexagon>());
					mapDictionary[currentHex.Neighbour_NW].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_NW[0], currentHex.Coordinates[1] + neighbourOffset_NW[1], currentHex.Coordinates[2] + neighbourOffset_NW[2]);
					NetworkServer.Spawn(mapDictionary[currentHex.Neighbour_NW].gameObject);
					currentHex = mapDictionary[currentHex.Neighbour_NW];

					//CreateUndiscoveredBlocks(currentHex);
				}
				*/
				currentHex = CreateTerrainHexagon(currentHex.Neighbour_NW, neighbourOffset_NW, currentHex, new Vector3(blockHeight / 2, 0, blockOffsetZ), GetRandomBlock());
			}
			for (int j = 0; j < k + 1; j++)
			{
				//north neighbour
				/*if (!mapDictionary.ContainsKey(currentHex.Neighbour_N))
				{
					mapDictionary.Add(currentHex.Neighbour_N, Instantiate(GetRandomBlock(), currentHex.transform.position + new Vector3(blockHeight, 0, 0), Quaternion.identity).GetComponent<TerrainHexagon>());
					mapDictionary[currentHex.Neighbour_N].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_N[0], currentHex.Coordinates[1] + neighbourOffset_N[1], currentHex.Coordinates[2] + neighbourOffset_N[2]);
					NetworkServer.Spawn(mapDictionary[currentHex.Neighbour_N].gameObject);
					currentHex = mapDictionary[currentHex.Neighbour_N];

					//CreateUndiscoveredBlocks(currentHex);
				}
				*/
				currentHex = CreateTerrainHexagon(currentHex.Neighbour_N, neighbourOffset_N, currentHex, new Vector3(blockHeight, 0, 0), GetRandomBlock());
			}
			for (int j = 0; j < k + 1; j++)
			{
				//north-east neighbour
				/*if (!mapDictionary.ContainsKey(currentHex.Neighbour_NE))
				{
					mapDictionary.Add(currentHex.Neighbour_NE, Instantiate(GetRandomBlock(), currentHex.transform.position + new Vector3(blockHeight / 2, 0, -blockOffsetZ), Quaternion.identity).GetComponent<TerrainHexagon>());
					mapDictionary[currentHex.Neighbour_NE].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_NE[0], currentHex.Coordinates[1] + neighbourOffset_NE[1], currentHex.Coordinates[2] + neighbourOffset_NE[2]);
					NetworkServer.Spawn(mapDictionary[currentHex.Neighbour_NE].gameObject);
					currentHex = mapDictionary[currentHex.Neighbour_NE];

					//CreateUndiscoveredBlocks(currentHex);
				}
				else
				{
					currentHex = mapDictionary[currentHex.Neighbour_NE];
				}
				*/
				if (!mapDictionary.ContainsKey(currentHex.Neighbour_NE))
				{
					currentHex = CreateTerrainHexagon(currentHex.Neighbour_NE, neighbourOffset_NE, currentHex, new Vector3(blockHeight / 2, 0, -blockOffsetZ), GetRandomBlock());
				}
				else
				{
					currentHex = mapDictionary[currentHex.Neighbour_NE];
				}
			}
			k++;
		}
	}

	[Server]
	public void CreateUndiscoveredBlocks()
	{
		foreach(KeyValuePair<string, TerrainHexagon> kvp in mapDictionary)
		{
			GameObject tempUndiscovered = Instantiate(undiscoveredBlock, kvp.Value.transform.position + new Vector3(0, 0.045f, 0), Quaternion.identity);
			unexploredsBlockObjects.Add(tempUndiscovered);
			NetworkServer.Spawn(tempUndiscovered);
			kvp.Value.unexploredBlock = tempUndiscovered;
			currentlyInstantiatedTerrainHexagonCount++;
		}
	}

	//replace water blocks that are single (don't have any water neighbour) with random ground block.
	[Server]
	public void DilateMap()
	{
		List<string> dilateWaterBlocks = new List<string>();
		List<string> dilateGroundBlocks = new List<string>();
		foreach (KeyValuePair<string, TerrainHexagon> keyValuePair in mapDictionary)
		{
			bool mustDilate = true;
			if (keyValuePair.Value.terrainType == TerrainType.Water)
			{
				for(int j = 0; j < 6; j++)
				{
					if (mapDictionary.ContainsKey(keyValuePair.Value.NeighbourKeys[j]))
					{
						if(mapDictionary[keyValuePair.Value.NeighbourKeys[j]].terrainType == TerrainType.Water)
						{
							mustDilate = false;
						}
					}
					if (!mustDilate)
					{
						break;
					}
				}
				if (!mustDilate)
				{
					continue;
				}
				else
				{
					dilateWaterBlocks.Add(keyValuePair.Key);
				}
			}
			else
			{
				for (int j = 0; j < 6; j++)
				{
					if (mapDictionary.ContainsKey(keyValuePair.Value.NeighbourKeys[j]))
					{
						if (mapDictionary[keyValuePair.Value.NeighbourKeys[j]].terrainType != TerrainType.Water)
						{
							mustDilate = false;
						}
					}
					if (!mustDilate)
					{
						break;
					}
				}
				if (!mustDilate)
				{
					continue;
				}
				else
				{
					dilateGroundBlocks.Add(keyValuePair.Key);
				}
			}
		}

		for (int i = 0; i < dilateWaterBlocks.Count; i++)
		{
			TerrainHexagon temp = mapDictionary[dilateWaterBlocks[i]];
			mapDictionary.Remove(dilateWaterBlocks[i]);
			mapDictionary.Add(dilateWaterBlocks[i], Instantiate(GetRandomBlockExceptWater(), temp.transform.position, Quaternion.identity/*, transform*/).GetComponent<TerrainHexagon>());
			NetworkServer.Spawn(mapDictionary[dilateWaterBlocks[i]].gameObject);
			mapDictionary[dilateWaterBlocks[i]].SetCoordinates(temp.Coordinates[0], temp.Coordinates[1], temp.Coordinates[2]);
			//Destroy(temp.gameObject);
			NetworkServer.Destroy(temp.gameObject);
		}

		for (int i = 0; i < dilateGroundBlocks.Count; i++)
		{
			TerrainHexagon temp = mapDictionary[dilateGroundBlocks[i]];
			mapDictionary.Remove(dilateGroundBlocks[i]);
			mapDictionary.Add(dilateGroundBlocks[i], Instantiate(GetWaterBlock(), temp.transform.position, Quaternion.identity/*, transform*/).GetComponent<TerrainHexagon>());
			NetworkServer.Spawn(mapDictionary[dilateGroundBlocks[i]].gameObject);
			mapDictionary[dilateGroundBlocks[i]].SetCoordinates(temp.Coordinates[0], temp.Coordinates[1], temp.Coordinates[2]);
			//Destroy(temp.gameObject);
			NetworkServer.Destroy(temp.gameObject);
		}
	}

	[Server]
	private GameObject GetRandomBlock()
	{
		float result = UnityEngine.Random.Range((float)0, 1);
		int index = 0;
		for(int i = 0; i < terrainPrefabs.Length; i++)
		{
			if(result < terrainPrefabs[i].creationProbability)
			{
				index = i;
				break;
			}
		}
		return terrainPrefabs[index].blockPrefab;
	}

	[Server]
	private GameObject GetRandomBlockExceptWater()
	{
		GameObject temp;
		do
		{
			temp = GetRandomBlock();
		} while (temp.GetComponent<TerrainHexagon>().terrainType == TerrainType.Water);
		return temp;
	}

	[Server]
	private GameObject GetWaterBlock()
	{
		return terrainPrefabs[1].blockPrefab;
	}

	//this function gets all neighbours within certain distance, no matter if blocked or water block etc.
	[Server]
	public List<TerrainHexagon> GetDistantHexagons(TerrainHexagon block, int distance) 
	{ 
		List<TerrainHexagon> neighbours = new List<TerrainHexagon>(); 
		for(int x = -distance; x < distance + 1; x++)
		{
			int val = Mathf.Max(-distance, -x - distance);
			int val2 = Mathf.Min(distance, -x + distance);
			for(int y = val; y < val2 + 1; y++)
			{
				int z = -x - y;
				string key = (x + block.Coordinates[0]) + "_" + (y + block.Coordinates[1]) + "_" + (z + block.Coordinates[2]);
				if (mapDictionary.ContainsKey(key))
				{
					neighbours.Add(mapDictionary[key]);
				}
			}
		}
		return neighbours; 
	}

	//this function gets only reachable neighbours within certain distance, considering if the unit calling this function can move to that hexagon.
	[Server]
	public List<TerrainHexagon> GetReachableHexagons(TerrainHexagon start, int moveRange, int attackRange, List<TerrainType> blockedToMoveHexagonTypes, List<TerrainType> blockedToAttackHexagonTypes, List<TerrainHexagon> occupiedNeighbours)
	{
		List<TerrainHexagon> reachableHexagons = new List<TerrainHexagon>();
		List<List<TerrainHexagon>> visitedHexagons = new List<List<TerrainHexagon>>();
		reachableHexagons.Add(start);
		visitedHexagons.Add(new List<TerrainHexagon>());
		visitedHexagons[0].Add(start);

		int lastIndex = moveRange > attackRange ? moveRange : attackRange;
		for (int i = 1; i < lastIndex + 1; i++)
		{
			visitedHexagons.Add(new List<TerrainHexagon>());
			foreach(TerrainHexagon hex in visitedHexagons[i-1])
			{
				for(int direction = 0; direction < 6; direction++)
				{
					TerrainHexagon neighbour =  GetNeighbourInDirection(hex, direction);

					if (neighbour != null)
					{
						if ((neighbour.GetOccupierUnit() == null))
						{
							if (!blockedToMoveHexagonTypes.Contains(neighbour.terrainType))
							{
								if (!reachableHexagons.Contains(neighbour))
								{
									if (i < (moveRange + 1))
									{
										reachableHexagons.Add(neighbour);
									}
									visitedHexagons[i].Add(neighbour);
								}
							}
						}
					}
				}
			}
		}

		//populating occupied neighbour array.
		visitedHexagons = new List<List<TerrainHexagon>>();
		visitedHexagons.Add(new List<TerrainHexagon>());
		visitedHexagons[0].Add(start);

		for (int i = 1; i < attackRange + 1; i++)
		{
			visitedHexagons.Add(new List<TerrainHexagon>());
			foreach (TerrainHexagon hex in visitedHexagons[i - 1])
			{
				for (int direction = 0; direction < 6; direction++)
				{
					TerrainHexagon neighbour = GetNeighbourInDirection(hex, direction);

					if (neighbour != null)
					{
						if ((neighbour.GetOccupierUnit() != null) && (neighbour != start) && (occupiedNeighbours != null) && !occupiedNeighbours.Contains(neighbour) && (i < (attackRange + 1)) && !blockedToAttackHexagonTypes.Contains(neighbour.terrainType))
						{
							//Debug.LogWarning("occupied neighbour:" + neighbour.Key);
							occupiedNeighbours.Add(neighbour);
						}
						visitedHexagons[i].Add(neighbour);
					}
				}
			}
		}

		return reachableHexagons;
	}

	[Server]
	public int GetDistanceBetweenTwoBlocks(TerrainHexagon hex1, TerrainHexagon hex2)
	{
		return Mathf.Max(Mathf.Abs(hex1.Coordinates[0] - hex2.Coordinates[0]), Mathf.Abs(hex1.Coordinates[1] - hex2.Coordinates[1]), Mathf.Abs(hex1.Coordinates[2] - hex2.Coordinates[2]));
	}

	[Server]
	public TerrainHexagon GetNeighbourInDirection(TerrainHexagon hexagon, int direction)
	{
		if (mapDictionary.ContainsKey(hexagon.NeighbourKeys[direction]))
		{
			return mapDictionary[hexagon.NeighbourKeys[direction]];
		}
		return null;
	}

	[Server]
	public List<TerrainHexagon> AStar(TerrainHexagon from, TerrainHexagon to, List<TerrainType> blockedToMoveHexagonTypes, List<TerrainType> blockedToAttackHexagonTypes, List<TerrainHexagon> occupiedNeighbours)
	{
		PriorityQueue<TerrainHexagon> frontier = new PriorityQueue<TerrainHexagon>(true);
		frontier.Enqueue(0, from);
		Dictionary<TerrainHexagon, TerrainHexagon> cameFrom = new Dictionary<TerrainHexagon, TerrainHexagon>();
		Dictionary<TerrainHexagon, int> currentCost = new Dictionary<TerrainHexagon, int>();
		cameFrom[from] = null;
		currentCost[from] = 0;
		while(frontier.Count != 0)
		{
			TerrainHexagon current_hexagon = frontier.Dequeue();
			if(current_hexagon == to)
			{
				break;
			}
			foreach(TerrainHexagon next_hexagon in GetReachableHexagons(current_hexagon, 1, 1, blockedToMoveHexagonTypes, blockedToAttackHexagonTypes, occupiedNeighbours))
			{
				//I can change constant value of 1 to a variable in the future depending on the movement cost (they may be affected by terrain conditions)
				int new_cost = currentCost[current_hexagon] + 1;
				if(!currentCost.ContainsKey(next_hexagon) || (new_cost < currentCost[next_hexagon]))
				{
					currentCost[next_hexagon] = new_cost;
					int priority = new_cost + GetDistanceBetweenTwoBlocks(to, next_hexagon);
					frontier.Enqueue(priority, next_hexagon);
					cameFrom[next_hexagon] = current_hexagon;
				}
			}
		}
		List<TerrainHexagon> path = new List<TerrainHexagon>();
		TerrainHexagon current_hex = to;

		while(current_hex != from)
		{
			path.Insert(0, current_hex);
			if (!cameFrom.ContainsKey(current_hex))
			{
				//path does not exist
				return null;
			}
			current_hex = cameFrom[current_hex];
		}
		path.Insert(0, from);
		return path;
	}
}

public enum State
{
	UnitAction,
	BuildingAction,
	None
}