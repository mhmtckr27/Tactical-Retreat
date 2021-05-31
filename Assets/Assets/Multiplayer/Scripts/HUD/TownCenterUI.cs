using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCenterUI : BuildingUI
{
	[SerializeField] public UnitCreationPanel unitCreationMenu;
	[SerializeField] public BuildingCreationPanel buildingCreationMenu;
	[SerializeField] private GameObject nextTurnButton;

	private void OnEnable()
	{
		nextTurnButton.SetActive(false);
	}

	private void OnDisable()
	{
		nextTurnButton.SetActive(true);
	}

	public void CreateUnitRequest(UnitProperties unitProperties)
	{
		townCenter.DeselectEverythingCmd();
		unitCreationMenu.Init(townCenter, unitProperties);
		unitCreationMenu.gameObject.SetActive(true);
	}
	public void CreateBuildingRequest(BuildingProperties buildingProperties)
	{
		townCenter.DeselectEverythingCmd();
		buildingCreationMenu.Init(townCenter, buildingProperties);
		buildingCreationMenu.gameObject.SetActive(true);
	}
	public void OnClose()
	{
		townCenter.OnCloseTownCenterUI();
	}
}
