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
			GameObject temp = map_dictionary[dilate_water_blocks[i]].gameObject;
			map_dictionary.Remove(dilate_water_blocks[i]);
			map_dictionary.Add(dilate_water_blocks[i], Instantiate(GetRandomBlockExceptWater(), temp.transform.position, Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
			Destroy(temp);
		}

		for (int i = 0; i < dilate_ground_blocks.Count; i++)
		{
			GameObject temp = map_dictionary[dilate_ground_blocks[i]].gameObject;
			map_dictionary.Remove(dilate_ground_blocks[i]);
			map_dictionary.Add(dilate_ground_blocks[i], Instantiate(GetWaterBlock(), temp.transform.position, Quaternion.identity, transform).GetComponent<HexagonBlockBase>());
			Destroy(temp);
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

	public List<HexagonBlockBase> GetNeighboursWithinDistance(HexagonBlockBase block, int distance) 
	{ 
		List<HexagonBlockBase> neighbours = new List<HexagonBlockBase>(); 
		for (int offset_x = -distance; offset_x <= distance; offset_x++) 
		{ 
			int coordinate_x = block.coordinates[0] + offset_x; 
			for (int offset_y = -distance; offset_y <= distance; offset_y++) 
			{ 
				int coordinate_y = block.coordinates[1] + offset_y; 
				for (int offset_z = -distance; offset_z <= distance; offset_z++) 
				{ 
					int coordinate_z = block.coordinates[2] + offset_z; 
					string key = coordinate_x + "_" + coordinate_y + "_" + coordinate_z; 
					if (map_dictionary.ContainsKey(key)) 
					{
						HexagonBlockBase hex = map_dictionary[key];
						//if (hex.block_type != BlockType.Water)
						{
							neighbours.Add(hex); 
						}
					} 
				} 
			}
		} 
		return neighbours; 
	}
}
