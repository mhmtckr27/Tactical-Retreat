using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPTownCenterUI : MonoBehaviour
{
	[SerializeField] public SPUnitCreationPanel unitCreationMenu;
	[SerializeField] private GameObject nextTurnButton;
	public SPTownCenter townCenter;



	private void Start()
	{

	}

	private void OnEnable()
	{
		nextTurnButton.SetActive(false);
	}

	private void OnDisable()
	{
		nextTurnButton.SetActive(true);
	}

	public void CreateUnitRequest(UnitProperties unitToCreate)
	{
		//townCenter.CreateUnit(townCenter, .name);
		unitCreationMenu.Init(townCenter, unitToCreate);
		unitCreationMenu.gameObject.SetActive(true);
	}

	public void OnClose()
	{
		townCenter.OnCloseTownCenterUI();
	}
}
