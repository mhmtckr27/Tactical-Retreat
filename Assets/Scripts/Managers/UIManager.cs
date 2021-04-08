using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	[SerializeField] private Button nextTurnButton;
	[SerializeField] private Text woodCountText;
	[SerializeField] private Text meatCountText;
	[SerializeField] private Text actionPointText;
	[SerializeField] private Text currentToMaxPopulationText;
	public TownCenterUI townCenterUI;
	public TerrainHexagonUI terrainHexagonUI;

	private void OnEnable()
	{
		terrainHexagonUI.uiManager = this;
	}

	private void Start()
	{
		townCenterUI.townCenter.onWoodCountChange += newWoodCount => woodCountText.text = newWoodCount.ToString();
		townCenterUI.townCenter.onMeatCountChange += newMeatCount => meatCountText.text = newMeatCount.ToString();
		townCenterUI.townCenter.onActionPointChange += newActionPoint => actionPointText.text = newActionPoint.ToString();
		townCenterUI.townCenter.onCurrentToMaxPopulationChange += (newCurrentPopulation, newMaxPopulation) => currentToMaxPopulationText.text = newCurrentPopulation.ToString() + "/" + newMaxPopulation;
	}

	public void OnNextTurnButton()
	{
		townCenterUI.townCenter.FinishTurn();
	}

	public void EnableNexTurnButton(bool enable)
	{
		nextTurnButton.interactable = enable;
	}

	
}
