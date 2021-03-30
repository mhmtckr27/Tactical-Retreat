using Mirror;
using System;
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

	public event Action<int> onWoodCountChange;
	public event Action<int> onMeatCountChange;
	public event Action<int> onActionPointChange;
	public event Action<int, int> onCurrentToMaxPopulationChange;
	

	protected override void Start()
	{
		if (!isLocalPlayer) { return; }
		base.Start();
		buildingMenuUI = uiManager.townCenterUI;
		buildingMenuUI.townCenter = this;
		InitCmd();
		inputManager = GetComponent<InputManager>();
	}

	[Command]
	public override void InitCmd()
	{
		base.InitCmd();
		transform.eulerAngles = new Vector3(0, -60, 0);
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
		if (onWoodCountChange != null)
		{
			onWoodCountChange.Invoke(newWoodCount);
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
		if (onMeatCountChange != null)
		{
			onMeatCountChange.Invoke(newMeatCount);
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
		if (onCurrentToMaxPopulationChange != null)
		{
			onCurrentToMaxPopulationChange.Invoke(newCurrent, newMax);
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
		if(onActionPointChange != null)
		{
			onActionPointChange.Invoke(newActionPoint);
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
			//if there is no selected unit before click
			if(Map.Instance.UnitToMove == null)
			{
				//if selected terrain is empty, show terrain info
				if((selectedHexagon.occupierUnit == null) && (selectedHexagon.OccupierBuilding == null))
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
				else if ((selectedHexagon.OccupierBuilding == null) && (selectedHexagon.occupierUnit != null) && (selectedHexagon.occupierUnit.playerID == playerID))
				{
					if (Map.Instance.selectedHexagon != selectedHexagon)
					{
						DeselectTerrain();
						if (menu_visible)
						{
							DeselectBuilding(this);
						}
						SelectUnit(selectedHexagon.occupierUnit);
					}
					else
					{
						DeselectTerrain();
					}
				}
				//if selected terrain only has a occupier building and not an unit, open building menu
				else if((selectedHexagon.occupierUnit == null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierBuilding.netId == netId))
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
				else if((selectedHexagon.occupierUnit != null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.occupierUnit.playerID == netId) && (selectedHexagon.OccupierBuilding.playerID == netId))
				{
					if (Map.Instance.selectedHexagon != null)
					{
						DeselectTerrain();
					}
					if (menu_visible)
					{
						DeselectBuilding(this);
					}
					SelectUnit(selectedHexagon.occupierUnit);
				}

			}
			//if there is a selected unit
			else
			{
				//if selected terrain is empty, ValidateRequestToMove to that terrain 
				if ((selectedHexagon.occupierUnit == null) && (selectedHexagon.OccupierBuilding == null))
				{
					if (Map.Instance.UnitToMove.ValidateRequestToMove(selectedHexagon) == false)
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectTerrain(selectedHexagon);
					}
				}
				//if selected terrain only has an occupier unit and not a building
				else if ((selectedHexagon.OccupierBuilding == null) && (selectedHexagon.occupierUnit != null) && (selectedHexagon.occupierUnit.playerID == netId))
				{
					//currently selected and previously selected units are same, show terrain info
					if (Map.Instance.UnitToMove == selectedHexagon.occupierUnit)
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectTerrain(selectedHexagon);
					}
					//select lastly selected unit
					else
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectUnit(selectedHexagon.occupierUnit);
					}
				}
				//if selected terrain only has an occupier building and not an unit, ValidateRequestToMove building
				else if ((selectedHexagon.occupierUnit == null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierBuilding.netId == netId))
				{
					if (Map.Instance.UnitToMove.ValidateRequestToMove(selectedHexagon) == false)
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectBuilding(selectedHexagon.OccupierBuilding);
					}
				}
				//if selected terrain has both friendly unit and friendly building, select unit on town
				else if ((selectedHexagon.OccupierBuilding != null) && (selectedHexagon.occupierUnit != null) && (selectedHexagon.occupierUnit.playerID == playerID) && (selectedHexagon.OccupierBuilding.playerID == netId))
				{
					if(Map.Instance.UnitToMove == selectedHexagon.occupierUnit)
					{
						DeselectUnit(Map.Instance.UnitToMove);
					}
					else
					{
						DeselectUnit(Map.Instance.UnitToMove);
						SelectUnit(selectedHexagon.occupierUnit);
					}
				}
				//if selected terrain has both enemy unit and enemy building, attack to enemy unit
				else if ((selectedHexagon.occupierUnit != null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.occupierUnit.playerID != netId) && (selectedHexagon.OccupierBuilding.playerID != netId))
				{
					Map.Instance.UnitToMove.ValidateAttack(selectedHexagon.occupierUnit);
				}
				//if selected terrain only has an enemy unit and not a building, attack to enemy unit
				else if((selectedHexagon.occupierUnit != null) && (selectedHexagon.OccupierBuilding == null) && (selectedHexagon.occupierUnit.playerID != netId))
				{
					Map.Instance.UnitToMove.ValidateAttack(selectedHexagon.occupierUnit);
				}
				//if selected terrain only has an enemy building and not an unit, ValidateRequestToMove to building and can start occupation next turn
				else if ((selectedHexagon.OccupierBuilding != null) && (selectedHexagon.occupierUnit == null) && (selectedHexagon.OccupierBuilding.playerID != netId))
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

	[Command]
	public void FinishTurnCmd()
	{
		OnlineGameManager.Instance.PlayerFinishedTurn(this);
	}

	public override void OnStartClient()
	{
		if (!hasAuthority) { return; }
		base.OnStartClient();
		CmdRegisterPlayer(isServer);
	}

	[Command]
	public void CmdRegisterPlayer(bool isHost)
	{
		OnlineGameManager.Instance.RegisterPlayer(this, isHost);
		playerID = netId;
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
	public void CreateUnit(BuildingBase owner, string unitName)
	{
		GameObject temp = Instantiate(NetworkRoomManagerWOT.Instance.spawnPrefabs.Find(prefab => prefab.name == unitName), transform.position + UnitBase.positionOffsetOnHexagons, Quaternion.identity);
		NetworkServer.Spawn(temp, gameObject);
		occupiedHex.occupierUnit = temp.GetComponent<UnitBase>();
		temp.GetComponent<UnitBase>().occupiedHexagon = occupiedHex;
		OnlineGameManager.Instance.RegisterUnit(netId, temp.GetComponent<UnitBase>());
		temp.GetComponent<UnitBase>().playerID = netId;
		ToggleBuildingMenuRpc(owner.netIdentity.connectionToClient, false);
	}
}/*
namespace HayriCakir
{
	/*public struct Resources
	{
		public int woodCount;
		public int meatCount;
		public int currentPopulation;
		public int maxPopulation;
		
		public Resources(int woodCount, int meatCount, int currentPopulation, int maxPopulation)
		{
			this.woodCount = woodCount;
			this.meatCount = meatCount;
			this.currentPopulation = currentPopulation;
			this.maxPopulation = maxPopulation;
		}
	}
}
	*/