using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPTownCenterUI : MonoBehaviour
{
	[SerializeField] private GameObject nextTurnButton;
	public SPTownCenter townCenter;

	private void OnEnable()
	{
		nextTurnButton.SetActive(false);
	}

	private void OnDisable()
	{
		nextTurnButton.SetActive(true);
	}

	public void CreateUnitRequest(GameObject unitToCreate)
	{
		townCenter.CreateUnit(unitToCreate.name);
	}

	public void OnClose()
	{
		townCenter.OnCloseTownCenterUI();
	}
}
