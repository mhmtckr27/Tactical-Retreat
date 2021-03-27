using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using UnityEngine.EventSystems;

public class TownCenter : BuildingBase
{
	[SyncVar] public bool hasTurn;
	//[SyncVar] private bool isConquered = false;
	private InputManager inputManager;

	[SerializeField] private int woodCount;
	[SerializeField] private int meatCount;
	private int currentPopulation;
	private int maxPopulation;

	public event Action<int> onWoodCountChange;
	public event Action<int> onMeatCountChange;
	public event Action<int, int> onCurrentToMaxPopulationChange;

	public TerrainMenuUI terrainConstructionMenuUI;
	protected override void Start()
	{
		base.Start();
		buildingMenuUI = uiManager.townCenterUI;
		buildingMenuUI.townCenter = this;
		terrainConstructionMenuUI = uiManager.terrainConstructionMenuUI;
		terrainConstructionMenuUI.townCenter = this;
		transform.eulerAngles = new Vector3(0, -60, 0);
		inputManager = GetComponent<InputManager>();
	}

	private void Update()
	{
		if (!hasAuthority) { return; }

#if UNITY_EDITOR
		if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			ValidatePlayRequestCmd();
		}
#elif UNITY_ANDROID
		if (inputManager.HasValidTap() && !EventSystem.current.IsPointerOverGameObject())
		{
			ValidatePlayRequestCmd();
		}
#endif
		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			UpdateWoodCount(1);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			UpdateMeatCount(1);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			UpdateCurrentPopulation(1);
		}
		else if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			UpdateMaxPopulation(1);
		}
	}

	[Server]
	public void UpdateWoodCount(int count)
	{
		woodCount += count;
		UpdateWoodCountRpc(woodCount);
	}

	[TargetRpc]
	public void UpdateWoodCountRpc(int count)
	{
		if (onWoodCountChange != null)
		{
			onWoodCountChange.Invoke(count);
		}
	}

	[Server]
	public void UpdateMeatCount(int count)
	{
		meatCount += count;
		UpdateMeatCountRpc(meatCount);
	}

	[TargetRpc]
	public void UpdateMeatCountRpc(int count)
	{
		if (onMeatCountChange != null)
		{
			onMeatCountChange.Invoke(count);
		}
	}

	[Server]
	public void UpdateCurrentPopulation(int count)
	{
		currentPopulation += count;
		UpdateCurrentToMaxPopulationRpc(currentPopulation, maxPopulation);
	}

	[Server]
	private void UpdateMaxPopulation(int count)
	{
		maxPopulation += count;
		UpdateCurrentToMaxPopulationRpc(currentPopulation, maxPopulation);
	}

	[TargetRpc]
	private void UpdateCurrentToMaxPopulationRpc(int current, int max)
	{
		if (onCurrentToMaxPopulationChange != null)
		{
			onCurrentToMaxPopulationChange.Invoke(current, max);
		}
	}

	[Command]
	protected void ValidatePlayRequestCmd()
	{
		if (!hasTurn) { return; }
		Play();
	}

	[TargetRpc]
	protected void Play()
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
				BuildingBase building = hit.collider.GetComponent<BuildingBase>();
				if (building != null)
				{
					ValidateBuildingSelectionCmd(building);
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
	public void ValidateTerrainHexagonSelectionCmd(BuildingBase townCenter, TerrainHexagon terrainHexagon)
	{
		if (Map.Instance.currentState == State.UnitAction)
		{
			Map.Instance.UnitToMove.ValidateRequestToMove(terrainHexagon);
		}
		else if((Map.Instance.currentState == State.None) && (terrainHexagon.OccupierBuilding == null) && (terrainHexagon.occupierUnit == null))
		{
			if ((!uiManager.terrainConstructionMenuUI.gameObject.activeInHierarchy) || (uiManager.terrainConstructionMenuUI.CurrentTerrain != terrainHexagon))
			{
				uiManager.terrainConstructionMenuUI.CurrentTerrain = terrainHexagon;
				uiManager.terrainConstructionMenuUI.gameObject.SetActive(true);
			}
			else if (uiManager.terrainConstructionMenuUI.gameObject.activeInHierarchy && (uiManager.terrainConstructionMenuUI.CurrentTerrain == terrainHexagon))
			{
				uiManager.terrainConstructionMenuUI.CurrentTerrain = null;
				uiManager.terrainConstructionMenuUI.gameObject.SetActive(false);
			}
		}
	}

	[Server]
	public void SetHasTurn(bool newHasTurn)
	{
		hasTurn = newHasTurn;
		EnableNextTurnButton(newHasTurn);
		if (hasTurn)
		{
			UpdateResources();
		}
	}

	[Server]
	private void UpdateResources()
	{
		List<BuildingBase> buildings = OnlineGameManager.Instance.GetBuildings(netId);
		if(buildings == null)
		{
			return;
		}
		foreach (BuildingBase building in buildings)
		{
			if(building.buildingType == BuildingType.Wood)
			{
				UpdateWoodCount((building as LumberjacksHut).CollectResource());
			}
		}
	}

	[TargetRpc]
	private void EnableNextTurnButton(bool enable)
	{
		uiManager.EnableNexTurnButton(enable);
	}

	[Command]
	public void FinishTurnCmd()
	{
		OnlineGameManager.Instance.PlayerFinishedTurn(this);
	}

	public override void OnStartClient()
	{
		if (!hasAuthority) { return; }
		base.OnStartClient();

		CmdRegisterPlayer();
	}

	[Command]
	public void CmdRegisterPlayer()
	{
		OnlineGameManager.Instance.RegisterPlayer(this);
		playerID = netId;
	}

	[Command]
	public void CreateUnitCmd(string unitName)
	{
		if (!occupiedHex.occupierUnit)
		{
			CreateUnit(unitName);
		}
	}

	[Server]
	public void CreateUnit(string unitName)
	{
		GameObject temp = Instantiate(NetworkRoomManagerWOT.Instance.spawnPrefabs.Find(prefab => prefab.name == unitName), transform.position + UnitBase.positionOffsetOnHexagons, Quaternion.identity);
		NetworkServer.Spawn(temp, gameObject);
		occupiedHex.occupierUnit = temp.GetComponent<UnitBase>();
		temp.GetComponent<UnitBase>().occupiedHexagon = occupiedHex;
		OnlineGameManager.Instance.RegisterUnit(netId, temp.GetComponent<UnitBase>());
		temp.GetComponent<UnitBase>().playerID = netId;
		ToggleBuildingMenuRpc(netIdentity.connectionToClient);
	}

	[Command]
	public void CreateBuildingCmd(string buildingName, TerrainHexagon terrain)
	{
		Debug.LogWarning("burdayim");
		if(woodCount >= NetworkRoomManagerWOT.Instance.spawnPrefabs.Find(prefab => prefab.name == buildingName).GetComponent<BuildingBase>().buildCostWood)
		{
			Debug.LogWarning("hoop burdayim");
			CreateBuilding(buildingName, terrain);
		}
	}

	[Server]
	public void CreateBuilding(string buildingName, TerrainHexagon terrain)
	{
		GameObject temp = Instantiate(NetworkRoomManagerWOT.Instance.spawnPrefabs.Find(prefab => prefab.name == buildingName), terrain.transform.position, Quaternion.identity);
		NetworkServer.Spawn(temp, gameObject);
		terrain.OccupierBuilding = temp.GetComponent<BuildingBase>();
		temp.GetComponent<BuildingBase>().occupiedHex = terrain;
		OnlineGameManager.Instance.RegisterBuilding(netId, temp.GetComponent<BuildingBase>());
		temp.GetComponent<BuildingBase>().playerID = netId;
	}
}