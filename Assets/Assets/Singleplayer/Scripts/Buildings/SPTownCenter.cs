using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SPTownCenter : SPBuildingBase
{
	public bool HasTurn { get; set; }
	public bool IsConquered { get; set; }
	/*public bool canBeConquered;
	public SPTownCenter currentConqueror;*/
	private InputManager inputManager;

	[SerializeField] protected int woodCount;
	[SerializeField] protected int meatCount;
	protected int currentPopulation;
	protected int maxPopulation;
	public int actionPoint;

	public event Action<int> OnWoodCountChange;
	public event Action<int> OnMeatCountChange;
	public event Action<int> OnActionPointChange;
	public event Action<int, int> OnCurrentToMaxPopulationChange;

	private Color playerColor;
	public Color PlayerColor
	{
		get => playerColor;
		set
		{
			OnPlayerColorSet(playerColor, value);
			playerColor = value;
		}
	}

	public void OnPlayerColorSet(Color oldColor, Color newColor)
	{
		GetComponent<Renderer>().materials[1].color = newColor;
	}

	protected override void Start()
	{
		base.Start();
		buildingMenuUI = uiManager.townCenterUI;
		buildingMenuUI.townCenter = this; 
		inputManager = GetComponent<InputManager>();
		OccupiedHex.OccupierBuilding = this;
		SetAllCameraPositions(new Vector3(transform.position.x - 5, 0, transform.position.z + 0.75f));
		if (!SPGameManager.Instance.enableMapVisibilityHack)
		{
			ExploreTerrains(SPMap.Instance.GetDistantHexagons(SPMap.Instance.mapDictionary["0_0_0"], SPMap.Instance.mapWidth), false);
		}
		//OnTerrainOccupiersChange(OccupiedHex.Key, 1);
		SPGameManager.Instance.AddDiscoveredTerrains(PlayerID, OccupiedHex.Key, 1);
	}

	protected virtual void OnEnable()
	{
		//SPTerrainHexagon.OnTerrainOccupiersChange += OnTerrainOccupiersChange;
	}

	protected virtual void OnDisable()
	{
		//SPTerrainHexagon.OnTerrainOccupiersChange -= OnTerrainOccupiersChange;
	}

	//invoker: 0 means unit, 1 means building
	public virtual void OnTerrainOccupiersChange(string key, int invoker)
	{
		if (invoker == 0)
		{
			if ((SPMap.Instance.mapDictionary.ContainsKey(key)) && (SPMap.Instance.mapDictionary[key].OccupierUnit.playerID != PlayerID) && (SPGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(PlayerID)))
			{
				ShowHideOccupier(SPMap.Instance.mapDictionary[key].OccupierUnit, SPGameManager.Instance.PlayersToDiscoveredTerrains[PlayerID].Contains(SPMap.Instance.mapDictionary[key]));
			}
		}
		else
		{
			if ((SPMap.Instance.mapDictionary.ContainsKey(key)) && (SPMap.Instance.mapDictionary[key].OccupierBuilding.PlayerID != PlayerID) && (SPGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(PlayerID)))
			{
				ShowHideOccupier(SPMap.Instance.mapDictionary[key].OccupierBuilding, SPGameManager.Instance.PlayersToDiscoveredTerrains[PlayerID].Contains(SPMap.Instance.mapDictionary[key]));
			}
		}
	}

	public virtual void ShowHideOccupier(MonoBehaviour occupier, bool isDiscovered)
	{
		//occupier.gameObject.SetActive(isDiscovered);
		foreach(MeshRenderer mesh in occupier.GetComponentsInChildren<MeshRenderer>())
		{
			mesh.enabled = isDiscovered;
		}
	}

	public virtual void ExploreTerrains(List<SPTerrainHexagon> distantNeighbours, bool isDiscovered)
	{
		foreach (SPTerrainHexagon hex in distantNeighbours)
		{
			hex.Explore(isDiscovered, false);
		}
	}

	protected virtual void Update()
	{
#if UNITY_EDITOR
		if(SPMap.Instance.UnitToMove == null || !SPMap.Instance.UnitToMove.IsMoving)
		{
			if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
			{
				if (ValidatePlayRequest())
				{
					Play(Camera.main.ScreenPointToRay(Input.mousePosition));
				}
			}
		}
#elif UNITY_ANDROID
		if(SPMap.Instance.UnitToMove == null || !SPMap.Instance.UnitToMove.IsMoving)
		{
			if (inputManager.HasValidTap() && !IsPointerOverUIObject())
			{
				if (ValidatePlayRequest())
				{
					Play(Camera.main.ScreenPointToRay(Input.mousePosition));
				}
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
	protected virtual bool IsPointerOverUIObject()
	{
		PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
		eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
		List<RaycastResult> results = new List<RaycastResult>();
		EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
		return results.Count > 0;
	}

	public virtual void UpdateResourceCount(Resource resource)
	{
		if (resource.costToCollect > actionPoint) { return; }

		switch (resource.resourceType)
		{
			case ResourceType.Wood:
				UpdateWoodCount(resource.resourceCount);
				break;
			case ResourceType.Meat:
				UpdateMeatCount(resource.resourceCount);
				break;
			default:
				break;
		}
		UpdateActionPoint(-resource.costToCollect);
	}

	public virtual void UpdateWoodCount(int count)
	{
		woodCount += count;
		if(OnWoodCountChange != null)
		{
			OnWoodCountChange.Invoke(woodCount);
		}
		if (SPMap.Instance.selectedHexagon != null)
		{
			SPMap.Instance.selectedHexagon.IsResourceCollected = true;
			SPMap.Instance.selectedHexagon.UpdateTerrainType();
		}
	}

	public virtual void UpdateMeatCount(int count)
	{
		meatCount += count;
		if(OnMeatCountChange != null)
		{
			OnMeatCountChange.Invoke(meatCount);
		}
		if (SPMap.Instance.selectedHexagon != null)
		{
			SPMap.Instance.selectedHexagon.IsResourceCollected = true;
			SPMap.Instance.selectedHexagon.UpdateTerrainType();
		}
	}

	public virtual void UpdateCurrentPopulation(int count)
	{
		currentPopulation += count;
		if (OnCurrentToMaxPopulationChange != null)
		{
			OnCurrentToMaxPopulationChange.Invoke(currentPopulation, maxPopulation);
		}
	}

	protected virtual void UpdateMaxPopulation(int count)
	{
		maxPopulation += count;
		if (OnCurrentToMaxPopulationChange != null)
		{
			OnCurrentToMaxPopulationChange.Invoke(currentPopulation, maxPopulation);
		}
	}

	public virtual void UpdateActionPoint(int count)
	{
		actionPoint += count;
		if (OnActionPointChange != null)
		{
			OnActionPointChange.Invoke(actionPoint);
		}
	}

	protected virtual bool ValidatePlayRequest()
	{
		return HasTurn;
	}

	//TODO update
	protected virtual void Play(Ray ray)
	{
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			SPTerrainHexagon selectedHexagon = hit.collider.GetComponent<SPTerrainHexagon>();
			if (SPGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(PlayerID) && !SPGameManager.Instance.PlayersToDiscoveredTerrains[PlayerID].Contains(selectedHexagon))
			{
				DeselectEverything();
				return;
			}
			//if there is no selected unit before click
			if (SPMap.Instance.UnitToMove == null)
			{
				//if selected terrain is empty, show terrain info
				if ((selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding == null))
				{
					if (SPMap.Instance.selectedHexagon != selectedHexagon)
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
				else if ((selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierUnit.playerID == PlayerID))
				{
					if (SPMap.Instance.selectedHexagon != selectedHexagon)
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
				else if ((selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierBuilding.PlayerID == PlayerID))
				{
					if (SPMap.Instance.selectedHexagon != null)
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
				else if ((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit.playerID == PlayerID) && (selectedHexagon.OccupierBuilding.PlayerID == PlayerID))
				{
					if (SPMap.Instance.selectedHexagon != null)
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
					if (SPMap.Instance.UnitToMove.ValidateRequestToMove(selectedHexagon) == false)
					{
						DeselectUnit(SPMap.Instance.UnitToMove);
						SelectTerrain(selectedHexagon);
					}
				}
				//if selected terrain only has an occupier unit and not a building
				else if ((selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierUnit.playerID == PlayerID))
				{
					//currently selected and previously selected units are same, show terrain info
					if (SPMap.Instance.UnitToMove == selectedHexagon.OccupierUnit)
					{
						DeselectUnit(SPMap.Instance.UnitToMove);
						SelectTerrain(selectedHexagon);
					}
					//select lastly selected unit
					else
					{
						DeselectUnit(SPMap.Instance.UnitToMove);
						SelectUnit(selectedHexagon.OccupierUnit);
					}
				}
				//if selected terrain only has an occupier building and not an unit, ValidateRequestToMove building
				else if ((selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierBuilding.PlayerID == PlayerID))
				{
					if (SPMap.Instance.UnitToMove.ValidateRequestToMove(selectedHexagon) == false)
					{
						DeselectUnit(SPMap.Instance.UnitToMove);
						SelectBuilding(selectedHexagon.OccupierBuilding);
					}
				}
				//if selected terrain has both friendly unit and friendly building, select unit on town
				else if ((selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierUnit.playerID == PlayerID) && (selectedHexagon.OccupierBuilding.PlayerID == PlayerID))
				{
					if (SPMap.Instance.UnitToMove == selectedHexagon.OccupierUnit)
					{
						DeselectUnit(SPMap.Instance.UnitToMove);
					}
					else
					{
						DeselectUnit(SPMap.Instance.UnitToMove);
						SelectUnit(selectedHexagon.OccupierUnit);
					}
				}
				//if selected terrain has both enemy unit and enemy building, attack to enemy unit
				else if ((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit.playerID != PlayerID) && (selectedHexagon.OccupierBuilding.PlayerID != PlayerID))
				{
					SPMap.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit);
				}
				//if selected terrain only has an enemy unit and not a building, attack to enemy unit
				else if ((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit.playerID != PlayerID))
				{
					SPMap.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit);
				}
				//if selected terrain only has an enemy building and not an unit, ValidateRequestToMove to building and can start occupation next turn
				else if ((selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding.PlayerID != PlayerID))
				{
					if (SPMap.Instance.UnitToMove.ValidateRequestToMove(selectedHexagon) == false)
					{
						DeselectUnit(SPMap.Instance.UnitToMove);
					}
					else
					{
						//can start occupation process here because movement was successful
					}
				}
			}
		}
	}

	public virtual void SelectUnit(SPUnitBase unit)
	{
		if (unit.CanMove())
		{
			unit.SetIsInMoveMode(true);
		}
	}

	public virtual void DeselectUnit(SPUnitBase unit)
	{
		unit.SetIsInMoveMode(false);
	}

	public virtual void SelectTerrain(SPTerrainHexagon terrainHexagon)
	{
		SPMap.Instance.selectedHexagon = terrainHexagon;
		ToggleSelectTerrain((int)terrainHexagon.terrainType, true);
	}
	public virtual void DeselectTerrain()
	{
		SPMap.Instance.selectedHexagon = null;
		ToggleSelectTerrain(-1, false);
	}

	public virtual void ToggleSelectTerrain(int terrainType, bool enable)
	{
		uiManager.terrainHexagonUI.SetEnable(terrainType, enable);
	}

	public virtual void ClearSelectedHexagon()
	{
		SPMap.Instance.selectedHexagon = null;
	}

	public virtual void SetHasTurn(bool newHasTurn)
	{
		HasTurn = newHasTurn;
		foreach(SPUnitBase unit in SPGameManager.Instance.GetUnits(PlayerID))
		{
			unit.remainingMovesThisTurn = unit.unitProperties.moveRange;
			unit.HasAttacked = false;
		}
		EnableNextTurnButton(newHasTurn);
		if (HasTurn)
		{
			int actionPointGain = CalculateActionPointGain();
			UpdateActionPoint(actionPointGain);
		}
	}

	public virtual int CalculateActionPointGain()
	{
		//TODO calculate using unit count and many other things
		return 3;
	}

	protected virtual void EnableNextTurnButton(bool enable)
	{
		uiManager.EnableNexTurnButton(enable);
	}

	//TODO update
	public virtual void FinishTurn()
	{
		if (!HasTurn) { return; }
		DeselectEverything();
		SPGameManager.Instance.PlayerFinishedTurn(this);
	}

	protected virtual void DeselectEverything()
	{
		DeselectBuilding(this);
		DeselectTerrain();
		if (SPMap.Instance.UnitToMove != null) { DeselectUnit(SPMap.Instance.UnitToMove); }
	}

	protected virtual void SetAllCameraPositions(Vector3 position)
	{
		foreach (Camera cam in Camera.allCameras)
		{
			cam.transform.position = new Vector3(position.x, cam.transform.position.y, position.z);
		}
	}

	public virtual bool CreateUnit(string unitName)
	{
		if (!OccupiedHex.OccupierUnit)
		{
			GameObject temp = SPGameManager.Instance.spawnablePrefabs.Find(prefab => prefab.name == unitName);
			if(temp.GetComponent<SPUnitBase>().unitProperties.actionPointCostToCreate <= actionPoint)
			{
				return CreateUnit(this, temp);
			}
			else
			{
				//visual feedback that says sth like: you dont have enough action point,
				//or just lock the create button.
			}
		}
		return false;
	}

	//TODO update
	public virtual bool CreateUnit(SPBuildingBase owner, GameObject unit)
	{
		GameObject unitObject = Instantiate(unit, transform.position + UnitProperties.positionOffsetOnHexagons, Quaternion.identity);
		SPUnitBase unitScript = unitObject.GetComponent<SPUnitBase>();
		unitScript.PlayerColor = PlayerColor;
		unitScript.occupiedHex = OccupiedHex;
		SPGameManager.Instance.RegisterUnit(PlayerID, unitScript);
		unitScript.playerID = PlayerID;
		OccupiedHex.OccupierUnit = unitScript;
		ToggleBuildingMenu(false);
		actionPoint -= unitScript.unitProperties.actionPointCostToCreate;
		return true;
	}
}