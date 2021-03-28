using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceBuilding : BuildingBase
{
	[SerializeField] protected List<TerrainType> blockedTerrainsForPeasants;
	[SerializeField] protected  int collectionRange = 2;
	protected List<TerrainHexagon> neighboursWithinRange;
	protected ResourceBase currentResource;

	[Server]
	public int CollectResource()
	{
		return 1;
	}

	protected override void Start()
	{
	}

	[Server]
	public virtual void GetResourceTerrains() { }
}
