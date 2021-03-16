using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map : MonoBehaviour
{
	private const float block_height = 1.73f;
	private const float block_width = 2f;
	private const float block_offset_z = 0.75f * block_width;
	[SerializeField] private int map_width;
	[SerializeField] private GameObject peasant;
	[SerializeField] private BlockPrefabsWithCreationProbability[] blocks;

	public int[] neighbour_offset_n = { 0, 1, -1 };
	public int[] neighbour_offset_ne = { 1, 0, -1 };
	public int[] neighbour_offset_se = { 1, -1, 0 };
	public int[] neighbour_offset_s = { 0, -1, 1 };
	public int[] neighbour_offset_sw = { -1, 0, 1 };
	public int[] neighbour_offset_nw = { -1, 1, 0 };

	[System.Serializable]
	public struct BlockPrefabsWithCreationProbability
	{
		public GameObject block_prefab;
		public float creation_probability;
	}

	private Dictionary<string, HexagonBlockBase> map_dictionary = new Dictionary<string, HexagonBlockBase>();

	private static Map instance;
	public static Map Instance
	{
		get
		{
			return instance;
		}
	}

	public State current_state = State.None;
	public UnitBase unit_to_move;

	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
		}
		else if (instance != this)
		{
			Destroy(instance);
		}
		GenerateMap();
		DilateMap();

		Instantiate(peasant, map_dictionary["0_0_0"].transform.position, Quaternion.identity).GetComponent<UnitBase>().block_under = map_dictionary["0_0_0"];
	}


	private void GenerateMap()
	{
		string coordinate_key = 0 + "_" + 0 + "_" + 0;
		map_dictionary.Add(coordinate_key, Instantiate(GetRandomBlock(), Vector3.zero, Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
		map_dictionary["0_0_0"].SetCoordinates(0, 0, 0);
		HexagonBlockBase initial_hex = map_dictionary["0_0_0"];
		HexagonBlockBase current_hex = initial_hex;

		int k = 0;
		while (k < map_width - 1)
		{
			//north neighbour
			if (!map_dictionary.ContainsKey(current_hex.neighbour_n))
			{
				map_dictionary.Add(current_hex.neighbour_n, Instantiate(GetRandomBlock(), current_hex.transform.position + new Vector3(block_height, 0, 0), Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
				map_dictionary[current_hex.neighbour_n].SetCoordinates(current_hex.coordinates[0] + neighbour_offset_n[0], current_hex.coordinates[1] + neighbour_offset_n[1], current_hex.coordinates[2] + neighbour_offset_n[2]);
				current_hex = map_dictionary[current_hex.neighbour_n];
			}
			for (int j = 0; j < k + 1; j++)
			{
				//south-east neighbour		
				if (!map_dictionary.ContainsKey(current_hex.neighbour_se))
				{
					map_dictionary.Add(current_hex.neighbour_se, Instantiate(GetRandomBlock(), current_hex.transform.position + new Vector3(-block_height / 2, 0, -block_offset_z), Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
					map_dictionary[current_hex.neighbour_se].SetCoordinates(current_hex.coordinates[0] + neighbour_offset_se[0], current_hex.coordinates[1] + neighbour_offset_se[1], current_hex.coordinates[2] + neighbour_offset_se[2]);
					current_hex = map_dictionary[current_hex.neighbour_se];
				}
			}
			for (int j = 0; j < k + 1; j++)
			{
				//south neighbour
				if (!map_dictionary.ContainsKey(current_hex.neighbour_s))
				{
					map_dictionary.Add(current_hex.neighbour_s, Instantiate(GetRandomBlock(), current_hex.transform.position + new Vector3(-block_height, 0, 0), Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
					map_dictionary[current_hex.neighbour_s].SetCoordinates(current_hex.coordinates[0] + neighbour_offset_s[0], current_hex.coordinates[1] + neighbour_offset_s[1], current_hex.coordinates[2] + neighbour_offset_s[2]);
					current_hex = map_dictionary[current_hex.neighbour_s];
				}

			}
			for (int j = 0; j < k + 1; j++)
			{
				//south-west neighbour
				if (!map_dictionary.ContainsKey(current_hex.neighbour_sw))
				{
					map_dictionary.Add(current_hex.neighbour_sw, Instantiate(GetRandomBlock(), current_hex.transform.position + new Vector3(-block_height / 2, 0, block_offset_z), Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
					map_dictionary[current_hex.neighbour_sw].SetCoordinates(current_hex.coordinates[0] + neighbour_offset_sw[0], current_hex.coordinates[1] + neighbour_offset_sw[1], current_hex.coordinates[2] + neighbour_offset_sw[2]);
					current_hex = map_dictionary[current_hex.neighbour_sw];
				}
			}
			for (int j = 0; j < k + 1; j++)
			{
				//north-west neighbour
				if (!map_dictionary.ContainsKey(current_hex.neighbour_nw))
				{
					map_dictionary.Add(current_hex.neighbour_nw, Instantiate(GetRandomBlock(), current_hex.transform.position + new Vector3(block_height / 2, 0, block_offset_z), Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
					map_dictionary[current_hex.neighbour_nw].SetCoordinates(current_hex.coordinates[0] + neighbour_offset_nw[0], current_hex.coordinates[1] + neighbour_offset_nw[1], current_hex.coordinates[2] + neighbour_offset_nw[2]);
					current_hex = map_dictionary[current_hex.neighbour_nw];
				}
			}
			for (int j = 0; j < k + 1; j++)
			{
				//north neighbour
				if (!map_dictionary.ContainsKey(current_hex.neighbour_n))
				{
					map_dictionary.Add(current_hex.neighbour_n, Instantiate(GetRandomBlock(), current_hex.transform.position + new Vector3(block_height, 0, 0), Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
					map_dictionary[current_hex.neighbour_n].SetCoordinates(current_hex.coordinates[0] + neighbour_offset_n[0], current_hex.coordinates[1] + neighbour_offset_n[1], current_hex.coordinates[2] + neighbour_offset_n[2]);
					current_hex = map_dictionary[current_hex.neighbour_n];
				}
			}
			for (int j = 0; j < k + 1; j++)
			{
				//north-east neighbour
				if (!map_dictionary.ContainsKey(current_hex.neighbour_ne))
				{
					map_dictionary.Add(current_hex.neighbour_ne, Instantiate(GetRandomBlock(), current_hex.transform.position + new Vector3(block_height / 2, 0, -block_offset_z), Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
					map_dictionary[current_hex.neighbour_ne].SetCoordinates(current_hex.coordinates[0] + neighbour_offset_ne[0], current_hex.coordinates[1] + neighbour_offset_ne[1], current_hex.coordinates[2] + neighbour_offset_ne[2]);
					current_hex = map_dictionary[current_hex.neighbour_ne];
				}
				else
				{
					current_hex = map_dictionary[current_hex.neighbour_ne];
				}
			}
			k++;
		}
	}

	//replace water blocks that are single (don't have any water neighbour) with random ground block.
	private void DilateMap()
	{
		List<string> dilate_water_blocks = new List<string>();
		List<string> dilate_ground_blocks = new List<string>();
		foreach (KeyValuePair<string, HexagonBlockBase> keyValuePair in map_dictionary)
		{
			bool must_dilate = true;
			if (keyValuePair.Value.block_type == BlockType.Water)
			{
				for(int j = 0; j < 6; j++)
				{
					if (map_dictionary.ContainsKey(keyValuePair.Value.neighbour_keys[j]))
					{
						if(map_dictionary[keyValuePair.Value.neighbour_keys[j]].block_type == BlockType.Water)
						{
							must_dilate = false;
						}
					}
					if (!must_dilate)
					{
						break;
					}
				}
				if (!must_dilate)
				{
					continue;
				}
				else
				{
					dilate_water_blocks.Add(keyValuePair.Key);
				}
			}
			else
			{
				for (int j = 0; j < 6; j++)
				{
					if (map_dictionary.ContainsKey(keyValuePair.Value.neighbour_keys[j]))
					{
						if (map_dictionary[keyValuePair.Value.neighbour_keys[j]].block_type != BlockType.Water)
						{
							must_dilate = false;
						}
					}
					if (!must_dilate)
					{
						break;
					}
				}
				if (!must_dilate)
				{
					continue;
				}
				else
				{
					dilate_ground_blocks.Add(keyValuePair.Key);
				}
			}
		}

		for (int i = 0; i < dilate_water_blocks.Count; i++)
		{
			HexagonBlockBase temp = map_dictionary[dilate_water_blocks[i]];
			map_dictionary.Remove(dilate_water_blocks[i]);
			map_dictionary.Add(dilate_water_blocks[i], Instantiate(GetRandomBlockExceptWater(), temp.transform.position, Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
			map_dictionary[dilate_water_blocks[i]].SetCoordinates(temp.coordinates[0], temp.coordinates[1], temp.coordinates[2]);
			Destroy(temp.gameObject);
		}

		for (int i = 0; i < dilate_ground_blocks.Count; i++)
		{
			HexagonBlockBase temp = map_dictionary[dilate_ground_blocks[i]];
			map_dictionary.Remove(dilate_ground_blocks[i]);
			map_dictionary.Add(dilate_ground_blocks[i], Instantiate(GetWaterBlock(), temp.transform.position, Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
			map_dictionary[dilate_ground_blocks[i]].SetCoordinates(temp.coordinates[0], temp.coordinates[1], temp.coordinates[2]);
			Destroy(temp.gameObject);
		}
	}

	private GameObject GetRandomBlock()
	{
		float result = Random.Range((float)0, 1);
		int index = 0;
		for(int i = 0; i < blocks.Length; i++)
		{
			if(result < blocks[i].creation_probability)
			{
				index = i;
				break;
			}
		}
		return blocks[index].block_prefab;
	}

	private GameObject GetRandomBlockExceptWater()
	{
		GameObject temp_block;
		do
		{
			temp_block = GetRandomBlock();
		} while (temp_block.GetComponent<HexagonBlockBase>().block_type == BlockType.Water);
		return temp_block;
	}

	private GameObject GetWaterBlock()
	{
		return blocks[1].block_prefab;
	}

	//this function gets all neighbours within certain distance, no matter if blocked or water block etc.
	public List<HexagonBlockBase> GetDistantHexagons(HexagonBlockBase block, int distance) 
	{ 
		List<HexagonBlockBase> neighbours = new List<HexagonBlockBase>(); 
		for(int x = -distance; x < distance + 1; x++)
		{
			int val = Mathf.Max(-distance, -x - distance);
			int val2 = Mathf.Min(distance, -x + distance);
			for(int y = val; y < val2 + 1; y++)
			{
				int z = -x - y;
				string key = (x + block.coordinates[0]) + "_" + (y + block.coordinates[1]) + "_" + (z + block.coordinates[2]);
				neighbours.Add(map_dictionary[key]);
			}
		}
		return neighbours; 
	}

	//this function gets only reachable neighbours within certain distance, considering if the unit calling this function can move to that hexagon.
	public List<HexagonBlockBase> GetReachableHexagons(HexagonBlockBase start_hexagon, int distance)
	{
		List<HexagonBlockBase> reachable_hexagons = new List<HexagonBlockBase>();
		reachable_hexagons.Add(start_hexagon);
		List<List<HexagonBlockBase>> visited_hexagons = new List<List<HexagonBlockBase>>();
		visited_hexagons.Add(new List<HexagonBlockBase>());
		visited_hexagons[0].Add(start_hexagon);
		
		for(int i = 1; i < distance + 1; i++)
		{
			visited_hexagons.Add(new List<HexagonBlockBase>());
			foreach(HexagonBlockBase hex in visited_hexagons[i-1])
			{
				for(int direction = 0; direction < 6; direction++)
				{
					HexagonBlockBase neighbour = GetNeighbourInDirection(hex, direction);
					//gonna replace second condition in future with a parameter to make it generic.
					if (!reachable_hexagons.Contains(neighbour) && neighbour.block_type != BlockType.Water)
					{
						reachable_hexagons.Add(neighbour);
						visited_hexagons[i].Add(neighbour);
					}
				}
			}
		}
		return reachable_hexagons;
	}

	public int GetDistanceBetweenTwoBlocks(HexagonBlockBase hex1, HexagonBlockBase hex2)
	{
		return Mathf.Max(Mathf.Abs(hex1.coordinates[0] - hex2.coordinates[0]), Mathf.Abs(hex1.coordinates[1] - hex2.coordinates[1]), Mathf.Abs(hex1.coordinates[2] - hex2.coordinates[2]));
	}

	public HexagonBlockBase GetNeighbourInDirection(HexagonBlockBase hexagon, int direction)
	{
		return map_dictionary[hexagon.neighbour_keys[direction]];
	}
}

public enum State
{
	MoveUnitMode,
	None
}