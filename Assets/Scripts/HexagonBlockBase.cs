using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexagonBlockBase : MonoBehaviour
{
	[SerializeField] public BlockType block_type;
	public int[] coordinates = new int[3];

	public List<string> neighbour_keys;
	public string neighbour_n;
	public string neighbour_ne;
	public string neighbour_se;
	public string neighbour_s;
	public string neighbour_sw;
	public string neighbour_nw;

	private Outline outline;

	private void Awake()
	{
		outline = GetComponent<Outline>();
	}

	public void ToggleOutlineVisibility(bool show_outline)
	{
		outline.enabled = show_outline;
	}

	public void SetCoordinates(int x, int y, int z)
	{
		coordinates[0] = x;
		coordinates[1] = y;
		coordinates[2] = z;

		SetNeighbourCoordinates();
	}

	private void SetNeighbourCoordinates()
	{
		neighbour_n = (coordinates[0]) + "_" + (coordinates[1] + 1) + "_" + (coordinates[2] - 1);
		neighbour_ne = (coordinates[0] + 1) + "_" + (coordinates[1]) + "_" + (coordinates[2] - 1);
		neighbour_se = (coordinates[0] + 1) + "_" + (coordinates[1] - 1) + "_" + (coordinates[2]);
		neighbour_s = (coordinates[0]) + "_" + (coordinates[1] - 1) + "_" + (coordinates[2] + 1);
		neighbour_sw = (coordinates[0] - 1) + "_" + (coordinates[1]) + "_" + (coordinates[2] + 1);
		neighbour_nw = (coordinates[0] - 1) + "_" + (coordinates[1] + 1) + "_" + (coordinates[2]);

		neighbour_keys.Add(neighbour_n);
		neighbour_keys.Add(neighbour_ne);
		neighbour_keys.Add(neighbour_se);
		neighbour_keys.Add(neighbour_s);
		neighbour_keys.Add(neighbour_sw);
		neighbour_keys.Add(neighbour_nw);
	}
}

public enum BlockType
{
	Ground,
	Water
}