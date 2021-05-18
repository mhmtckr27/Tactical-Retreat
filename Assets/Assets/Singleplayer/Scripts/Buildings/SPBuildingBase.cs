using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPBuildingBase : MonoBehaviour
{
	[SerializeField] private GameObject canvasPrefab;
	[SerializeField] public BuildingType buildingType;

	public TerrainHexagon occupiedHex;
	public uint playerID;

	protected TownCenterUI buildingMenuUI;
	protected UIManager uiManager;
	protected bool menu_visible = false;
	private GameObject canvas;

	protected virtual void Start()
	{
		canvas = Instantiate(canvasPrefab);
		uiManager = canvas.GetComponent<UIManager>();
	}

	public void SelectBuilding(BuildingBase building)
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