using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPTownCenterUI : SPBuildingUI
{
	[SerializeField] public SPUnitCreationPanel unitCreationMenu;
	[SerializeField] public SPBuildingCreationPanel buildingCreationMenu;
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
		townCenter.DeselectEverything();
		unitCreationMenu.Init(townCenter, unitProperties);
		unitCreationMenu.gameObject.SetActive(true);
	}
	public void CreateBuildingRequest(BuildingProperties buildingProperties)
	{
		townCenter.DeselectEverything();
		buildingCreationMenu.Init(townCenter, buildingProperties);
		buildingCreationMenu.gameObject.SetActive(true);
	}

	public void OnClose()
	{
		townCenter.OnCloseTownCenterUI();
	}
}
