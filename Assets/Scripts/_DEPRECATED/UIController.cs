using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
	public Button nextTurnButton;
	public TownCenterUI townCenterUI;
	public void OnNextTurnButton()
	{
		townCenterUI.townCenter.FinishTurnCmd();
	}

	public void EnableNexTurnButton(bool enable)
	{
		nextTurnButton.interactable = enable;
	}
}
