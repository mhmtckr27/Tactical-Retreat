using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCenterUI : MonoBehaviour
{
	[SerializeField] private GameObject nextTurnButton;
	public TownCenter townCenter;

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
		townCenter.CreateUnitCmd(unitToCreate.name);
	}

	public void OnClose()
	{
		townCenter.OnCloseTownCenterUI();
	}
}
