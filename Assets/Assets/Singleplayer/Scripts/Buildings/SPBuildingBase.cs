using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPBuildingBase : MonoBehaviour
{
	[SerializeField] private GameObject canvasPrefab;
	[SerializeField] public BuildingType buildingType;

	public SPTerrainHexagon OccupiedHex { get; set; }
	public uint PlayerID { get; set; }

	protected SPTownCenterUI buildingMenuUI;
	protected SPUIManager uiManager;
	protected bool menu_visible = false;
	private GameObject canvas;

	public PluggableAI PluggableAI { get; set; }

	public bool IsAI { get; set; }

	protected virtual void Start()
	{
		canvas = Instantiate(canvasPrefab);
		uiManager = canvas.GetComponent<SPUIManager>();
	}

	public virtual void SelectBuilding(SPBuildingBase building)
	{
		menu_visible = true;
		ToggleBuildingMenu(menu_visible);
	}

	public virtual void OnCloseTownCenterUI()
	{
		DeselectBuilding(this);
	}

	public virtual void DeselectBuilding(SPBuildingBase building)
	{
		menu_visible = false;
		ToggleBuildingMenu(menu_visible);
	}

	public virtual void ToggleBuildingMenu(bool enable)
	{
		buildingMenuUI.gameObject.SetActive(enable);
	}
}