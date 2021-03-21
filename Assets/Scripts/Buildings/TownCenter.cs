using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TownCenter : NetworkBehaviour
{
	public LayerMask unitMask;
	public GameObject tcUI;
	public TerrainHexagon occupiedHex;
	public uint playerID;

	private bool menu_visible = false;
	public TownCenterUI townCenterUI;
	private bool isConquered = false;

	//if player loses all players AND loses town center, he/she will lose the game.
	private List<UnitBase> units;
	public List<UnitBase> Units { get => units; set => units = value; }

	private void Start()
	{
		townCenterUI = Instantiate(tcUI, GameObject.Find("Canvas").transform).GetComponent<TownCenterUI>();
		townCenterUI.townCenter = this;
		Units = new List<UnitBase>();
		RaycastHit hit;
		if (Physics.Raycast(transform.position + Vector3.up * .1f, Vector3.down, out hit, .2f))
		{
			occupiedHex = hit.collider.GetComponent<TerrainHexagon>();
		}
		Debug.LogError(netId);
		playerID = netId;
	}

	[Client]
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
				//Debug.LogWarning("unit");
				ValidateUnitSelectionCmd(unit);
			}
			else
			{
				TownCenter townCenter = hit.collider.GetComponent<TownCenter>();
				if (townCenter != null)
				{
					//Debug.LogWarning("tc");
					ValidateTownCenterSelectionCmd(townCenter);
					#region comment
					/*else
					{
						if (Map.Instance.UnitToMove.playerID == temp2.playerID)
						{
							Map.Instance.UnitToMove.TryMoveTo(temp2.occupiedHex);
						}
						else
						{
							if (temp2.occupiedHex.OccupierUnit != null)
							{
								if (temp2.occupiedHex.OccupierUnit.playerID == Map.Instance.UnitToMove.playerID)
								{
									Map.Instance.UnitToMove.IsInMoveMode = false;
									temp2.occupiedHex.OccupierUnit.IsInMoveMode = true;
								}
								else
								{
									Map.Instance.UnitToMove.Attack(temp2.occupiedHex.OccupierUnit);
								}
							}
							else
							{
								//try to occupy the building.
							}
						}
					}*/
					#endregion
				}
				else
				{
					//Debug.LogWarning("hex");
					TerrainHexagon terrainHexagon = hit.collider.GetComponent<TerrainHexagon>();
					if(terrainHexagon != null)
					{
						//Debug.LogWarning("first+");
						ValidateTerrainHexagonSelectionCmd(this, terrainHexagon);
					}
				}
			}
		}
		#region comment
		/*if (Map.Instance.UnitToMove != null && Input.GetMouseButtonDown(0))
		{
			if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit))
			{
				UnitBase temp = hit.collider.GetComponent<UnitBase>();
				if (temp != null && Map.Instance.UnitToMove.playerID != temp.playerID)
				{
					Debug.Log("saldirildi");
				}
			}
		}
		*/
		#endregion
	}

	[Command]
	public void ValidateUnitSelectionCmd(UnitBase unit)
	{
		NetworkIdentity target = unit.GetComponent<NetworkIdentity>();
		NetworkIdentity target2 = null;
		if (Map.Instance.UnitToMove != null)
		{
			target2 = Map.Instance.UnitToMove.GetComponent<NetworkIdentity>();
		}
		if (Map.Instance.UnitToMove == null)
		{
			RpcValidateUnitSelection(target.connectionToClient, unit, true);
			//unit.IsInMoveMode = true;
		}
		else if (Map.Instance.UnitToMove != unit)
		{
			RpcValidateUnitSelection(target2.connectionToClient, Map.Instance.UnitToMove, false);
			RpcValidateUnitSelection(target.connectionToClient, unit, true);
			//Map.Instance.UnitToMove.IsInMoveMode = false;
			//unit.IsInMoveMode = true;
		}
		else
		{
			//Map.Instance.UnitToMove.IsInMoveMode = false;
			RpcValidateUnitSelection(target2.connectionToClient, Map.Instance.UnitToMove, false);
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
			//Debug.LogWarning("second+");
		if (Map.Instance.currentState == State.UnitAction)
		{
			//Debug.LogWarning("third+");
			NetworkIdentity target = townCenter.GetComponent<NetworkIdentity>();
			RpcDeneme(target.connectionToClient, Map.Instance.UnitToMove, terrainHexagon);
			//Map.Instance.UnitToMove.TryMoveTo(terrainHexagon);
			//Map.Instance.UnitToMove.TryMoveTo(terrainHexagon);
		}
	}

	[TargetRpc]
	public void RpcDeneme(NetworkConnection target, UnitBase unit, TerrainHexagon to)
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
		if (!occupiedHex.OccupierUnit)
		{
			CreateUnitCmd(this);
		}
	}

	[Command]
	public void CreateUnitCmd(TownCenter owner)
	{
		/*UnitBase temp = Instantiate(unitToCreate, transform.position, Quaternion.identity).GetComponent<UnitBase>();
		temp.playerID = playerID;
		units.Add(temp);*/
		GameObject temp = Instantiate(MyNetworkRoomManager.Instance.spawnPrefabs.Find(prefab => prefab.name == "Peasant"), transform.position, Quaternion.identity);
		Debug.LogError("he " + owner.playerID);
		NetworkServer.Spawn(temp, owner.gameObject);
		temp.GetComponent<UnitBase>().playerID = owner.playerID;
	}

	public void UnitDied(UnitBase unitBase)
	{
		Units.Remove(unitBase);
		if(Units.Count == 0 && isConquered)
		{
			//Game over for this player.
		}
	}
}