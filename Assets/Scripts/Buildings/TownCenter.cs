using Mirror;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TownCenter : BuildingBase
{
	[SyncVar] public bool hasTurn;
	[SyncVar] public bool isConquered = false;
	private InputManager inputManager;

	[SerializeField] private int woodCount;
	[SerializeField] private int meatCount;
	private int currentPopulation;
	private int maxPopulation;
	[SyncVar] private int actionPoint;

	public bool isHost = false;

	public event Action<int> OnWoodCountChange;
	public event Action<int> OnMeatCountChange;
	public event Action<int> OnActionPointChange;
	public event Action<int, int> OnCurrentToMaxPopulationChange;

	[SyncVar(hook = nameof(OnPlayerColorSet))] public Color playerColor;
	//SyncList<string> discoveredTerrains = new SyncList<string>();
	public void OnPlayerColorSet(Color oldColor, Color newColor)
	{
		GetComponent<Renderer>().materials[1].color = newColor;
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		playerID = netId;
		TerrainHexagon.OnTerrainOccupiersChange += OnTerrainOccupiersChange;
		occupiedHex.OccupierBuilding = this;
		OnTerrainOccupiersChange(occupiedHex.Key, 1);
	}

	protected override void Start()
	{
		if (!isLocalPlayer) { return; }
		base.Start();
		buildingMenuUI = uiManager.townCenterUI;
		buildingMenuUI.townCenter = this;
		InitCmd();
		inputManager = GetComponent<InputManager>();
	}

	//invoker: 0 means unit, 1 means building
	public void OnTerrainOccupiersChange(string key, int invoker)
	{
		if(invoker == 0)
		{
			if ((Map.Instance.mapDictionary.ContainsKey(key)) && (Map.Instance.mapDictionary[key].OccupierUnit.playerID != playerID) && (OnlineGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(playerID)))
			{
				ShowHideOccupier(Map.Instance.mapDictionary[key].OccupierUnit, OnlineGameManager.Instance.PlayersToDiscoveredTerrains[playerID].Contains(key));
			}
		}
		else
		{
			if ((Map.Instance.mapDictionary.ContainsKey(key)) && (Map.Instance.mapDictionary[key].OccupierBuilding.playerID != playerID) && (OnlineGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(playerID)))
			{
				ShowHideOccupier(Map.Instance.mapDictionary[key].OccupierBuilding, OnlineGameManager.Instance.PlayersToDiscoveredTerrains[playerID].Contains(key));
			}
		}
	}

	[TargetRpc]
	public void ShowHideOccupier(NetworkBehaviour occupier, bool isDiscovered)
	{
		occupier.gameObject.SetActive(isDiscovered);
	}

	[Command]
	public override void InitCmd()
	{
		base.InitCmd();
		transform.eulerAngles = new Vector3(0, -60, 0);

		DiscoverTerrainsRpc(Map.Instance.GetDistantHexagons(Map.Instance.mapDictionary["0_0_0"], Map.Instance.mapWidth), false);
		OnlineGameManager.Instance.AddDiscoveredTerrains(playerID, occupiedHex.Key, 1);
	}


	[TargetRpc]
	public void DiscoverTerrainsRpc(List<TerrainHexagon> distantNeighbours, bool isDiscovered)
	{
		foreach (TerrainHexagon hex in distantNeighbours)
		{
			hex.IsDiscovered = isDiscovered;
		}
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
		if (inputManager.HasValidTap() && !IsPointerOverUIObject())
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
	private bool IsPointerOverUIObject()
	{
		PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
		eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
		return results.Count > 0;
	}

	[Command]
	public void UpdateResourceCountCmd(ResourceType resourceType, int count, int cost)
	{
		if(cost > actionPoint) { return; }
		switch (resourceType)
		{
			case ResourceType.Wood:
				UpdateWoodCount(count);
				break;
			case ResourceType.Meat:
				UpdateMeatCount(count);
				break;
			default:
				break;
		}
		UpdateActionPoint(-cost);
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
			if(OnlineGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(playerID) && !OnlineGameManager.Instance.PlayersToDiscoveredTerrains[playerID].Contains(selectedHexagon.Key)) 
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
					Map.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit);
				}
				//if selected terrain only has an enemy unit and not a building, attack to enemy unit
				else if((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit.playerID != netId))
				{
					Map.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit);
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
	private void EnableNextTurnButton(bool enable)
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

	[Server]
	private void DeselectEverything()
	{
		DeselectBuilding(this);
		DeselectTerrain();
		if (Map.Instance.UnitToMove != null) { DeselectUnit(Map.Instance.UnitToMove); }
	}

	public override void OnStartClient()
	{
		if (!hasAuthority) { return; }
		base.OnStartClient();
		CmdRegisterPlayer(isServer);
		foreach(Camera cam in Camera.allCameras)
		{
			cam.transform.position = new Vector3(transform.position.x - 5, cam.transform.position.y, transform.position.z + 0.75f);
		}
	}

	[Command]
	public void CmdRegisterPlayer(bool isHost)
	{
		OnlineGameManager.Instance.RegisterPlayer(this, isHost);
	}

	[Command]
	public void CreateUnitCmd(string unitName)
	{
		if (!occupiedHex.OccupierUnit)
		{
			CreateUnit(this, unitName);
		}
	}

	[Server]
	public void CreateUnit(BuildingBase owner, string unitName)
	{
		GameObject temp = Instantiate(NetworkRoomManagerWOT.Instance.spawnPrefabs.Find(prefab => prefab.name == unitName), transform.position + UnitBase.positionOffsetOnHexagons, Quaternion.identity);
		temp.GetComponent<UnitBase>().playerColor = playerColor;
		NetworkServer.Spawn(temp, gameObject);
		temp.GetComponent<UnitBase>().occupiedHexagon = occupiedHex;
		OnlineGameManager.Instance.RegisterUnit(netId, temp.GetComponent<UnitBase>());
		temp.GetComponent<UnitBase>().playerID = netId;
		occupiedHex.OccupierUnit = temp.GetComponent<UnitBase>();
		ToggleBuildingMenuRpc(owner.netIdentity.connectionToClient, false);
	}
}