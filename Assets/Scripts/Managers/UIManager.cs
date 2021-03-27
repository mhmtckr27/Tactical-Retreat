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
	[SerializeField] private Text currentToMaxPopulationText;
	public TownCenterUI townCenterUI;

	private void Start()
	{
		townCenterUI.townCenter.onWoodCountChange += newWoodCount => woodCountText.text = newWoodCount.ToString();
		townCenterUI.townCenter.onMeatCountChange += newMeatCount => meatCountText.text = newMeatCount.ToString();
		townCenterUI.townCenter.onCurrentToMaxPopulationChange += (newCurrentPopulation, newMaxPopulation) => currentToMaxPopulationText.text = newCurrentPopulation.ToString() + "/" + newMaxPopulation;
	}

	public void OnNextTurnButton()
	{
		townCenterUI.townCenter.FinishTurnCmd();
	}

	public void EnableNexTurnButton(bool enable)
	{
		nextTurnButton.interactable = enable;
	}

	
}
