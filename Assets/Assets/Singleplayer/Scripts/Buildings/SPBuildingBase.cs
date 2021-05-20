using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPBuildingBase : MonoBehaviour
{
	[SerializeField] private GameObject canvasPrefab;
	[SerializeField] public BuildingType buildingType;

	public SPTerrainHexagon occupiedHex;
	public uint playerID;

	protected SPTownCenterUI buildingMenuUI;
	protected SPUIManager uiManager;
	protected bool menu_visible = false;
	private GameObject canvas;

	protected virtual void Awake()
	{
		canvas = Instantiate(canvasPrefab);
		uiManager = canvas.GetComponent<SPUIManager>();
	}

	public void SelectBuilding(SPBuildingBase building)
	{
		menu_visible = true;
		ToggleBuildingMenu(menu_visible);
	}

	public void OnCloseTownCenterUI()
	{
		DeselectBuilding(this);
	}

	public void DeselectBuilding(SPBuildingBase building)
	{
		menu_visible = false;
		ToggleBuildingMenu(menu_visible);
	}

	public void ToggleBuildingMenu(bool enable)
	{
		buildingMenuUI.gameObject.SetActive(enable);
	}
}