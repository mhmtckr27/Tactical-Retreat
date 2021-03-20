using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainHexagon : MonoBehaviour
{
	[SerializeField] public TerrainType terrainType;
	private GameObject[] outlines = new GameObject[2];
	private int[] coordinates = new int[3];

	private List<string> neighbourKeys;
	private string neighbour_N;
	private string neighbour_NE;
	private string neighbour_SE;
	private string neighbour_S;
	private string neighbour_SW;
	private string neighbour_NW;

	public List<string> NeighbourKeys { get => neighbourKeys; set => neighbourKeys = value; }
	public string Neighbour_N { get => neighbour_N; set => neighbour_N = value; }
	public string Neighbour_NE { get => neighbour_NE; set => neighbour_NE = value; }
	public string Neighbour_SE { get => neighbour_SE; set => neighbour_SE = value; }
	public string Neighbour_S { get => neighbour_S; set => neighbour_S = value; }
	public string Neighbour_SW { get => neighbour_SW; set => neighbour_SW = value; }
	public string Neighbour_NW { get => neighbour_NW; set => neighbour_NW = value; }
	public UnitBase OccupierUnit { get => occupierUnit; set => occupierUnit = value; }
	public int[] Coordinates { get => coordinates; set => coordinates = value; }

	private UnitBase occupierUnit;
	[SerializeField] private TownCenter occupierBuilding;
	public TownCenter OccupierBuilding { get => occupierBuilding; set => occupierBuilding = value; }

	private void Awake()
	{
		outlines[0] = transform.GetChild(0).gameObject;
		outlines[1] = transform.GetChild(1).gameObject;
		NeighbourKeys = new List<string>();
	}

	public void ToggleOutlineVisibility(int outline_index, bool show_outline)
	{
		outlines[outline_index].SetActive(show_outline);
	}

	public void SetCoordinates(int x, int y, int z)
	{
		Coordinates[0] = x;
		Coordinates[1] = y;
		Coordinates[2] = z;

		SetNeighbourCoordinates();
	}

	private void SetNeighbourCoordinates()
	{
		Neighbour_N = (Coordinates[0]) + "_" + (Coordinates[1] + 1) + "_" + (Coordinates[2] - 1);
		Neighbour_NE = (Coordinates[0] + 1) + "_" + (Coordinates[1]) + "_" + (Coordinates[2] - 1);
		Neighbour_SE = (Coordinates[0] + 1) + "_" + (Coordinates[1] - 1) + "_" + (Coordinates[2]);
		Neighbour_S = (Coordinates[0]) + "_" + (Coordinates[1] - 1) + "_" + (Coordinates[2] + 1);
		Neighbour_SW = (Coordinates[0] - 1) + "_" + (Coordinates[1]) + "_" + (Coordinates[2] + 1);
		Neighbour_NW = (Coordinates[0] - 1) + "_" + (Coordinates[1] + 1) + "_" + (Coordinates[2]);

		NeighbourKeys.Add(Neighbour_N);
		NeighbourKeys.Add(Neighbour_NE);
		NeighbourKeys.Add(Neighbour_SE);
		NeighbourKeys.Add(Neighbour_S);
		NeighbourKeys.Add(Neighbour_SW);
		NeighbourKeys.Add(Neighbour_NW);
	}

	private void OnMouseUpAsButton()
	{
		if(Map.Instance.currentState == State.UnitMovement)
		{
			Map.Instance.UnitToMove.TryMoveTo(this);
		}
		else if(occupierBuilding != null)
		{
			occupierBuilding.ToggleBuildingMenu();
		}
	}
}

public enum TerrainType
{
	Ground,
	Water,
	Forest
}
