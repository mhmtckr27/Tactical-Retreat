using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BuildingBase : NetworkBehaviour
{
	[SerializeField] public BuildingProperties buildingProperties;
	//[SerializeField] public BuildingType buildingType;
	
	///*[SyncVar]*/ public TerrainHexagon OccupiedHex { get; set; }
	[HideInInspector][SyncVar] public uint playerID;
	
	protected BuildingUI buildingMenuUI;
	protected UIManager uiManager;
	protected bool menu_visible = false;

	[HideInInspector][SyncVar(hook = nameof(OnPlayerColorSet))] public Color playerColor;
	//SyncList<string> discoveredTerrains = new SyncList<string>();
	public void OnPlayerColorSet(Color oldColor, Color newColor)
	{
		if(buildingProperties != null && buildingProperties.buildingType != BuildingType.House)
		{
			GetComponent<Renderer>().materials[1].color = newColor;
		}
	}

	[Command]
	public virtual void InitCmd()
	{
		/*RaycastHit hit;
		if (Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, out hit, .2f))
		{
			occupiedHex = hit.collider.GetComponent<TerrainHexagon>();
			occupiedHex.OccupierBuilding = this;
		}*/
	}



	[TargetRpc]
	public void ToggleBuildingMenuRpc(NetworkConnection target, bool enable)
	{
		if (isLocalPlayer)
		{
			buildingMenuUI.gameObject.SetActive(enable);
		}
	}
}

public enum BuildingType
{
	TownCenter,
	WoodcutterCottage,
	House
}

/*
public static class CustomReadWriteFunctions2
{
	public static void WriteBuildingBase(this NetworkWriter writer, BuildingBase value)
	{
		if (value == null) { return; }

		NetworkIdentity networkIdentity = value.GetComponent<NetworkIdentity>();
		writer.WriteNetworkIdentity(networkIdentity);
	}

	public static BuildingBase ReadTownCenter(this NetworkReader reader)
	{
		NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
		BuildingBase hex = networkIdentity != null
			? networkIdentity.GetComponent<BuildingBase>()
			: null;
		return hex;
	}
}
*/