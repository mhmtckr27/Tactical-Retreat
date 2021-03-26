using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TownCenter : NetworkBehaviour
{
	[SerializeField] private GameObject canvasPrefab;
	
	[SyncVar] public TerrainHexagon occupiedHex;
	[SyncVar] public uint playerID;
	[SyncVar] public bool hasTurn;
	[SyncVar] private bool isConquered = false;
	
	private TownCenterUI townCenterUI;
	private UIManager uiController;
	private bool menu_visible = false;
	private InputManager inputManager;

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
		uiController = tempCanvas.GetComponent<UIManager>();
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
		transform.eulerAngles = new Vector3(0, -60, 0);
		inputManager = GetComponent<InputManager>();
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

		if (inputManager.HasValidTap() || Input.GetMouseButtonDown(0))
		{
			ValidatePlayRequestCmd();
		}
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
		GameObject temp = Instantiate(NetworkRoomManagerWOT.Instance.spawnPrefabs.Find(prefab => prefab.name == unitName), transform.position + UnitBase.positionOffsetOnHexagons, Quaternion.identity);
		NetworkServer.Spawn(temp, gameObject);
		occupiedHex.occupierUnit = temp.GetComponent<UnitBase>();
		temp.GetComponent<UnitBase>().occupiedHexagon = occupiedHex;
		OnlineGameManager.Instance.RegisterUnit(netId, temp.GetComponent<UnitBase>());
		temp.GetComponent<UnitBase>().playerID = netId;
		ToggleBuildingMenuRpc(owner.netIdentity.connectionToClient);
	}
}