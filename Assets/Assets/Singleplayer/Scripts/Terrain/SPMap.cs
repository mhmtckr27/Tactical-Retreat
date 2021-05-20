using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPMap : MonoBehaviour
{

	private const float blockHeight = 1.73f;
	private const float blockWidth = 2f;
	private const float blockOffsetZ = 0.75f * blockWidth;

	[SerializeField] public int mapWidth;
	[SerializeField] private BlockPrefabsWithCreationProbability[] terrainPrefabs;
	[SerializeField] private GameObject undiscoveredBlock;

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

	public Dictionary<string, SPTerrainHexagon> mapDictionary = new Dictionary<string, SPTerrainHexagon>();

	private static SPMap instance;
	public static SPMap Instance
	{
		get
		{
			return instance;
		}
	}

	private SPUnitBase unitToMove;
	public SPUnitBase UnitToMove
	{
		get => unitToMove;
		set
		{
			unitToMove = value;
		}
	}

	public SPTerrainHexagon selectedHexagon;

	public State currentState = State.None;

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

	#region Singleplayer

	public void SPGenerateMap()
	{
		string coordinate_key = 0 + "_" + 0 + "_" + 0;
		mapDictionary.Add(coordinate_key, Instantiate(SPGetRandomBlock(), Vector3.zero, Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
		mapDictionary["0_0_0"].SetCoordinates(0, 0, 0);
		SPTerrainHexagon initialHex = mapDictionary["0_0_0"];
		SPTerrainHexagon currentHex = initialHex;

		//SPCreateUndiscoveredBlocks(currentHex);

		int k = 0;
		while (k < mapWidth - 1)
		{
			//north neighbour
			if (!mapDictionary.ContainsKey(currentHex.Neighbour_N))
			{
				mapDictionary.Add(currentHex.Neighbour_N, Instantiate(SPGetRandomBlock(), currentHex.transform.position + new Vector3(blockHeight, 0, 0), Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
				mapDictionary[currentHex.Neighbour_N].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_N[0], currentHex.Coordinates[1] + neighbourOffset_N[1], currentHex.Coordinates[2] + neighbourOffset_N[2]);
				currentHex = mapDictionary[currentHex.Neighbour_N];

				//SPCreateUndiscoveredBlocks(currentHex);
			}
			for (int j = 0; j < k + 1; j++)
			{
				//south-east neighbour		
				if (!mapDictionary.ContainsKey(currentHex.Neighbour_SE))
				{
					mapDictionary.Add(currentHex.Neighbour_SE, Instantiate(SPGetRandomBlock(), currentHex.transform.position + new Vector3(-blockHeight / 2, 0, -blockOffsetZ), Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
					mapDictionary[currentHex.Neighbour_SE].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_SE[0], currentHex.Coordinates[1] + neighbourOffset_SE[1], currentHex.Coordinates[2] + neighbourOffset_SE[2]);
					currentHex = mapDictionary[currentHex.Neighbour_SE];

					//SPCreateUndiscoveredBlocks(currentHex);
				}
			}
			for (int j = 0; j < k + 1; j++)
			{
				//south neighbour
				if (!mapDictionary.ContainsKey(currentHex.Neighbour_S))
				{
					mapDictionary.Add(currentHex.Neighbour_S, Instantiate(SPGetRandomBlock(), currentHex.transform.position + new Vector3(-blockHeight, 0, 0), Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
					mapDictionary[currentHex.Neighbour_S].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_S[0], currentHex.Coordinates[1] + neighbourOffset_S[1], currentHex.Coordinates[2] + neighbourOffset_S[2]);
					currentHex = mapDictionary[currentHex.Neighbour_S];

					//SPCreateUndiscoveredBlocks(currentHex);
				}

			}
			for (int j = 0; j < k + 1; j++)
			{
				//south-west neighbour
				if (!mapDictionary.ContainsKey(currentHex.Neighbour_SW))
				{
					mapDictionary.Add(currentHex.Neighbour_SW, Instantiate(SPGetRandomBlock(), currentHex.transform.position + new Vector3(-blockHeight / 2, 0, blockOffsetZ), Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
					mapDictionary[currentHex.Neighbour_SW].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_SW[0], currentHex.Coordinates[1] + neighbourOffset_SW[1], currentHex.Coordinates[2] + neighbourOffset_SW[2]);
					currentHex = mapDictionary[currentHex.Neighbour_SW];

					//SPCreateUndiscoveredBlocks(currentHex);
				}
			}
			for (int j = 0; j < k + 1; j++)
			{
				//north-west neighbour
				if (!mapDictionary.ContainsKey(currentHex.Neighbour_NW))
				{
					mapDictionary.Add(currentHex.Neighbour_NW, Instantiate(SPGetRandomBlock(), currentHex.transform.position + new Vector3(blockHeight / 2, 0, blockOffsetZ), Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
					mapDictionary[currentHex.Neighbour_NW].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_NW[0], currentHex.Coordinates[1] + neighbourOffset_NW[1], currentHex.Coordinates[2] + neighbourOffset_NW[2]);
					currentHex = mapDictionary[currentHex.Neighbour_NW];

					//SPCreateUndiscoveredBlocks(currentHex);
				}
			}
			for (int j = 0; j < k + 1; j++)
			{
				//north neighbour
				if (!mapDictionary.ContainsKey(currentHex.Neighbour_N))
				{
					mapDictionary.Add(currentHex.Neighbour_N, Instantiate(SPGetRandomBlock(), currentHex.transform.position + new Vector3(blockHeight, 0, 0), Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
					mapDictionary[currentHex.Neighbour_N].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_N[0], currentHex.Coordinates[1] + neighbourOffset_N[1], currentHex.Coordinates[2] + neighbourOffset_N[2]);
					currentHex = mapDictionary[currentHex.Neighbour_N];

					//SPCreateUndiscoveredBlocks(currentHex);
				}
			}
			for (int j = 0; j < k + 1; j++)
			{
				//north-east neighbour
				if (!mapDictionary.ContainsKey(currentHex.Neighbour_NE))
				{
					mapDictionary.Add(currentHex.Neighbour_NE, Instantiate(SPGetRandomBlock(), currentHex.transform.position + new Vector3(blockHeight / 2, 0, -blockOffsetZ), Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
					mapDictionary[currentHex.Neighbour_NE].SetCoordinates(currentHex.Coordinates[0] + neighbourOffset_NE[0], currentHex.Coordinates[1] + neighbourOffset_NE[1], currentHex.Coordinates[2] + neighbourOffset_NE[2]);
					currentHex = mapDictionary[currentHex.Neighbour_NE];

					//SPCreateUndiscoveredBlocks(currentHex);
				}
				else
				{
					currentHex = mapDictionary[currentHex.Neighbour_NE];
				}
			}
			k++;
		}
	}

	public void SPDilateMap()
	{
		List<string> dilateWaterBlocks = new List<string>();
		List<string> dilateGroundBlocks = new List<string>();
		foreach (KeyValuePair<string, SPTerrainHexagon> keyValuePair in mapDictionary)
		{
			bool mustDilate = true;
			if (keyValuePair.Value.terrainType == TerrainType.Water)
			{
				for (int j = 0; j < 6; j++)
				{
					if (mapDictionary.ContainsKey(keyValuePair.Value.NeighbourKeys[j]))
					{
						if (mapDictionary[keyValuePair.Value.NeighbourKeys[j]].terrainType == TerrainType.Water)
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
			SPTerrainHexagon temp = mapDictionary[dilateWaterBlocks[i]];
			mapDictionary.Remove(dilateWaterBlocks[i]);
			mapDictionary.Add(dilateWaterBlocks[i], Instantiate(SPGetRandomBlockExceptWater(), temp.transform.position, Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
			mapDictionary[dilateWaterBlocks[i]].SetCoordinates(temp.Coordinates[0], temp.Coordinates[1], temp.Coordinates[2]);
			Destroy(temp.gameObject);
		}

		for (int i = 0; i < dilateGroundBlocks.Count; i++)
		{
			SPTerrainHexagon temp = mapDictionary[dilateGroundBlocks[i]];
			mapDictionary.Remove(dilateGroundBlocks[i]);
			mapDictionary.Add(dilateGroundBlocks[i], Instantiate(SPGetWaterBlock(), temp.transform.position, Quaternion.identity/*, transform*/).GetComponent<SPTerrainHexagon>());
			mapDictionary[dilateGroundBlocks[i]].SetCoordinates(temp.Coordinates[0], temp.Coordinates[1], temp.Coordinates[2]);
			Destroy(temp.gameObject);
		}
	}

	public void SPCreateUndiscoveredBlocks()
	{
		foreach(KeyValuePair<string, SPTerrainHexagon> kvp in mapDictionary)
		{
			GameObject tempUndiscovered = Instantiate(undiscoveredBlock, kvp.Value.transform.position, Quaternion.identity);
			kvp.Value.unexploredBlock = tempUndiscovered;
		}
	}

	private GameObject SPGetRandomBlock()
	{
		float result = UnityEngine.Random.Range((float)0, 1);
		int index = 0;
		for (int i = 0; i < terrainPrefabs.Length; i++)
		{
			if (result < terrainPrefabs[i].creationProbability)
			{
				index = i;
				break;
			}
		}
		return terrainPrefabs[index].blockPrefab;
	}

	private GameObject SPGetRandomBlockExceptWater()
	{
		GameObject temp;
		do
		{
			temp = SPGetRandomBlock();
		} while (temp.GetComponent<SPTerrainHexagon>().terrainType == TerrainType.Water);
		return temp;
	}

	private GameObject SPGetWaterBlock()
	{
		return terrainPrefabs[1].blockPrefab;
	}
	#endregion

	public void Explore(string key)
	{
		mapDictionary[key].gameObject.SetActive(false);
	}

	public void ClearSelectedHexagon()
	{
		SPMap.Instance.selectedHexagon = null;
	}

	//this function gets all neighbours within certain distance, no matter if blocked or water block etc.
	public List<SPTerrainHexagon> GetDistantHexagons(SPTerrainHexagon block, int distance)
	{
		List<SPTerrainHexagon> neighbours = new List<SPTerrainHexagon>();
		for (int x = -distance; x < distance + 1; x++)
		{
			int val = Mathf.Max(-distance, -x - distance);
			int val2 = Mathf.Min(distance, -x + distance);
			for (int y = val; y < val2 + 1; y++)
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
	public List<SPTerrainHexagon> GetReachableHexagons(SPTerrainHexagon start, int moveRange, int attackRange, List<TerrainType> blockedHexagonTypes, List<SPTerrainHexagon> occupiedNeighbours)
	{
		List<SPTerrainHexagon> reachableHexagons = new List<SPTerrainHexagon>();
		List<List<SPTerrainHexagon>> visitedHexagons = new List<List<SPTerrainHexagon>>();
		reachableHexagons.Add(start);
		visitedHexagons.Add(new List<SPTerrainHexagon>());
		visitedHexagons[0].Add(start);

		int lastIndex = moveRange > attackRange ? moveRange : attackRange;
		for (int i = 1; i < lastIndex + 1; i++)
		{
			visitedHexagons.Add(new List<SPTerrainHexagon>());
			foreach (SPTerrainHexagon hex in visitedHexagons[i - 1])
			{
				for (int direction = 0; direction < 6; direction++)
				{
					SPTerrainHexagon neighbour = GetNeighbourInDirection(hex, direction);

					if (neighbour != null && !blockedHexagonTypes.Contains(neighbour.terrainType))
					{
						if ((neighbour.OccupierUnit == null))
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
						else if ((neighbour != start) && (occupiedNeighbours != null) && !occupiedNeighbours.Contains(neighbour) && (i < (attackRange + 1)))
						{
							occupiedNeighbours.Add(neighbour);
						}
					}
				}
			}
		}
		return reachableHexagons;
	}

	public int GetDistanceBetweenTwoBlocks(SPTerrainHexagon hex1, SPTerrainHexagon hex2)
	{
		return Mathf.Max(Mathf.Abs(hex1.Coordinates[0] - hex2.Coordinates[0]), Mathf.Abs(hex1.Coordinates[1] - hex2.Coordinates[1]), Mathf.Abs(hex1.Coordinates[2] - hex2.Coordinates[2]));
	}

	public SPTerrainHexagon GetNeighbourInDirection(SPTerrainHexagon hexagon, int direction)
	{
		if (mapDictionary.ContainsKey(hexagon.NeighbourKeys[direction]))
		{
			return mapDictionary[hexagon.NeighbourKeys[direction]];
		}
		return null;
	}

	public List<SPTerrainHexagon> AStar(SPTerrainHexagon from, SPTerrainHexagon to, List<TerrainType> blockedTerrains)
	{
		PriorityQueue<SPTerrainHexagon> frontier = new PriorityQueue<SPTerrainHexagon>(true);
		frontier.Enqueue(0, from);
		Dictionary<SPTerrainHexagon, SPTerrainHexagon> cameFrom = new Dictionary<SPTerrainHexagon, SPTerrainHexagon>();
		Dictionary<SPTerrainHexagon, int> currentCost = new Dictionary<SPTerrainHexagon, int>();
		cameFrom[from] = null;
		currentCost[from] = 0;
		while (frontier.Count != 0)
		{
			SPTerrainHexagon current_hexagon = frontier.Dequeue();
			if (current_hexagon == to)
			{
				break;
			}
			foreach (SPTerrainHexagon next_hexagon in GetReachableHexagons(current_hexagon, 1, 1, blockedTerrains, null))
			{
				//I can change constant value of 1 to a variable in the future depending on the movement cost (they may be affected by terrain conditions)
				int new_cost = currentCost[current_hexagon] + 1;
				if (!currentCost.ContainsKey(next_hexagon) || (new_cost < currentCost[next_hexagon]))
				{
					currentCost[next_hexagon] = new_cost;
					int priority = new_cost + GetDistanceBetweenTwoBlocks(to, next_hexagon);
					frontier.Enqueue(priority, next_hexagon);
					cameFrom[next_hexagon] = current_hexagon;
				}
			}
		}
		List<SPTerrainHexagon> path = new List<SPTerrainHexagon>();
		SPTerrainHexagon current_hex = to;

		while (current_hex != from)
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

