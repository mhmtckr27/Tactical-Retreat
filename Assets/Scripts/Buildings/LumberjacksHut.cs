using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class LumberjacksHut : BuildingBase
{
	[SerializeField] private List<TerrainType> blockedTerrainsForPeasants;
	[SerializeField] private int collectionRange = 2;
	private List<TerrainHexagon> neighboursWithinRange;
	private Forest currentForest;
	[Server]
	public int CollectResource()
	{
		return 1;
	}

	[Server]
	public void GetForests()
	{
		neighboursWithinRange = Map.Instance.GetReachableHexagons(occupiedHex, collectionRange, 0, blockedTerrainsForPeasants, null);
		foreach(TerrainHexagon neighbour in neighboursWithinRange)
		{
			if(neighbour.terrainType == TerrainType.Forest)
			{
				currentForest = neighbour.GetComponentInChildren<Forest>();
			}
		}
	}
}
