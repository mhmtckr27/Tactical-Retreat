using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

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
	

	protected override void Start()
	{
		base.Start();
		buildingMenuUI = uiManager.townCenterUI;
		buildingMenuUI.townCenter = this;
		transform.eulerAngles = new Vector3(0, -60, 0);
		inputManager = GetComponent<InputManager>();
	}

	private void Update()
	{
		if (!hasAuthority) { return; }

#if UNITY_EDITOR
		if (Input.GetMouseButtonUp(0))
		{
			ValidatePlayRequestCmd();
		}
#elif UNITY_ANDROID
		if (inputManager.HasValidTap())
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
		else if(Map.Instance.currentState == State.None)
		{

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
		foreach (BuildingBase building in OnlineGameManager.Instance.GetBuildings(netId))
		{
			if(building.buildingType == BuildingType.WoodcutterCottage)
			{
				UpdateWoodCount((building as WoodcutterCottage).CollectResource());
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
		ToggleBuildingMenuRpc(owner.netIdentity.connectionToClient);
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