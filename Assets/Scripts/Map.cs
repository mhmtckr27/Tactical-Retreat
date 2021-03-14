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
	[SerializeField] private LayerMask hexagon_blocks_layer_mask;
	
	[System.Serializable]
	public struct BlockPrefabsWithCreationProbability
	{
		public GameObject block_prefab;
		public float creation_probability;
	}

	private List<GameObject> map;

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
		//bu fonksiyonu(PopulateNeighbourLists) direkt cagirinca bazi neighbourlar missing oluyor(Dilate fonksiyonunda olusturulan blocklar), sebebini cozemedigim icin boyle bir workaround yaptim simdilik.
		Invoke("PopulateNeighbourLists", .2f);
		Instantiate(peasant, map[41].transform.position, Quaternion.identity);
	}
	private void Start()
	{
	}
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
		}
	}
	private void GenerateMap()
	{
		map = new List<GameObject>();
		map.Add(Instantiate(GetRandomBlockExceptWater(), Vector3.zero, Quaternion.identity, transform));
		//First creations
		for (int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), new Vector3(i * block_height / 2, 0, i * block_offset_z), Quaternion.identity, transform));
		}
		Vector3 lower_left_vertex_position = map[map.Count - 1].transform.position;

		for (int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), new Vector3(i * block_height / 2, 0, i * -block_offset_z), Quaternion.identity, transform));
		}
		Vector3 lower_right_vertex_position = map[map.Count - 1].transform.position;

		for (int i = 1; i < map_width * 2 - 2; i++)
		{
			map.Add(Instantiate(GetRandomBlock(), new Vector3(i * block_height, 0, 0), Quaternion.identity, transform));
		}

		//Second creations
		for (int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), lower_left_vertex_position + new Vector3(i * block_height, 0, 0), Quaternion.identity, transform));
		}
		Vector3 upper_left_vertex_position = map[map.Count - 1].transform.position;

		for (int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), lower_right_vertex_position + new Vector3(i * block_height, 0, 0), Quaternion.identity, transform));
		}
		Vector3 upper_right_vertex_position = map[map.Count - 1].transform.position;

		//Third creations
		for (int i = 1; i < map_width - 1; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), upper_left_vertex_position + new Vector3(i * block_height / 2, 0, i * -block_offset_z), Quaternion.identity, transform));
		}

		for (int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), upper_right_vertex_position + new Vector3(i * block_height / 2, 0, i * block_offset_z), Quaternion.identity, transform));
		}

		//fill left part
		for (int j = 0; j < map_width - 2; j++)
		{
			for (int i = 1; i < map_width + j; i++)
			{
				map.Add(Instantiate(GetRandomBlock(), lower_left_vertex_position + (j + 1) * new Vector3(-block_height / 2, 0, -block_offset_z) + new Vector3(i * block_height, 0, 0), Quaternion.identity, transform));
			}
		}

		//fill right part
		for (int j = 0; j < map_width - 2; j++)
		{
			for (int i = 1; i < map_width + j; i++)
			{
				map.Add(Instantiate(GetRandomBlock(), lower_right_vertex_position + (j + 1) * new Vector3(-block_height / 2, 0, block_offset_z) + new Vector3(i * block_height, 0, 0), Quaternion.identity, transform));
			}
		}
	}

	//replace water blocks that are single (don't have any water neighbour) with random ground block.
	private void DilateMap()
	{
		for(int i = 0; i < map.Count; i++)
		{
			if(map[i].GetComponent<HexagonBlockBase>().block_type == BlockType.Water)
			{
				bool must_replace = true;
				List<HexagonBlockBase> neighbours = GetNeighbours(map[i]);
				for(int j = 0; j < neighbours.Count; j++)
				{
					if(neighbours[j].block_type == BlockType.Water)
					{
						must_replace = false;
					}
					if (!must_replace)
					{
						continue;
					}
				}
				if (!must_replace)
				{
					continue;
				}
				else
				{
					Vector3 pos = map[i].transform.position;
					Destroy(map[i]);
					map.RemoveAt(i);
					map.Insert(i, Instantiate(GetRandomBlockExceptWater(), pos, Quaternion.identity, transform));
					Debug.Log(i);
				}
			}
		}

		for (int i = 0; i < map.Count; i++)
		{
			if (map[i].GetComponent<HexagonBlockBase>().block_type != BlockType.Water)
			{
				bool must_replace = true;
				List<HexagonBlockBase> neighbours = GetNeighbours(map[i]);
				for(int j = 0; j < neighbours.Count; j++)
				{
					if(neighbours[j].block_type != BlockType.Water)
					{
						must_replace = false;
					}
					if (!must_replace)
					{
						continue;
					}
				}
				if (!must_replace)
				{
					continue;
				}
				else
				{
					Vector3 pos = map[i].transform.position;
					Destroy(map[i]);
					map.RemoveAt(i);
					map.Insert(i, Instantiate(blocks[1].block_prefab, map[i].transform.position, Quaternion.identity, transform));
					Debug.Log(i);
				}
			}
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

	public List<HexagonBlockBase> GetNeighbours(GameObject block)
	{
		List<HexagonBlockBase> neighbours = new List<HexagonBlockBase>();
		RaycastHit hit;
		//up neighbour
		if (Physics.Raycast(block.transform.position + new Vector3(block_height, 1.9f, 0), Vector3.down, out hit, 2, hexagon_blocks_layer_mask))
		{
			neighbours.Add(hit.collider.GetComponent<HexagonBlockBase>());
		}
		//down neighbour
		if (Physics.Raycast(block.transform.position + new Vector3(-block_height, 1.9f, 0), Vector3.down, out hit, 2, hexagon_blocks_layer_mask))
		{
			neighbours.Add(hit.collider.GetComponent<HexagonBlockBase>());
		}
		//upper forward neighbour
		if (Physics.Raycast(block.transform.position + new Vector3(block_height / 2, 1.9f, block_offset_z), Vector3.down, out hit, 2, hexagon_blocks_layer_mask))
		{
			neighbours.Add(hit.collider.GetComponent<HexagonBlockBase>());
		}
		//upper backward neighbour
		if (Physics.Raycast(block.transform.position + new Vector3(block_height / 2, 1.9f, -block_offset_z), Vector3.down, out hit, 2, hexagon_blocks_layer_mask))
		{
			neighbours.Add(hit.collider.GetComponent<HexagonBlockBase>());
		}
		//lower forward neighbour
		if (Physics.Raycast(block.transform.position + new Vector3(-block_height / 2, 1.9f, block_offset_z), Vector3.down, out hit, 2, hexagon_blocks_layer_mask))
		{
			neighbours.Add(hit.collider.GetComponent<HexagonBlockBase>());
		}
		//lower backward neighbour
		if (Physics.Raycast(block.transform.position + new Vector3(-block_height / 2, 1.9f, -block_offset_z), Vector3.down, out hit, 2, hexagon_blocks_layer_mask))
		{
			neighbours.Add(hit.collider.GetComponent<HexagonBlockBase>());
		}
		return neighbours;
	}

	private void PopulateNeighbourLists()
	{
		foreach(GameObject block in map)
		{
			block.GetComponent<HexagonBlockBase>().neighbours = GetNeighbours(block);
		}
	}
}
