using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SPTownCenter : SPBuildingBase
{
	public bool hasTurn;
	public bool isConquered = false;
	private InputManager inputManager;

	[SerializeField] private int woodCount;
	[SerializeField] private int meatCount;
	private int currentPopulation;
	private int maxPopulation;
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

	protected void Start()
	{
		buildingMenuUI = uiManager.townCenterUI;
		buildingMenuUI.townCenter = this;
		inputManager = GetComponent<InputManager>();
		occupiedHex.OccupierBuilding = this;
		OnTerrainOccupiersChange(occupiedHex.Key, 1);
		SetAllCameraPositions(new Vector3(transform.position.x - 5, 0, transform.position.z + 0.75f));
		ExploreTerrains(SPMap.Instance.GetDistantHexagons(SPMap.Instance.mapDictionary["0_0_0"], SPMap.Instance.mapWidth), false);
		SPGameManager.Instance.AddDiscoveredTerrains(playerID, occupiedHex.Key, 1);
	}

	public void Init()
	{
		buildingMenuUI = uiManager.townCenterUI;
		buildingMenuUI.townCenter = this;
		inputManager = GetComponent<InputManager>();
		occupiedHex.OccupierBuilding = this;
		OnTerrainOccupiersChange(occupiedHex.Key, 1);
		SetAllCameraPositions(new Vector3(transform.position.x - 5, 0, transform.position.z + 0.75f));
		ExploreTerrains(SPMap.Instance.GetDistantHexagons(SPMap.Instance.mapDictionary["0_0_0"], SPMap.Instance.mapWidth), false);
		SPGameManager.Instance.AddDiscoveredTerrains(playerID, occupiedHex.Key, 1);
	}

	private void OnEnable()
	{
		SPTerrainHexagon.OnTerrainOccupiersChange += OnTerrainOccupiersChange;
	}

	private void OnDisable()
	{
		SPTerrainHexagon.OnTerrainOccupiersChange -= OnTerrainOccupiersChange;
	}

	//invoker: 0 means unit, 1 means building
	public void OnTerrainOccupiersChange(string key, int invoker)
	{
		if (invoker == 0)
		{
			if ((SPMap.Instance.mapDictionary.ContainsKey(key)) && (SPMap.Instance.mapDictionary[key].OccupierUnit.playerID != playerID) && (SPGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(playerID)))
			{
				ShowHideOccupier(SPMap.Instance.mapDictionary[key].OccupierUnit, SPGameManager.Instance.PlayersToDiscoveredTerrains[playerID].Contains(key));
			}
		}
		else
		{
			if ((SPMap.Instance.mapDictionary.ContainsKey(key)) && (SPMap.Instance.mapDictionary[key].OccupierBuilding.playerID != playerID) && (SPGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(playerID)))
			{
				ShowHideOccupier(SPMap.Instance.mapDictionary[key].OccupierBuilding, SPGameManager.Instance.PlayersToDiscoveredTerrains[playerID].Contains(key));
			}
		}
	}

	public void ShowHideOccupier(MonoBehaviour occupier, bool isDiscovered)
	{
		occupier.gameObject.SetActive(isDiscovered);
	}

	public void ExploreTerrains(List<SPTerrainHexagon> distantNeighbours, bool isDiscovered)
	{
		foreach (SPTerrainHexagon hex in distantNeighbours)
		{
			hex.IsExplored = isDiscovered;
		}
	}

	private void Update()
	{

#if UNITY_EDITOR
		if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
		{
			ValidatePlayRequest();
		}
#elif UNITY_ANDROID
		if (inputManager.HasValidTap() && !IsPointerOverUIObject())
		{
			ValidatePlayRequest();
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

	public void UpdateResourceCount(ResourceType resourceType, int count, int cost)
	{
		if (cost > actionPoint) { return; }
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

	public void UpdateWoodCount(int count)
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

	public void UpdateMeatCount(int count)
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

	public void UpdateCurrentPopulation(int count)
	{
		currentPopulation += count;
		if (OnCurrentToMaxPopulationChange != null)
		{
			OnCurrentToMaxPopulationChange.Invoke(currentPopulation, maxPopulation);
		}
	}

	private void UpdateMaxPopulation(int count)
	{
		maxPopulation += count;
		if (OnCurrentToMaxPopulationChange != null)
		{
			OnCurrentToMaxPopulationChange.Invoke(currentPopulation, maxPopulation);
		}
	}

	public void UpdateActionPoint(int count)
	{
		actionPoint += count;
		if (OnActionPointChange != null)
		{
			OnActionPointChange.Invoke(actionPoint);
		}
	}

	protected void ValidatePlayRequest()
	{
		if (!hasTurn) { return; }
		Play(Camera.main.ScreenPointToRay(Input.mousePosition));
	}

	//TODO update
	protected void Play(Ray ray)
	{
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit))
		{
			SPTerrainHexagon selectedHexagon = hit.collider.GetComponent<SPTerrainHexagon>();
			if (SPGameManager.Instance.PlayersToDiscoveredTerrains.ContainsKey(playerID) && !SPGameManager.Instance.PlayersToDiscoveredTerrains[playerID].Contains(selectedHexagon.Key))
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
				else if ((selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierUnit.playerID == playerID))
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
				else if ((selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierBuilding.playerID == playerID))
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
				else if ((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit.playerID == playerID) && (selectedHexagon.OccupierBuilding.playerID == playerID))
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
				else if ((selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierUnit.playerID == playerID))
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
				else if ((selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierBuilding.playerID == playerID))
				{
					if (SPMap.Instance.UnitToMove.ValidateRequestToMove(selectedHexagon) == false)
					{
						DeselectUnit(SPMap.Instance.UnitToMove);
						SelectBuilding(selectedHexagon.OccupierBuilding);
					}
				}
				//if selected terrain has both friendly unit and friendly building, select unit on town
				else if ((selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierUnit.playerID == playerID) && (selectedHexagon.OccupierBuilding.playerID == playerID))
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
				else if ((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit.playerID != playerID) && (selectedHexagon.OccupierBuilding.playerID != playerID))
				{
					SPMap.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit);
				}
				//if selected terrain only has an enemy unit and not a building, attack to enemy unit
				else if ((selectedHexagon.OccupierUnit != null) && (selectedHexagon.OccupierBuilding == null) && (selectedHexagon.OccupierUnit.playerID != playerID))
				{
					SPMap.Instance.UnitToMove.ValidateAttack(selectedHexagon.OccupierUnit);
				}
				//if selected terrain only has an enemy building and not an unit, ValidateRequestToMove to building and can start occupation next turn
				else if ((selectedHexagon.OccupierBuilding != null) && (selectedHexagon.OccupierUnit == null) && (selectedHexagon.OccupierBuilding.playerID != playerID))
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

	public void SelectUnit(SPUnitBase unit)
	{
		if (unit.CanMove())
		{
			unit.SetIsInMoveMode(true);
		}
	}

	public void DeselectUnit(SPUnitBase unit)
	{
		unit.SetIsInMoveMode(false);
	}

	public void SelectTerrain(SPTerrainHexagon terrainHexagon)
	{
		SPMap.Instance.selectedHexagon = terrainHexagon;
		ToggleSelectTerrain((int)terrainHexagon.terrainType, true);
	}
	public void DeselectTerrain()
	{
		SPMap.Instance.selectedHexagon = null;
		ToggleSelectTerrain(-1, false);
	}

	public void ToggleSelectTerrain(int terrainType, bool enable)
	{
		uiManager.terrainHexagonUI.SetEnable(terrainType, enable);
	}

	public void ClearSelectedHexagon()
	{
		SPMap.Instance.selectedHexagon = null;
	}

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

	public int CalculateActionPointGain()
	{
		//TODO calculate using unit count and many other things
		return 3;
	}

	private void EnableNextTurnButton(bool enable)
	{
		uiManager.EnableNexTurnButton(enable);
	}

	//TODO update
	public void FinishTurn()
	{
		if (!hasTurn) { return; }
		DeselectEverything();
		SPGameManager.Instance.PlayerFinishedTurn(this);
	}

	private void DeselectEverything()
	{
		DeselectBuilding(this);
		DeselectTerrain();
		if (SPMap.Instance.UnitToMove != null) { DeselectUnit(SPMap.Instance.UnitToMove); }
	}

	private void SetAllCameraPositions(Vector3 position)
	{
		foreach (Camera cam in Camera.allCameras)
		{
			cam.transform.position = new Vector3(position.x, cam.transform.position.y, position.z);
		}
	}

	public void CreateUnit(string unitName)
	{
		if (!occupiedHex.OccupierUnit)
		{
			CreateUnit(this, unitName);
		}
	}

	//TODO update
	public void CreateUnit(SPBuildingBase owner, string unitName)
	{
		GameObject temp = Instantiate(SPGameManager.Instance.spawnablePrefabs.Find(prefab => prefab.name == unitName), transform.position + SPUnitBase.positionOffsetOnHexagons, Quaternion.identity);
		temp.GetComponent<SPUnitBase>().PlayerColor = PlayerColor;
		temp.GetComponent<SPUnitBase>().occupiedHex = occupiedHex;
		SPGameManager.Instance.RegisterUnit(playerID, temp.GetComponent<SPUnitBase>());
		temp.GetComponent<SPUnitBase>().playerID = playerID;
		occupiedHex.OccupierUnit = temp.GetComponent<SPUnitBase>();
		ToggleBuildingMenu(false);
	}
}