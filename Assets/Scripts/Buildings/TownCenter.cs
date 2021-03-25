using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TownCenter : NetworkBehaviour
{
	public LayerMask unitMask;
	[SyncVar] public TerrainHexagon occupiedHex;
	[SyncVar] public uint playerID;
	[SyncVar] public bool hasTurn;
	private bool menu_visible = false;
	public TownCenterUI townCenterUI;
	public UIController uiController;
	[SyncVar] private bool isConquered = false;
	[SerializeField] private GameObject canvasPrefab;

	[Server]
	public void SetHasTurn(bool newHasTurn)
	{
		hasTurn = newHasTurn;
		EnableNextTurnButton(newHasTurn);
	}

	[TargetRpc]
	public void EnableNextTurnButton(bool enable)
	{
		uiController.EnableNexTurnButton(enable);
	}

	private void Start()
	{
		GameObject tempCanvas = Instantiate(canvasPrefab);
		uiController = tempCanvas.GetComponent<UIController>();
		townCenterUI = uiController.townCenterUI;
		townCenterUI.townCenter = this;

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
	public override void OnStartClient()
	{
		if(!hasAuthority) { return; }

		base.OnStartClient();
		CmdRegisterPlayer(); 
	}

	[Command]
	public void FinishTurnCmd()
	{
		OnlineGameManager.Instance.PlayerFinishedTurn(this);
	}

	[Command]
	public void CmdRegisterPlayer()
	{
		OnlineGameManager.Instance.RegisterPlayer(this);
		playerID = netId;
	}

	private void Update()
	{
		if (!hasAuthority) { return; }
		if (!Input.GetMouseButtonDown(0)) { return; }
		ValidatePlayRequestCmd();
	}

	[Command]
	private void ValidatePlayRequestCmd()
	{
		if(!hasTurn) { return; }
		Play();
	}

	[TargetRpc]
	public void Play()
	{
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
					if (terrainHexagon != null)
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
			if (hasAuthority && unit.CanMoveCmd())
			{
				unit.SetIsInMoveMode(true);
			}
		}
		else if (Map.Instance.UnitToMove != unit)
		{
			if (hasAuthority)
			{
				Map.Instance.UnitToMove.SetIsInMoveMode(false);
				if (unit.CanMoveCmd())
				{
					unit.SetIsInMoveMode(true);
				}
			}
			else
			{
				NetworkIdentity targetIdentity = Map.Instance.UnitToMove.GetComponent<NetworkIdentity>();
				Map.Instance.UnitToMove.ValidateAttack(unit);
			}
		}
		else
		{
			if (hasAuthority)
			{
				Map.Instance.UnitToMove.SetIsInMoveMode(false);
			}
		}
	}

	[Command]
	public void ValidateTownCenterSelectionCmd(TownCenter townCenter)
	{
		if (Map.Instance.currentState != State.UnitAction)
		{
			NetworkIdentity target = townCenter.netIdentity;
			ToggleBuildingMenuRpc(target.connectionToClient);
		}
	}

	[Command]
	public void ValidateTerrainHexagonSelectionCmd(TownCenter townCenter, TerrainHexagon terrainHexagon)
	{
		if (Map.Instance.currentState == State.UnitAction)
		{
			Map.Instance.UnitToMove.ValidateRequestToMove(terrainHexagon);
		}
	}
	
	[TargetRpc]
	public void ToggleBuildingMenuRpc(NetworkConnection target)
	{
		if (isLocalPlayer)
		{
			menu_visible = !menu_visible; 
			townCenterUI.gameObject.SetActive(menu_visible);
		}
	}

	public void CreateUnitt(string unitName)
	{
		CreateUnitCmd(unitName);
	}

	[Command]
	public void CreateUnitCmd(string unitName)
	{
		if (!occupiedHex.occupierUnit)
		{
			CreateUnit(this, unitName);
		}
	}

	[Server]
	public void CreateUnit(TownCenter owner, string unitName)
	{
		GameObject temp = Instantiate(NetworkRoomManagerWoT.Instance.spawnPrefabs.Find(prefab => prefab.name == unitName), transform.position, Quaternion.identity);
		NetworkServer.Spawn(temp, gameObject);
		occupiedHex.occupierUnit = temp.GetComponent<UnitBase>();
		temp.GetComponent<UnitBase>().occupiedHexagon = occupiedHex;
		OnlineGameManager.Instance.RegisterUnit(netId, temp.GetComponent<UnitBase>());
		temp.GetComponent<UnitBase>().playerID = netId;
	}
}