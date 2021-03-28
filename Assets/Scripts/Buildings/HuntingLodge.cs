using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuntingLodge : ResourceBuilding
{
	protected override void Start()
	{
		if(!hasAuthority) { return; }
		buildingUI = uiManager.huntingLodgeUI;
		(buildingUI as HuntingLodgeUI).huntingLodge = this;
	}

	[Server]
	public override void GetResourceTerrains()
	{
		neighboursWithinRange = Map.Instance.GetReachableHexagons(occupiedHex, collectionRange, 0, blockedTerrainsForPeasants, null);
		foreach (TerrainHexagon neighbour in neighboursWithinRange)
		{
			if (neighbour.terrainType == TerrainType.Animals)
			{
				currentResource = neighbour.GetComponentInChildren<ResourceBase>();
			}
		}
	}
}
