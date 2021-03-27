using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BuildingBase : NetworkBehaviour
{
	[SerializeField] private GameObject canvasPrefab;
	[SerializeField] public BuildingType buildingType;
	
	[SyncVar] public TerrainHexagon occupiedHex;
	[SyncVar] public uint playerID;
	
	protected TownCenterUI buildingMenuUI;
	protected UIManager uiManager;
	private bool menu_visible = false;

	protected virtual void Start()
	{
		GameObject tempCanvas = Instantiate(canvasPrefab);
		uiManager = tempCanvas.GetComponent<UIManager>();

		if (!hasAuthority)
		{
			tempCanvas.SetActive(false);
			Destroy(tempCanvas);
		}

		RaycastHit hit;
		if (Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, out hit, .2f))
		{
			occupiedHex = hit.collider.GetComponent<TerrainHexagon>();
			occupiedHex.OccupierBuilding = this;
		}
	}

	[Command]
	public void ValidateBuildingSelectionCmd(BuildingBase bulding)
	{
		if (Map.Instance.currentState != State.UnitAction)
		{
			NetworkIdentity target = bulding.netIdentity;
			ToggleBuildingMenuRpc(target.connectionToClient);
		}
	}

	[TargetRpc]
	public void ToggleBuildingMenuRpc(NetworkConnection target)
	{
		if (isLocalPlayer)
		{
			menu_visible = !menu_visible; 
			buildingMenuUI.gameObject.SetActive(menu_visible);
		}
	}
}

public enum BuildingType
{
	TownCenter,
	WoodcutterCottage
}