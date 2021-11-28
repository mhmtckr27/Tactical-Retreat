using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerInfoManager : MonoBehaviour
{
	[SerializeField] public GameObject removeButton;
	[SerializeField] public Text readyText;
	[SerializeField] public Text playerNameText;

	private void Awake()
	{
		UIManager uiManager = FindObjectOfType<UIManager>();
		uiManager.removeButton = removeButton;
		uiManager.readyText = readyText;
		uiManager.playerNameText = playerNameText;
	}
}
