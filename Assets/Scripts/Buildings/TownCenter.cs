using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TownCenter : NetworkBehaviour
{
	public LayerMask unitMask;
	public GameObject townCenterUIPrefab;
	[SyncVar] public TerrainHexagon occupiedHex;
	[SyncVar] public uint playerID;

	private bool menu_visible = false;
	public TownCenterUI townCenterUI;
	[SyncVar] private bool isConquered = false;

	private void Start()
	{
		townCenterUI = Instantiate(townCenterUIPrefab, GameObject.Find("Canvas").transform).GetComponent<TownCenterUI>();
		townCenterUI.townCenter = this;
		RaycastHit hit;
		if (Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, out hit, .2f))
		{
			occupiedHex = hit.collider.GetComponent<TerrainHexagon>();
			occupiedHex.OccupierBuilding = this;
		}
		playerID = netId;
	}
	public override void OnStartClient()
	{
		if(!hasAuthority) { return; }

		base.OnStartClient();
		CmdRegisterPlayer();
	}
	[Command]
	public void CmdRegisterPlayer()
	{
		OnlineGameManager.Instance.RegisterPlayer(this);
	}

	private void Update()
	{
		if (!hasAuthority) { return; }
		if (!Input.GetMouseButtonDown(0)) { return; }
		RaycastHit hit;
		if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
		{
			UnitBase unit = hit.collider.GetComponent<UnitBase>();
			if (unit != null)
			{
				ValidateUnitSelectionCmd(unit, unit.hasAuthority);
			}
			else
			{
				TownCenter townCenter = hit.collider.GetComponent<TownCenter>();
				if (townCenter != null)
				{
					ValidateTownCenterSelectionCmd(townCenter);
				}
				else
				{
					TerrainHexagon terrainHexagon = hit.collider.GetComponent<TerrainHexagon>();
					if(terrainHexagon != null)
					{
						ValidateTerrainHexagonSelectionCmd(this, terrainHexagon);
					}
				}
			}
		}
	}

	[Command]
	public void ValidateUnitSelectionCmd(UnitBase unit, bool hasAuthority)
	{
		NetworkIdentity target = unit.GetComponent<NetworkIdentity>();
		NetworkIdentity target2 = null;
		if (Map.Instance.UnitToMove != null)
		{
			target2 = Map.Instance.UnitToMove.GetComponent<NetworkIdentity>();
		}
		if (Map.Instance.UnitToMove == null)
		{
			if (hasAuthority)
			{
				RpcValidateUnitSelection(target.connectionToClient, unit, true);
			}
		}
		else if (Map.Instance.UnitToMove != unit)
		{
			if (hasAuthority)
			{
				RpcValidateUnitSelection(target2.connectionToClient, Map.Instance.UnitToMove, false);
				RpcValidateUnitSelection(target.connectionToClient, unit, true);
			}
			else
			{
				NetworkIdentity targetIdentity = Map.Instance.UnitToMove.GetComponent<NetworkIdentity>();
				Map.Instance.UnitToMove.TryAttackRpc(targetIdentity.connectionToClient, Map.Instance.UnitToMove, unit);
				Debug.LogWarning("saldirmak istiyor");
			}
		}
		else
		{
			if (hasAuthority)
			{
				RpcValidateUnitSelection(target2.connectionToClient, Map.Instance.UnitToMove, false);
			}
		}
	}

	[TargetRpc]
	public void RpcValidateUnitSelection(NetworkConnection target, UnitBase unit, bool isInMoveMode)
	{
		unit.IsInMoveMode = isInMoveMode;
	}

	[Command]
	public void ValidateTownCenterSelectionCmd(TownCenter townCenter)
	{
		if (Map.Instance.currentState != State.UnitAction)
		{
			NetworkIdentity target = townCenter.GetComponent<NetworkIdentity>();
			RpcValidateTownCenterSelection(target.connectionToClient);
		}
	}

	[TargetRpc]
	public void RpcValidateTownCenterSelection(NetworkConnection target)
	{
		ToggleBuildingMenu();
	}

	[Command]
	public void ValidateTerrainHexagonSelectionCmd(TownCenter townCenter, TerrainHexagon terrainHexagon)
	{
		if (Map.Instance.currentState == State.UnitAction)
		{
			NetworkIdentity target = townCenter.GetComponent<NetworkIdentity>();
			RpcMove(target.connectionToClient, Map.Instance.UnitToMove, terrainHexagon);
		}
	}

	[TargetRpc]
	public void RpcMove(NetworkConnection target, UnitBase unit, TerrainHexagon to)
	{
		unit.TryMoveTo(to);
	}

	public void ToggleBuildingMenu()
	{
		if (isLocalPlayer)
		{
			menu_visible = !menu_visible; 
			townCenterUI.gameObject.SetActive(menu_visible);
		}
	}

	public void CreateUnit(GameObject unitToCreate)
	{
		if (!occupiedHex.occupierUnit)
		{
			CreateUnitCmd(this);
		}
	}

	[Command]
	public void CreateUnitCmd(TownCenter owner)
	{
		NetworkIdentity target = owner.GetComponent<NetworkIdentity>();
		GameObject temp = Instantiate(NetworkRoomManagerWoT.Instance.spawnPrefabs.Find(prefab => prefab.name == "Peasant"), transform.position, Quaternion.identity);
		NetworkServer.Spawn(temp, target.connectionToClient);
		/////////////////
		occupiedHex.occupierUnit = temp.GetComponent<UnitBase>();
		temp.GetComponent<UnitBase>().occupiedHexagon = occupiedHex;
		OnlineGameManager.Instance.RegisterUnit(netId, temp.GetComponent<UnitBase>());
		temp.GetComponent<UnitBase>().playerID = netId;
		//////////////////
		//RpcUnitCreated(target.connectionToClient, temp.GetComponent<UnitBase>());
	}

	[TargetRpc]
	public void RpcUnitCreated(NetworkConnection target, UnitBase createdUnit)
	{
		//////////////////////////
	//	createdUnit.occupiedHexagon = occupiedHex;
	//	occupiedHex.OccupierUnit = createdUnit;
		//////////////////////////
		//Units.Add(createdUnit);
	}

	/*public void UnitDied(UnitBase unitBase)
	{
		Units.Remove(unitBase);
		if(Units.Count == 0 && isConquered)
		{
			//Game over for this player.
		}
	}*/
}