using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TerrainHexagon : NetworkBehaviour
{
	[SerializeField] public TerrainType terrainType;
	[SerializeField] public GameObject resourceGameObject;
	//[SerializeField] public Resource resource;

	//TODO: attention pls! eger problem yaratirsa syncvar yap, sebebini bilmiyorum :d
	[SyncVar] public GameObject unexploredBlock;

	private bool isExplored;
	/*public bool IsExplored
	{
		get => isExplored;
		set
		{
			isExplored = value;
			OnIsExploredChange();
			OnIsExploredChangeCmd();
		}
	}*/

	[HideInInspector] [SyncVar(hook = nameof(OnResourceCollected))] public bool isResourceCollected;
	[HideInInspector] public List<BuildingType> buildablesOnThisTerrain;
	public GameObject[] outlines = new GameObject[2];
	private int[] coordinates = new int[3];
	private string key;

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
	public int[] Coordinates { get => coordinates; set => coordinates = value; }
	public string Key { get => key; set => key = value; }


	public UnitBase occupierUnit;


	public BuildingBase occupierBuilding;
	/*public UnitBase OccupierUnit
	{
		get => occupierUnit;
		set
		{
			occupierUnit = value;
			if ((OnTerrainOccupiersChange != null) && (occupierUnit != null))
			{
				OnTerrainOccupiersChange.Invoke(Key, 0);
			}
		}
	}
	public BuildingBase OccupierBuilding
	{
		get => occupierBuilding;
		set
		{
			occupierBuilding = value;
			if ((OnTerrainOccupiersChange != null) && (occupierBuilding != null))
			{
				OnTerrainOccupiersChange.Invoke(Key, 1);
			}
		}
	}*/

	public static event Action<string, int> OnTerrainOccupiersChange;

	public void SetOccupierBuilding(BuildingBase newValue)
	{
		occupierBuilding = newValue;
		if ((OnTerrainOccupiersChange != null) && (occupierBuilding != null))
		{
			OnTerrainOccupiersChange.Invoke(Key, 1);
		}
		if (!isServer)
		{
			Debug.LogError("hello mf");
			SetOccupierBuildingCmd(newValue);
		}
	}
	[Command(requiresAuthority = false)]
	public void SetOccupierBuildingCmd(BuildingBase newValue)
	{
		occupierBuilding = newValue;
		if ((OnTerrainOccupiersChange != null) && (occupierUnit != null))
		{
			OnTerrainOccupiersChange.Invoke(Key, 1);
		}
	}
	public void SetOccupierUnit(UnitBase newValue)
	{
		occupierUnit = newValue;
		if ((OnTerrainOccupiersChange != null) && (occupierUnit != null))
		{
			OnTerrainOccupiersChange.Invoke(Key, 0);
		}
		if (!isServer)
		{
			SetOccupierUnitCmd(newValue);
		}
	}

	[Command(requiresAuthority = false)]
	public void SetOccupierUnitCmd(UnitBase newValue)
	{
		occupierUnit = newValue;
		if ((OnTerrainOccupiersChange != null) && (occupierUnit != null))
		{
			OnTerrainOccupiersChange.Invoke(Key, 0);
		}
	}

	[Server]
	public BuildingBase GetOccupierBuilding()
	{
		return occupierBuilding;
	}

	[Server]
	public UnitBase GetOccupierUnit()
	{
		return occupierUnit;
	}

	public void SetIsExplored(bool newValue)
	{
		isExplored = newValue;
		OnIsExploredChange();
		OnIsExploredChangeCmd();
	}

	public bool GetIsExplored()
	{
		return isExplored;
	}

	[Command(requiresAuthority = false)]
	public void OnIsExploredChangeCmd()
	{
		if ((OnTerrainOccupiersChange != null) && (occupierUnit != null))
		{
			OnTerrainOccupiersChange.Invoke(Key, 0);
		}
		if ((OnTerrainOccupiersChange != null) && (occupierBuilding != null))
		{
			OnTerrainOccupiersChange.Invoke(Key, 1);
		}
	}

	public void OnIsExploredChange()
	{
		unexploredBlock.GetComponent<MeshRenderer>().enabled = !isExplored;
		foreach (MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
		{
			meshRenderer.enabled = isExplored;
		}
		/*if(OccupierUnit != null)
		{
			foreach (MeshRenderer meshRenderer in OccupierUnit.GetComponentsInChildren<MeshRenderer>())
			{
				meshRenderer.enabled = IsDiscovered;
			}
		}
		if(occupierBuilding != null)
		{
			foreach (MeshRenderer meshRenderer in occupierBuilding.GetComponentsInChildren<MeshRenderer>())
			{
				meshRenderer.enabled = IsDiscovered;
			}
		}*/
	}

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

	private void OnResourceCollected(bool oldVal, bool newVal)
	{
		if(newVal)
		{
			//NetworkServer.Destroy(resourceGameObject);
			Destroy(resourceGameObject);
		}
	}

	[Server]
	public void UpdateTerrainType()
	{
		Destroy(resourceGameObject);
		terrainType = TerrainType.Plain;
	}

	public void SetCoordinates(int x, int y, int z)
	{
		Coordinates[0] = x;
		Coordinates[1] = y;
		Coordinates[2] = z;

		key = x + "_" + y + "_" + z;

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
	
	/*public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		Debug.LogWarning("onser:" + writer.Length);
		CustomReadWriteFunctions.WriteTerrainHexagon(writer, this);
		return base.OnSerialize(writer, initialState);
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		Debug.LogWarning("ondeser:" + reader.Length);
		CustomReadWriteFunctions.ReadTerrainHexagon(reader);
		base.OnDeserialize(reader, initialState);
	}*/
}

public enum TerrainType
{
	Plain,
	Water,
	Forest,
	Animals
}
/*
public static class CustomReadWriteFunctions
{
	public static void WriteTerrainHexagon(this NetworkWriter writer, TerrainHexagon value)
	{
		if(value == null) { return; }
		writer.WriteInt32((int)value.terrainType);
		writer.WriteGameObject(value.resourceGameObject);
		writer.WriteGameObject(value.unexploredBlock);
		writer.WriteBoolean(value.GetIsExplored());
		writer.WriteBoolean(value.isResourceCollected);
		writer.WriteList<BuildingType>(value.buildablesOnThisTerrain);
		writer.WriteArray<GameObject>(value.outlines);
		writer.WriteArray<int>(value.Coordinates);
		writer.WriteString(value.Key);
		writer.WriteList<string>(value.NeighbourKeys);
		writer.WriteString(value.Neighbour_N);
		writer.WriteString(value.Neighbour_NE);
		writer.WriteString(value.Neighbour_SE);
		writer.WriteString(value.Neighbour_S);
		writer.WriteString(value.Neighbour_SW);
		writer.WriteString(value.Neighbour_NW);

		writer.WriteNetworkBehaviour(value);

		writer.WriteNetworkBehaviour(value.GetOccupierUnit());

		writer.WriteNetworkBehaviour(value.GetOccupierBuilding());

		Debug.LogWarning("server mi writer");
	}

	public static TerrainHexagon ReadTerrainHexagon(this NetworkReader reader)
	{
		TerrainType terrainType = (TerrainType) reader.ReadInt32();
		GameObject resourceGameObject = reader.ReadGameObject();
		GameObject unexploredBlock = reader.ReadGameObject();
		bool isExplored = reader.ReadBoolean();
		bool isResourceCollected = reader.ReadBoolean();
		List<BuildingType> buildablesOnThisTerrain = reader.ReadList<BuildingType>();
		GameObject[] outlines = reader.ReadArray<GameObject>();
		int[] coordinates = reader.ReadArray<int>();
		string key = reader.ReadString();
		List<string> neighbourKeys = reader.ReadList<string>();
		string neighbour_n = reader.ReadString();
		string neighbour_ne = reader.ReadString();
		string neighbour_se = reader.ReadString();
		string neighbour_s = reader.ReadString();
		string neighbour_sw = reader.ReadString();
		string neighbour_nw = reader.ReadString();



		TerrainHexagon toReturn = reader.ReadNetworkBehaviour() as TerrainHexagon;
		UnitBase unit = reader.ReadNetworkBehaviour() as UnitBase;
		BuildingBase building = reader.ReadNetworkBehaviour() as BuildingBase;

		toReturn.terrainType = terrainType;
		toReturn.resourceGameObject = resourceGameObject;
		toReturn.unexploredBlock = unexploredBlock;
		toReturn.SetIsExplored(isExplored);
		toReturn.isResourceCollected = isResourceCollected;
		toReturn.buildablesOnThisTerrain = buildablesOnThisTerrain;
		toReturn.outlines = outlines;
		toReturn.Coordinates = coordinates;
		toReturn.Key = key;
		toReturn.NeighbourKeys = neighbourKeys;
		toReturn.Neighbour_N = neighbour_n;
		toReturn.Neighbour_NE = neighbour_ne;
		toReturn.Neighbour_SE = neighbour_se;
		toReturn.Neighbour_S = neighbour_s;
		toReturn.Neighbour_SW = neighbour_sw;
		toReturn.Neighbour_NW = neighbour_nw;

		toReturn.SetOccupierUnit(unit);
		toReturn.SetOccupierBuilding(building);
		Debug.LogWarning("server mi reader");


		return toReturn;
	}
}*/