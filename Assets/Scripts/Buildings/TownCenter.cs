using Mirror;
using System;
using UnityEngine;
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
	[SyncVar] private int actionPoint;

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
			if(terrainHexagon != Map.Instance.selectedHexagon)
			{
				Map.Instance.selectedHexagon = terrainHexagon;
				TerrainHexagonSelectionRpc(-1, false);
				TerrainHexagonSelectionRpc((int)terrainHexagon.terrainType, true);
			}
			else
			{
				Map.Instance.selectedHexagon = null;
				TerrainHexagonSelectionRpc(-1, false);
			}
		}
	}

	[TargetRpc]
	public void TerrainHexagonSelectionRpc(int terrainType, bool enable)
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