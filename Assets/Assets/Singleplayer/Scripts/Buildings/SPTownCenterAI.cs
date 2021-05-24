using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPTownCenterAI : SPTownCenter
{

	#region Empty Overrides
	public override void SelectBuilding(SPBuildingBase building)
	{
	}

	public override void OnCloseTownCenterUI()
	{
	}

	public override void DeselectBuilding(SPBuildingBase building)
	{
	}

	public override void ToggleBuildingMenu(bool enable)
	{
	}
	protected override void OnEnable()
	{
	}

	protected override void OnDisable()
	{
	}

	public override void OnTerrainOccupiersChange(string key, int invoker)
	{
	}

	public override void ShowHideOccupier(MonoBehaviour occupier, bool isDiscovered)
	{
	}

	protected override bool IsPointerOverUIObject()
	{
		return false;
	}


	public override void ToggleSelectTerrain(int terrainType, bool enable)
	{
	}

	protected override void Play(Ray ray)
	{
	}

	protected override void EnableNextTurnButton(bool enable)
	{
	}

	protected override void SetAllCameraPositions(Vector3 position)
	{
	}

	#endregion

	#region Overrides
	protected override void Start()
	{
		OccupiedHex.OccupierBuilding = this;
		ExploreTerrains(SPMap.Instance.GetDistantHexagons(SPMap.Instance.mapDictionary["0_0_0"], SPMap.Instance.mapWidth), false);
		SPGameManager.Instance.AddDiscoveredTerrains(PlayerID, OccupiedHex.Key, 1);
		PluggableAI.TownCenter = this;
	}

	protected override void Update()
	{
		//TODO: startta invoke repeating ile yap, her framede kontrol yapmak zorunda degilsin.
		if (ValidatePlayRequest())
		{
			PluggableAI.Think();
		}
	}

	public override void ExploreTerrains(List<SPTerrainHexagon> distantNeighbours, bool isDiscovered)
	{
		foreach (SPTerrainHexagon hex in distantNeighbours)
		{
			hex.Explore(isDiscovered, true);
		}
	}

	public override void UpdateResourceCount(ResourceType resourceType, int count, int cost)
	{
		base.UpdateResourceCount(resourceType, count, cost);
	}

	public override void UpdateWoodCount(int count)
	{
		woodCount += count;
		if (SPMap.Instance.selectedHexagon != null)
		{
			SPMap.Instance.selectedHexagon.IsResourceCollected = true;
			SPMap.Instance.selectedHexagon.UpdateTerrainType();
		}
	}

	public override void UpdateMeatCount(int count)
	{
		meatCount += count;
		if (SPMap.Instance.selectedHexagon != null)
		{
			SPMap.Instance.selectedHexagon.IsResourceCollected = true;
			SPMap.Instance.selectedHexagon.UpdateTerrainType();
		}
	}

	public override void UpdateCurrentPopulation(int count)
	{
		currentPopulation += count;
	}

	protected override void UpdateMaxPopulation(int count)
	{
		maxPopulation += count;
	}

	public override void UpdateActionPoint(int count)
	{
		actionPoint += count;
	}

	protected override bool ValidatePlayRequest()
	{
		return IsAI && HasTurn && !PluggableAI.IsPlaying;
	}

	public override void SelectUnit(SPUnitBase unit)
	{
		base.SelectUnit(unit);
	}

	public override void DeselectUnit(SPUnitBase unit)
	{
		base.DeselectUnit(unit);
	}

	public override void SelectTerrain(SPTerrainHexagon terrainHexagon)
	{
		SPMap.Instance.selectedHexagon = terrainHexagon;
	}

	public override void DeselectTerrain()
	{
		SPMap.Instance.selectedHexagon = null;
	}

	public override void ClearSelectedHexagon()
	{
		base.ClearSelectedHexagon();
	}

	public override void SetHasTurn(bool newHasTurn)
	{
		HasTurn = newHasTurn;
		if (HasTurn)
		{
			int actionPointGain = CalculateActionPointGain();
			UpdateActionPoint(actionPointGain);
		}
	}

	public override int CalculateActionPointGain()
	{
		return base.CalculateActionPointGain();
	}

	public override void FinishTurn()
	{
		base.FinishTurn();
	}

	protected override void DeselectEverything()
	{
		base.DeselectEverything();
	}

	public override bool CreateUnit(string unitName)
	{
		if (!OccupiedHex.OccupierUnit)
		{
			GameObject temp = SPGameManager.Instance.spawnablePrefabs.Find(prefab => prefab.name == unitName);
			if (temp.GetComponent<SPUnitBase>().unitProperties.actionPointCostToCreate <= actionPoint)
			{
				return CreateUnit(this, temp);
			}
		}
		return false;
	}

	//TODO update
	public override bool CreateUnit(SPBuildingBase owner, GameObject unit)
	{
		return base.CreateUnit(owner, unit);
	}
	#endregion
}
