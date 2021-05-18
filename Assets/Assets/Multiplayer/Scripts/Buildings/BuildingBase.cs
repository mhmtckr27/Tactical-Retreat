using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BuildingBase : NetworkBehaviour
{
	[SerializeField] private GameObject canvasPrefab;
	[SerializeField] public BuildingType buildingType;
	
	/*[SyncVar]*/ public TerrainHexagon occupiedHex;
	[SyncVar] public uint playerID;
	
	protected TownCenterUI buildingMenuUI;
	protected UIManager uiManager;
	protected bool menu_visible = false;
	private GameObject canvas;

	protected virtual void Start()
	{
		if (!isLocalPlayer) { return; }
		canvas = Instantiate(canvasPrefab);
		uiManager = canvas.GetComponent<UIManager>();
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

	[Server]
	public void SelectBuilding(BuildingBase building)
	{
		NetworkIdentity target = building.netIdentity;
		menu_visible = true;
		ToggleBuildingMenuRpc(target.connectionToClient, menu_visible);
	}

	[Command]
	public void OnCloseTownCenterUI()
	{
		DeselectBuilding(this);
	} 

	[Server]
	public void DeselectBuilding(BuildingBase building)
	{
		NetworkIdentity target = building.netIdentity;
		menu_visible = false;
		ToggleBuildingMenuRpc(target.connectionToClient, menu_visible);
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
	WoodcutterCottage
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