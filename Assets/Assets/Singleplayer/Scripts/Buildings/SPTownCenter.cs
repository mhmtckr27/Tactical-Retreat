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

	[SerializeField] private GameObject canvasPrefab;
	[SerializeField] public int woodCount;
	[SerializeField] public int meatCount;
	[SerializeField] public int currentPopulation;
	[SerializeField] public int maxPopulation;
	[SerializeField] public int actionPoint;

	public event Action<int> OnWoodCountChange;
	public event Action<int> OnMeatCountChange;
	public event Action<int> OnActionPointChange;
	public event Action<int, int> OnCurrentToMaxPopulationChange;

	public PluggableAI PluggableAI { get; set; }
	public bool IsAI { get; set; }
	private GameObject canvas;

	protected virtual void Awake()
	{
		canvas = Instantiate(canvasPrefab);
		uiManager = canvas.GetComponent<SPUIManager>();
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
			OnTerrainOccupiersChange(OccupiedHex.Key, 1);
		}
		SPGameManager.Instance.AddDiscoveredTerrains(PlayerID, OccupiedHex.Key, 1);

		if (!IsAI)
		{
			Camera[] cams = Camera.allCameras;
			foreach (Camera cam in cams)
			{
				cam.GetComponent<CameraManager>().SPUpdateCameraSizes();
			}
		}

		StartCoroutine(InitResources());
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

	protected virtual void OnEnable()
	{
		if (!SPGameManager.Instance.enableMapVisibilityHack)
			SPTerrainHexagon.OnTerrainOccupiersChange += OnTerrainOccupiersChange;
	}

	protected virtual void OnDisable()
	{
		if (!SPGameManager.Instance.enableMapVisibilityHack)
			SPTerrainHexagon.OnTerrainOccupiersChange -= OnTerrainOccupiersChange;
	}

	//invoker: 0 means unit, 1 means building
	public virtual void OnTerrainOccupiersChange(string key, int invoker)
	{
		if (invoker == 0)
		{
			if ((SPMap.Instance.mapDictionary.ContainsKey(key)) && (SPMap.Instance.mapDictionary[key].OccupierUnit.playerID != PlayerID) && (SPGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(PlayerID)))
			{
				ShowHideOccupier(SPMap.Instance.mapDictionary[key].OccupierUnit, SPGameManager.Instance.PlayersToDiscoveredTerrains[PlayerID].Contains(SPMap.Instance.mapDictionary[key]));
				SPMap.Instance.mapDictionary[key].OccupierUnit.canvas.SetActive(SPGameManager.Instance.PlayersToDiscoveredTerrains[PlayerID].Contains(SPMap.Instance.mapDictionary[key]));
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
			if (Input.GetMouseButtonUp(0) )
			{
				//if(EventSystem.current.currentSelectedGameObject != null)
				//Debug.LogError(EventSystem.current.currentSelectedGameObject.name);
				if (!EventSystem.current.IsPointerOverGameObject())
				{
					if (ValidatePlayRequest())
					{
						Play(Camera.main.ScreenPointToRay(Input.mousePosition));
					}
				}
			}
		}
#elif UNITY_ANDROID
		if(SPMap.Instance.UnitToMove == null || !SPMap.Instance.UnitToMove.IsMoving)
		{
			if (inputManager.HasValidTap() && !IsPointerOverUIObject() && EventSystem.current.currentSelectedGameObject == null)
			{
				/*if(EventSystem.current.currentSelectedGameObject != null)
				Debug.LogError(EventSystem.current.currentSelectedGameObject.name);*/
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

	public virtual void CollectResource(Resource resource)
	{
		if (resource == null) { return; }
		if (resource.canBeCollected == false) { return; }
		if (resource.costToCollect > actionPoint) { return; }

		UpdateResourceCount(resource.resourceType, resource.resourceCount);
		UpdateActionPoint(-resource.costToCollect);
	}

	public void UpdateResourceCount(ResourceType resourceType, int resourceCount)
	{
		switch (resourceType)
		{
			case ResourceType.Wood:
				UpdateWoodCount(resourceCount);
				break;
			case ResourceType.Meat:
				UpdateMeatCount(resourceCount);
				break;
			case ResourceType.CurrentPopulation:
				UpdateCurrentPopulation(resourceCount);
				break;
			case ResourceType.MaxPopulation:
				UpdateMaxPopulation(resourceCount);
				break;
			case ResourceType.ActionPoint:
				UpdateActionPoint(resourceCount);
				break;
			default:
				break;
		}
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
				else if ((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit.playerID != PlayerID))
				{
					StartCoroutine(SPMap.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit));
				}
				//if selected terrain only has an enemy unit and not a building, attack to enemy unit
				else if ((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit.playerID != PlayerID))
				{
					StartCoroutine(SPMap.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit));
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
			if (!IsAI)
			{
				foreach(SPUnitBase friendlyUnit in SPGameManager.Instance.GetUnits(PlayerID))
				{
					friendlyUnit.occupiedHex.ToggleOutlineVisibility(2, false);
				}
			}
			//unit.occupiedHex.ToggleOutlineVisibility(2, true);
		}
	}

	public virtual void DeselectUnit(SPUnitBase unit)
	{
		unit.SetIsInMoveMode(false);
		if (!IsAI)
		{
			foreach (SPUnitBase friendlyUnit in SPGameManager.Instance.GetUnits(PlayerID))
			{
				friendlyUnit.occupiedHex.ToggleOutlineVisibility(2, true);
			}
		}
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

	public virtual void SelectBuilding(SPBuildingBase building)
	{
		menu_visible = true;
		ToggleBuildingMenu(menu_visible);
	}

	public virtual void DeselectBuilding(SPBuildingBase building)
	{
		menu_visible = false;
		ToggleBuildingMenu(menu_visible);
	}

	public virtual void ToggleSelectTerrain(int terrainType, bool enable)
	{
		uiManager.terrainHexagonUI.SetEnable(terrainType, enable);
	}

	public virtual void ClearSelectedHexagon()
	{
		SPMap.Instance.selectedHexagon = null;
	}

	public virtual void OnCloseTownCenterUI()
	{
		DeselectBuilding(this);
	}


	public virtual void SetHasTurn(bool newHasTurn)
	{
		HasTurn = newHasTurn;
		foreach(SPUnitBase unit in SPGameManager.Instance.GetUnits(PlayerID))
		{
			unit.remainingMovesThisTurn = unit.unitProperties.moveRange;
			unit.HasAttacked = false;
			if(unit.remainingMovesThisTurn > 0 && !IsAI)
			{
				unit.occupiedHex.ToggleOutlineVisibility(2, newHasTurn);
			}
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
		return SPGameManager.Instance.GetUnits(PlayerID).Count < 2 ? 2 : SPGameManager.Instance.GetUnits(PlayerID).Count;
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

	public virtual void DeselectEverything()
	{
		/*if (SPMap.Instance.UnitToMove != null && SPMap.Instance.UnitToMove.playerID == PlayerID)
		{
			SPMap.Instance.UnitToMove.DisableHexagonOutlines();
		}*/
		//Debug.Log(SPMap.Instance.UnitToMove);
		DeselectBuilding(this);
		DeselectTerrain();
		DeselectUnitCreationPanel();
		DeselectBuildingCreationPanel();
		if (SPMap.Instance.UnitToMove != null) { DeselectUnit(SPMap.Instance.UnitToMove); }
	}

	private void DeselectUnitCreationPanel()
	{
		if (uiManager != null)
		{
			uiManager.unitCreationUI.gameObject.SetActive(false);
		}
	}	
	
	private void DeselectBuildingCreationPanel()
	{
		if (uiManager != null)
		{
			uiManager.buildingCreationUI.gameObject.SetActive(false);
		}
	}

	protected virtual void SetAllCameraPositions(Vector3 position)
	{
		foreach (Camera cam in Camera.allCameras)
		{
			cam.transform.position = new Vector3(position.x, cam.transform.position.y, position.z);
		}
	}

	/*public virtual bool CreateUnit(string unitName)
	{
		return CreateUnit(this, temp);
		return false;
	}*/

	public virtual bool ValidateCreateUnit(SPUnitBase unitScript)
	{
		return	(unitScript != null) &&
				(OccupiedHex.OccupierUnit == null) &&
				(unitScript.unitProperties.woodCostToCreate <= woodCount) &&
				(unitScript.unitProperties.meatCostToCreate <= meatCount) &&
				(unitScript.unitProperties.populationCostToCreate <= maxPopulation - currentPopulation) &&
				(unitScript.unitProperties.actionPointCostToCreate <= actionPoint);
	}

	//TODO update
	public virtual bool CreateUnit(SPBuildingBase owner, string unitName)
	{
		GameObject unit = SPGameManager.Instance.spawnablePrefabs.Find(prefab => prefab.name == unitName);
		if(unit == null) { return false; }
		SPUnitBase unitScript = unit.GetComponent<SPUnitBase>();
		if (ValidateCreateUnit(unitScript) == false) { return false; }

		GameObject unitObject = Instantiate(unit, transform.position + UnitProperties.positionOffsetOnHexagons, Quaternion.identity);
		
		unitScript = unitObject.GetComponent<SPUnitBase>();
		unitScript.PlayerColor = PlayerColor;
		unitScript.occupiedHex = OccupiedHex;
		unitScript.playerID = PlayerID;
		OccupiedHex.OccupierUnit = unitScript;

		SPGameManager.Instance.RegisterUnit(PlayerID, unitScript);
		ToggleBuildingMenu(false);

		UpdateResourceCount(ResourceType.Wood, -unitScript.unitProperties.woodCostToCreate);
		UpdateResourceCount(ResourceType.Meat, -unitScript.unitProperties.meatCostToCreate);
		UpdateResourceCount(ResourceType.CurrentPopulation, unitScript.unitProperties.populationCostToCreate);
		UpdateResourceCount(ResourceType.ActionPoint, -unitScript.unitProperties.actionPointCostToCreate);

		return true;
	}

	public virtual bool CreateBuilding(SPBuildingBase owner, string buildingName)
	{
		GameObject building = SPGameManager.Instance.spawnablePrefabs.Find(prefab => prefab.name == buildingName);

		if (building == null) { return false; }

		SPBuildingBase buildingScript = building.GetComponent<SPBuildingBase>();

		if (ValidateCreateBuilding(buildingScript) == false) { return false; }

		GameObject buildingObject = Instantiate(building, transform.position + BuildingProperties.positionOffsetOnHexagons, Quaternion.identity);

		buildingScript = buildingObject.GetComponent<SPBuildingBase>();
		buildingScript.PlayerColor = PlayerColor;
		buildingScript.OccupiedHex = OccupiedHex;
		buildingScript.PlayerID = PlayerID;
		if(buildingScript.buildingProperties.buildingType != BuildingType.House)
		{
			OccupiedHex.OccupierBuilding = buildingScript;
		}
		else
		{
			UpdateResourceCount(ResourceType.MaxPopulation, 2);
		}
		SPGameManager.Instance.RegisterBuilding(PlayerID, buildingScript);
		ToggleBuildingMenu(false);
		UpdateResourceCount(ResourceType.Wood, -buildingScript.buildingProperties.woodCostToCreate);
		UpdateResourceCount(ResourceType.Meat, -buildingScript.buildingProperties.meatCostToCreate);
		UpdateResourceCount(ResourceType.CurrentPopulation, -buildingScript.buildingProperties.populationCostToCreate);
		UpdateResourceCount(ResourceType.ActionPoint, -buildingScript.buildingProperties.actionPointCostToCreate);
		
		return true;
	}

	public virtual bool ValidateCreateBuilding(/*SPTerrainHexagon buildOnTerrain, */SPBuildingBase buildingScript)
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
		return  (buildingScript != null) &&
				(buildingScript.buildingProperties.woodCostToCreate <= woodCount) &&
				(buildingScript.buildingProperties.meatCostToCreate <= meatCount) &&
				(buildingScript.buildingProperties.populationCostToCreate <= maxPopulation - currentPopulation) &&
				(buildingScript.buildingProperties.actionPointCostToCreate <= actionPoint);
	}
}
