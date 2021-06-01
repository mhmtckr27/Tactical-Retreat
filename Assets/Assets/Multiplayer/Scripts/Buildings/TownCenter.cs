using Mirror;
using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class TownCenter : BuildingBase
{
	[SyncVar] public bool hasTurn;
	[SyncVar] public bool isConquered = false;
	private InputManager inputManager;

	[SerializeField] private GameObject canvasPrefab;
	[SerializeField] public int woodCount;
	[SerializeField] public int meatCount;
	[SerializeField] public int currentPopulation;
	[SerializeField] public int maxPopulation;
	[SyncVar] public int actionPoint;

	public event Action<int> OnWoodCountChange;
	public event Action<int> OnMeatCountChange;
	public event Action<int> OnActionPointChange;
	public event Action<int, int> OnCurrentToMaxPopulationChange;


	public bool isHost = false;
	private GameObject canvas;

	public override void OnStartServer()
	{
		base.OnStartServer();
		playerID = netId;
		TerrainHexagon.OnTerrainOccupiersChange += OnTerrainOccupiersChange;
		OccupiedHex.OccupierBuilding = this;
		OnTerrainOccupiersChange(OccupiedHex.Key, 1);
		StartCoroutine(InitResources());
	}

	protected void Start()
	{
		if (!isLocalPlayer) { return; }
		canvas = Instantiate(canvasPrefab);
		uiManager = canvas.GetComponent<UIManager>();
		buildingMenuUI = uiManager.townCenterUI;
		buildingMenuUI.townCenter = this;
		InitCmd();
		inputManager = GetComponent<InputManager>();

	}

	private IEnumerator InitResources()
	{
		yield return new WaitForEndOfFrame();
		UpdateResourceCount(ResourceType.Wood, 0);
		UpdateResourceCount(ResourceType.Meat, 0);
		UpdateResourceCount(ResourceType.CurrentPopulation, 0);
		UpdateResourceCount(ResourceType.MaxPopulation, 0);
		UpdateResourceCount(ResourceType.ActionPoint, 0);
	}

	//invoker: 0 means unit, 1 means building
	public void OnTerrainOccupiersChange(string key, int invoker)
	{
		if(invoker == 0)
		{
			if ((Map.Instance.mapDictionary.ContainsKey(key)) && (Map.Instance.mapDictionary[key].OccupierUnit.playerID != playerID) && (OnlineGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(playerID)))
			{
				ShowHideOccupierRpc(Map.Instance.mapDictionary[key].OccupierUnit, OnlineGameManager.Instance.PlayersToDiscoveredTerrains[playerID].Contains(Map.Instance.mapDictionary[key]), true);
				//Debug.LogError("unit null:" + Map.Instance.mapDictionary[key].OccupierUnit == null);
			}
		}
		else
		{
			if ((Map.Instance.mapDictionary.ContainsKey(key)) && (Map.Instance.mapDictionary[key].OccupierBuilding.playerID != playerID) && (OnlineGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(playerID)))
			{
				ShowHideOccupierRpc(Map.Instance.mapDictionary[key].OccupierBuilding, OnlineGameManager.Instance.PlayersToDiscoveredTerrains[playerID].Contains(Map.Instance.mapDictionary[key]), false);
				//Debug.LogError("building null:" + Map.Instance.mapDictionary[key].OccupierBuilding == null);
			}
		}
	}

	[TargetRpc]
	public void ShowHideOccupierRpc(NetworkBehaviour occupier, bool isDiscovered, bool isUnit)
	{
		if(occupier == null) {/* Debug.LogError("occupier null");*/ return; }

		if (isUnit)
		{
			((UnitBase)occupier).canvas.SetActive(isDiscovered);
		}
		foreach (MeshRenderer mr in occupier.gameObject.GetComponentsInChildren<MeshRenderer>())
		{
			mr.enabled = isDiscovered;
		}
		//occupier.gameObject.SetActive(isDiscovered);
	}

	[Command]
	public override void InitCmd()
	{
		base.InitCmd();
		//transform.eulerAngles = new Vector3(0, -60, 0);

		ExploreTerrainsRpc(Map.Instance.GetDistantHexagons(Map.Instance.mapDictionary["0_0_0"], Map.Instance.mapWidth), false);
		OnlineGameManager.Instance.AddDiscoveredTerrains(playerID, OccupiedHex.Key, 1);
	}


	[TargetRpc]
	public void ExploreTerrainsRpc(List<TerrainHexagon> distantNeighbours, bool isDiscovered)
	{
		foreach (TerrainHexagon hex in distantNeighbours)
		{
			hex.IsExplored = isDiscovered;
		}
	}

	private void Update()
	{
		if (!hasAuthority) { return; }

#if UNITY_EDITOR
		if(Map.Instance.UnitToMove == null || !Map.Instance.UnitToMove.IsMoving)//suspicious
		{
			if (Input.GetMouseButtonUp(0))
			{
				if (!EventSystem.current.IsPointerOverGameObject())
				{
					ValidatePlayRequestCmd();
				}
			}
		}
#elif UNITY_ANDROID
		if(Map.Instance.UnitToMove == null || !Map.Instance.UnitToMove.IsMoving)
		{
			if (inputManager.HasValidTap() && !IsPointerOverUIObject() &&  && EventSystem.current.currentSelectedGameObject == null)
			{
				ValidatePlayRequestCmd();
			}
		}
#endif
		/*if (Input.GetKeyDown(KeyCode.Alpha1))
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
		}*/
	}
	private bool IsPointerOverUIObject()
	{
		PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
		eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
		return results.Count > 0;
	}

	[Command]
	public virtual void CollectResource(bool canBeCollected, int costToCollect, ResourceType resourceType, int resourceCount)
	{
		//if (resource == null) { return; }
		if (canBeCollected == false) { return; }
		if (costToCollect > actionPoint) { return; }

		UpdateResourceCount(resourceType, resourceCount);
		UpdateResourceCount(ResourceType.ActionPoint, -costToCollect);
	}

	[Server]
	public void UpdateResourceCount(ResourceType resourceType, int count)
	{
		switch (resourceType)
		{
			case ResourceType.Wood:
				UpdateWoodCount(count);
				break;
			case ResourceType.Meat:
				UpdateMeatCount(count);
				break;
			case ResourceType.CurrentPopulation:
				UpdateCurrentPopulation(count);
				break;
			case ResourceType.MaxPopulation:
				UpdateMaxPopulation(count);
				break;
			case ResourceType.ActionPoint:
				UpdateActionPoint(count);
				break;
			default:
				break;
		}
	}

	[Server]
	public void UpdateWoodCount(int count)
	{
		woodCount += count;
		UpdateWoodCountRpc(woodCount);
		if (Map.Instance.selectedHexagon != null)
		{
			Map.Instance.selectedHexagon.isResourceCollected = true;
			Map.Instance.selectedHexagon.UpdateTerrainType();
		}
	}

	[TargetRpc]
	public void UpdateWoodCountRpc(int newWoodCount)
	{
		if (OnWoodCountChange != null)
		{
			OnWoodCountChange.Invoke(newWoodCount);
		}
	}

	[Server]
	public void UpdateMeatCount(int count)
	{
		meatCount += count;
		UpdateMeatCountRpc(meatCount);
		if(Map.Instance.selectedHexagon != null)
		{
			Map.Instance.selectedHexagon.isResourceCollected = true;
			Map.Instance.selectedHexagon.UpdateTerrainType();
		}
	}

	[TargetRpc]
	public void UpdateMeatCountRpc(int newMeatCount)
	{
		if (OnMeatCountChange != null)
		{
			OnMeatCountChange.Invoke(newMeatCount);
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
	private void UpdateCurrentToMaxPopulationRpc(int newCurrent, int newMax)
	{
		if (OnCurrentToMaxPopulationChange != null)
		{
			OnCurrentToMaxPopulationChange.Invoke(newCurrent, newMax);
		}
	}

	[Server]
	public void UpdateActionPoint(int count)
	{
		actionPoint += count;
		UpdateActionPointRpc(actionPoint);
	}

	[TargetRpc]
	public void UpdateActionPointRpc(int newActionPoint)
	{
		if(OnActionPointChange != null)
		{
			OnActionPointChange.Invoke(newActionPoint);
		}
	}

	[Command]
	protected void ValidatePlayRequestCmd()
	{
		if (!hasTurn) { return; }
		PlayRpc();
	}

	[TargetRpc]
	protected void PlayRpc()
	{
		PlayCmd(Camera.main.ScreenPointToRay(Input.mousePosition));
	}

	[Command]
	protected void PlayCmd(Ray ray)
	{
		Play(ray);
	}

	[Server]
	protected void Play(Ray ray)
	{
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			TerrainHexagon selectedHexagon = hit.collider.GetComponent<TerrainHexagon>();
			if(OnlineGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(playerID) && !OnlineGameManager.Instance.PlayersToDiscoveredTerrains[playerID].Contains(selectedHexagon)) 
			{
				DeselectEverything();
				return; 
			}
			//if there is no selected unit before click
			if(Map.Instance.UnitToMove == null)
			{
				//if selected terrain is empty, show terrain info
				if((selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding == null))
				{
					if(Map.Instance.selectedHexagon != selectedHexagon)
					{
						DeselectTerrain();
						if (menu_visible)
						{
							DeselectBuilding(this);
						}
						SelectTerrain(selectedHexagon);
					}
					else
					{
						DeselectTerrain();
					}
				}
				//if selected terrain only has a occupier friendly unit and not a building
				else if ((selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierUnit.playerID == playerID))
				{
					if (Map.Instance.selectedHexagon != selectedHexagon)
					{
						DeselectTerrain();
						if (menu_visible)
						{
							DeselectBuilding(this);
						}
						SelectUnit(selectedHexagon.OccupierUnit);
					}
					else
					{
						DeselectTerrain();
					}
				}
				//if selected terrain only has a occupier building and not an unit, open building menu
				else if((selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierBuilding.netId == netId))
				{
					if(Map.Instance.selectedHexagon != null)
					{
						DeselectTerrain();
					}
					if (!menu_visible)
					{
						SelectBuilding(selectedHexagon.OccupierBuilding);
					}
					else
					{
						DeselectBuilding(selectedHexagon.OccupierBuilding);
					}
				}
				//if selected terrain has both friendly unit and friendly building, select unit
				else if((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit.playerID == netId) && (selectedHexagon.OccupierBuilding.playerID == netId))
				{
					if (Map.Instance.selectedHexagon != null)
					{
						DeselectTerrain();
					}
					if (menu_visible)
					{
						DeselectBuilding(this);
					}
					SelectUnit(selectedHexagon.OccupierUnit);
				}

			}
			//if there is a selected unit
			else
			{
				//if selected terrain is empty, ValidateRequestToMove to that terrain 
				if ((selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding == null))
				{
					if (Map.Instance.UnitToMove.ValidateRequestToMove(selectedHexagon) == false)
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectTerrain(selectedHexagon);
					}
				}
				//if selected terrain only has an occupier unit and not a building
				else if ((selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierUnit.playerID == netId))
				{
					//currently selected and previously selected units are same, show terrain info
					if (Map.Instance.UnitToMove == selectedHexagon.OccupierUnit)
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectTerrain(selectedHexagon);
					}
					//select lastly selected unit
					else
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectUnit(selectedHexagon.OccupierUnit);
					}
				}
				//if selected terrain only has an occupier building and not an unit, ValidateRequestToMove building
				else if ((selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierBuilding.netId == netId))
				{
					if (Map.Instance.UnitToMove.ValidateRequestToMove(selectedHexagon) == false)
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectBuilding(selectedHexagon.OccupierBuilding);
					}
				}
				//if selected terrain has both friendly unit and friendly building, select unit on town
				else if ((selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierUnit.playerID == playerID) && (selectedHexagon.OccupierBuilding.playerID == netId))
				{
					if(Map.Instance.UnitToMove == selectedHexagon.OccupierUnit)
					{
						DeselectUnit(Map.Instance.UnitToMove);
					}
					else
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectUnit(selectedHexagon.OccupierUnit);
					}
				}
				//if selected terrain has both enemy unit and enemy building, attack to enemy unit
				else if ((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit.playerID != netId) && (selectedHexagon.OccupierBuilding.playerID != netId))
				{
					StartCoroutine(Map.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit));
				}
				//if selected terrain only has an enemy unit and not a building, attack to enemy unit
				else if((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit.playerID != netId))
				{
					StartCoroutine(Map.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit));
				}
				//if selected terrain only has an enemy building and not an unit, ValidateRequestToMove to building and can start occupation next turn
				else if ((selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding.playerID != netId))
				{
					if(Map.Instance.UnitToMove.ValidateRequestToMove(selectedHexagon) == false)
					{
						DeselectUnit(Map.Instance.UnitToMove);
					}
					else
					{
						//can start occupation process here because movement was successful
					}
				}
			}
		}
	}

	[Server]
	public void SelectUnit(UnitBase unit)
	{
		if (unit.CanMoveCmd())
		{
			unit.SetIsInMoveMode(true);
		}
	}
	
	[Server]
	public void DeselectUnit(UnitBase unit)
	{
		unit.SetIsInMoveMode(false);
	}

	[Server]
	public void SelectTerrain(TerrainHexagon terrainHexagon)
	{
		Map.Instance.selectedHexagon = terrainHexagon;
		ToggleSelectTerrainRpc((int)terrainHexagon.terrainType, true);
	}

	[Server]
	public void DeselectTerrain()
	{
		Map.Instance.selectedHexagon = null;
		ToggleSelectTerrainRpc(-1, false);
	}

	[Command]
	public void SelectBuildingCmd(BuildingBase building)
	{
		SelectBuilding(building);
	}

	[Server]
	public void SelectBuilding(BuildingBase building)
	{
		NetworkIdentity target = building.netIdentity;
		menu_visible = true;
		ToggleBuildingMenuRpc(target.connectionToClient, menu_visible);
	}

	[Server]
	public void DeselectBuilding(BuildingBase building)
	{
		NetworkIdentity target = building.netIdentity;
		menu_visible = false;
		ToggleBuildingMenuRpc(target.connectionToClient, menu_visible);
	}

	[Command]
	public void OnCloseTownCenterUI()
	{
		DeselectBuilding(this);
	}


	[TargetRpc]
	public void ToggleSelectTerrainRpc(int terrainType, bool enable)
	{
		uiManager.terrainHexagonUI.SetEnable(terrainType, enable);
	}

	[Command]
	public void ClearSelectedHexagonCmd()
	{
		Map.Instance.selectedHexagon = null;
	}

	[Server]
	public void SetHasTurn(bool newHasTurn)
	{
		hasTurn = newHasTurn;
		foreach(UnitBase unit in OnlineGameManager.Instance.GetUnits(playerID))
		{
			unit.remainingMovesThisTurn = unit.unitProperties.moveRange;
			unit.HasAttacked = false;
		}
		EnableNextTurnButton(newHasTurn);
		if (hasTurn)
		{
			int actionPointGain = CalculateActionPointGain();
			UpdateActionPoint(actionPointGain);
		}
	}

	[Server]
	public int CalculateActionPointGain()
	{
		//calculate using unit count and many other things
		return 3;
	}

	[TargetRpc]
	public void EnableNextTurnButton(bool enable)
	{
		uiManager.EnableNexTurnButton(enable);
	}

	public void FinishTurn()
	{
		FinishTurnCmd();
	}

	[Command]
	public void FinishTurnCmd()
	{
		if (!hasTurn) { return; }
		DeselectEverything();
		OnlineGameManager.Instance.PlayerFinishedTurn(this);
	}

	[Command]
	public void DeselectEverythingCmd()
	{
		DeselectEverything();
	}

	[Server]
	private void DeselectEverything()
	{
		DeselectBuilding(this);
		DeselectTerrain();
		DeselectUnitCreationPanelRpc();
		DeselectBuildingCreationPanelRpc();
		if (Map.Instance.UnitToMove != null) { DeselectUnit(Map.Instance.UnitToMove); }
	}

	[TargetRpc]
	private void DeselectUnitCreationPanelRpc()
	{
		if(uiManager != null)
		{
			uiManager.unitCreationUI.gameObject.SetActive(false);
		}
	}

	[TargetRpc]
	private void DeselectBuildingCreationPanelRpc()
	{
		if (uiManager != null)
		{
			uiManager.buildingCreationUI.gameObject.SetActive(false);
		}
	}

	public override void OnStartClient()
	{
		if (!hasAuthority) { return; }
		base.OnStartClient();
		CmdRegisterPlayer(isServer);
		SetAllCameraPositions(new Vector3(transform.position.x - 5, 0, transform.position.z + 0.75f));
	}

	private void SetAllCameraPositions(Vector3 position)
	{
		foreach (Camera cam in Camera.allCameras)
		{
			cam.transform.position = new Vector3(position.x, cam.transform.position.y, position.z);
		}
	}

	[Command]
	public void CmdRegisterPlayer(bool isHost)
	{
		OnlineGameManager.Instance.RegisterPlayer(this, isHost);
	}

	[Server]
	public bool ValidateCreateUnit(UnitBase unitScript)
	{
		return  (unitScript != null) &&
				(OccupiedHex.OccupierUnit == null) &&
				(unitScript.unitProperties.woodCostToCreate <= woodCount) &&
				(unitScript.unitProperties.meatCostToCreate <= meatCount) &&
				(unitScript.unitProperties.populationCostToCreate <= maxPopulation - currentPopulation) &&
				(unitScript.unitProperties.actionPointCostToCreate <= actionPoint);

	}

	[Command]
	public void CreateUnitCmd(string unitName)
	{
		CreateUnit(this, unitName);
	}

	[Server]
	public bool CreateUnit(BuildingBase owner, string unitName)
	{
		GameObject unit = NetworkRoomManagerWOT.singleton.spawnPrefabs.Find(prefab => prefab.name == unitName);
		if(unit == null) { return false; }
		UnitBase unitScript = unit.GetComponent<UnitBase>();
		if(ValidateCreateUnit(unitScript) == false) { return false; }

		GameObject unitObject = Instantiate(unit, transform.position + UnitProperties.positionOffsetOnHexagons, Quaternion.identity);

		unitScript = unitObject.GetComponent<UnitBase>();
		unitScript.playerColor = playerColor;
		unitScript.occupiedHex = OccupiedHex;
		unitScript.playerID = netId;
		OccupiedHex.OccupierUnit = unitScript;
		NetworkServer.Spawn(unitObject, gameObject);
		OnlineGameManager.Instance.RegisterUnit(netId, unitScript);
		ToggleBuildingMenuRpc(owner.netIdentity.connectionToClient, false);

		UpdateResourceCount(ResourceType.Wood, -unitScript.unitProperties.woodCostToCreate);
		UpdateResourceCount(ResourceType.Meat, -unitScript.unitProperties.meatCostToCreate);
		UpdateResourceCount(ResourceType.CurrentPopulation, unitScript.unitProperties.populationCostToCreate);
		UpdateResourceCount(ResourceType.ActionPoint, -unitScript.unitProperties.actionPointCostToCreate);

		return true;
	}

	[Command]
	public void CreateBuildingCmd(string buildingName)
	{
		CreateBuilding(this, buildingName);
	}

	[Server]
	public virtual bool CreateBuilding(BuildingBase owner, string buildingName)
	{
		GameObject building = NetworkRoomManagerWOT.singleton.spawnPrefabs.Find(prefab => prefab.name == buildingName);

		if (building == null) { return false; }

		BuildingBase buildingScript = building.GetComponent<BuildingBase>();

		if (ValidateCreateBuilding(buildingScript) == false) { return false; }

		GameObject buildingObject = Instantiate(building, transform.position + BuildingProperties.positionOffsetOnHexagons, Quaternion.identity);

		buildingScript = buildingObject.GetComponent<BuildingBase>();
		buildingScript.playerColor = playerColor;
		buildingScript.OccupiedHex = OccupiedHex;
		buildingScript.playerID = playerID;
		NetworkServer.Spawn(building, gameObject);
		if(buildingScript.buildingProperties.buildingType != BuildingType.House)
		{
			OccupiedHex.OccupierBuilding = buildingScript;
		}
		else
		{
			UpdateResourceCount(ResourceType.MaxPopulation, 2);
		}
		OnlineGameManager.Instance.RegisterBuilding(netId, buildingScript);
		ToggleBuildingMenuRpc(owner.netIdentity.connectionToClient, false);

		UpdateResourceCount(ResourceType.Wood, -buildingScript.buildingProperties.woodCostToCreate);
		UpdateResourceCount(ResourceType.Meat, -buildingScript.buildingProperties.meatCostToCreate);
		UpdateResourceCount(ResourceType.CurrentPopulation, -buildingScript.buildingProperties.populationCostToCreate);
		UpdateResourceCount(ResourceType.ActionPoint, -buildingScript.buildingProperties.actionPointCostToCreate);

		return true;
	}

	[Server]
	public virtual bool ValidateCreateBuilding(/*SPTerrainHexagon buildOnTerrain, */BuildingBase buildingScript)
	{/*
		if(buildingScript.buildingProperties.buildingType != BuildingType.House)
		{
			return (buildingScript != null) &&
					(buildOnTerrain.OccupierUnit == null) &&
					(buildingScript.buildingProperties.actionPointCostToCreate <= actionPoint) &&
					(unitScript.unitProperties.meatCostToCreate <= meatCount);
		}
		else
		{

		}
		*/
		return true;
	}
}
/*
public static class CustomReadWriteFunctions2
{
	public static void WriteTownCenter(this NetworkWriter writer, TownCenter value)
	{
		if (value == null) { return; }

		NetworkIdentity networkIdentity = value.GetComponent<NetworkIdentity>();
		writer.WriteNetworkIdentity(networkIdentity);

		writer.WriteUInt32(value.playerID);
		writer.WriteBoolean(value.hasTurn);
		writer.WriteBoolean(value.isConquered);
		writer.WriteInt32(value.actionPoint);
		writer.WriteColor(value.playerColor);
		//writer.WriteTerrainHexagon(value.occupiedHex);

	}

	public static TownCenter ReadTownCenter(this NetworkReader reader)
	{
		NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
		TownCenter townCenter = networkIdentity != null
			? networkIdentity.GetComponent<TownCenter>()
			: null;

		if (townCenter == null) { return null; }

		uint playerID = reader.ReadUInt32();
		bool hasTurn = reader.ReadBoolean();
		bool isConquered = reader.ReadBoolean();
		int actionPoint = reader.ReadInt32();
		Color playerColor = reader.ReadColor();
		//TerrainHexagon occupiedHex = reader.ReadTerrainHexagon();




		townCenter.playerID = playerID;
		townCenter.hasTurn = hasTurn;
		townCenter.isConquered = isConquered;
		townCenter.actionPoint = actionPoint;
		townCenter.playerColor = playerColor;
		//townCenter.occupiedHex = occupiedHex;

		return townCenter;
	}
}
*/