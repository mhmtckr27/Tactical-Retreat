using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomMapGenerator : MonoBehaviour
{
	[SerializeField] private int map_width;
	[SerializeField] private BlockPrefabsWithCreationProbability[] blocks;
	[SerializeField] private LayerMask water_layer;
	[SerializeField] private LayerMask ground_layer;
	
	[System.Serializable]
	public struct BlockPrefabsWithCreationProbability
	{
		public GameObject block_prefab;
		public float creation_probability;
	}

	private List<GameObject> map;
	private void Awake()
	{
		map = new List<GameObject>();
		map.Add(Instantiate(GetRandomBlockExceptWater(), Vector3.zero, Quaternion.identity, transform));
		//First creations
		for (int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), new Vector3(i * 1.73f/2, 0, i * 1.5f), Quaternion.identity, transform));
		}
		Vector3 lower_left_vertex_position = map[map.Count - 1].transform.position;

		for (int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), new Vector3(i * 1.73f / 2, 0, i * -1.5f), Quaternion.identity, transform));
		}
		Vector3 lower_right_vertex_position = map[map.Count - 1].transform.position;

		for (int i = 1; i < map_width * 2 - 2; i++)
		{
			map.Add(Instantiate(GetRandomBlock(), new Vector3(i * 1.73f, 0, 0), Quaternion.identity, transform));
		}

		//Second creations
		for(int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), lower_left_vertex_position + new Vector3(i * 1.73f, 0, 0), Quaternion.identity, transform));
		}
		Vector3 upper_left_vertex_position = map[map.Count - 1].transform.position;

		for (int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), lower_right_vertex_position + new Vector3(i * 1.73f, 0, 0), Quaternion.identity, transform));
		}
		Vector3 upper_right_vertex_position = map[map.Count - 1].transform.position;

		//Third creations
		for (int i = 1; i < map_width - 1; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), upper_left_vertex_position + new Vector3(i * 1.73f / 2, 0, i * -1.5f), Quaternion.identity, transform));
		}

		for (int i = 1; i < map_width; i++)
		{
			map.Add(Instantiate(GetRandomBlockExceptWater(), upper_right_vertex_position + new Vector3(i * 1.73f / 2, 0, i * 1.5f), Quaternion.identity, transform));
		}

		//fill left part
		for(int j = 0; j < map_width - 2; j++)
		{
			for (int i = 1; i < map_width + j; i++)
			{
				map.Add(Instantiate(GetRandomBlock(), lower_left_vertex_position + (j + 1) * new Vector3(-1.73f / 2, 0, -1.5f) + new Vector3(i * 1.73f, 0, 0), Quaternion.identity, transform));
			}
		}

		//fill right part
		for(int j = 0; j < map_width - 2; j++)
		{
			for (int i = 1; i < map_width + j; i++)
			{
				map.Add(Instantiate(GetRandomBlock(), lower_right_vertex_position + (j + 1) * new Vector3(-1.73f / 2, 0, 1.5f) + new Vector3(i * 1.73f, 0, 0), Quaternion.identity, transform));
			}
		}

		Dilation();
	}

	//replace water blocks that are single (don't have any water neighbour) with random ground block.
	private void Dilation()
	{
		for(int i = 0; i < map.Count; i++)
		{
			if(map[i].GetComponent<HexagonBlockBase>().block_type == BlockType.Water)
			{
				if (Physics.Raycast(map[i].transform.position + new Vector3(1.73f, .9f, 0), Vector3.down, 2, water_layer))
				{
					continue;
				}
				else if(Physics.Raycast(map[i].transform.position + new Vector3(-1.73f, 0.9f, 0), Vector3.down, 2, water_layer))
				{
					continue;
				}
				else if (Physics.Raycast(map[i].transform.position + new Vector3(1.73f / 2, 0.9f, 1.5f), Vector3.down, 2, water_layer))
				{
					continue;
				}
				else if (Physics.Raycast(map[i].transform.position + new Vector3(1.73f / 2, 0.9f, -1.5f), Vector3.down, 2, water_layer))
				{
					continue;
				}
				else if (Physics.Raycast(map[i].transform.position + new Vector3(-1.73f / 2, 0.9f, 1.5f), Vector3.down, 2, water_layer))
				{
					continue;
				}
				else if (Physics.Raycast(map[i].transform.position + new Vector3(-1.73f / 2, 0.9f, -1.5f), Vector3.down, 2, water_layer))
				{
					continue;
				}
				else
				{
					GameObject temp_block = map[i];
					map[i] = Instantiate(GetRandomBlockExceptWater(), map[i].transform.position, Quaternion.identity, transform);
					Destroy(temp_block);
				}
			}
		}

		for (int i = 0; i < map.Count; i++)
		{
			if (map[i].GetComponent<HexagonBlockBase>().block_type != BlockType.Water)
			{
				if (Physics.Raycast(map[i].transform.position + new Vector3(1.73f, .9f, 0), Vector3.down, 2, ground_layer))
				{
					continue;
				}
				else if (Physics.Raycast(map[i].transform.position + new Vector3(-1.73f, 0.9f, 0), Vector3.down, 2, ground_layer))
				{
					continue;
				}
				else if (Physics.Raycast(map[i].transform.position + new Vector3(1.73f / 2, 0.9f, 1.5f), Vector3.down, 2, ground_layer))
				{
					continue;
				}
				else if (Physics.Raycast(map[i].transform.position + new Vector3(1.73f / 2, 0.9f, -1.5f), Vector3.down, 2, ground_layer))
				{
					continue;
				}
				else if (Physics.Raycast(map[i].transform.position + new Vector3(-1.73f / 2, 0.9f, 1.5f), Vector3.down, 2, ground_layer))
				{
					continue;
				}
				else if (Physics.Raycast(map[i].transform.position + new Vector3(-1.73f / 2, 0.9f, -1.5f), Vector3.down, 2, ground_layer))
				{
					continue;
				}
				else
				{
					GameObject temp_block = map[i];
					map[i] = Instantiate(blocks[1].block_prefab, map[i].transform.position, Quaternion.identity, transform);
					Destroy(temp_block);
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
		Debug.Log(temp_block.GetComponent<HexagonBlockBase>().block_type);
		return temp_block;
	}
}
