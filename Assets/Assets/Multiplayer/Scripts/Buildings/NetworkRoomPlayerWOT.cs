using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkRoomPlayerWOT : NetworkRoomPlayer
{
	UIManager canvasUI;
	LobbyPlayerInfoManager lobbyPlayerInfoPanel;
	[SyncVar] public bool isPlayerSpawned;

	private void OnEnable()
	{
		canvasUI = FindObjectOfType<UIManager>();
		canvasUI.readyButton.onClick.AddListener(OnReadyButton);
		lobbyPlayerInfoPanel = Instantiate(canvasUI.lobbyPlayerInfoPrefab, canvasUI.playersContent.transform).GetComponent<LobbyPlayerInfoManager>();
		canvasUI.removeButton.GetComponent<Button>().onClick.AddListener(OnRemoveButton);
	}
	
	public override void OnDisable()
	{
		base.OnDisable();
		if (NetworkClient.active && NetworkManager.singleton is NetworkRoomManager room)
		{
			if (lobbyPlayerInfoPanel)
			{
				Destroy(lobbyPlayerInfoPanel.gameObject);
			}
		}
	}

	public override void OnGUI()
	{
		NetworkRoomManager room = NetworkManagerHUDWOT.manager as NetworkRoomManager;
		if (room)
		{
			if (!NetworkRoomManagerWOT.IsSceneActive(room.RoomScene))
			{
				return;
			}

			DrawPlayerReadyState();
			DrawPlayerReadyButton();
		}
	}
	
	public override void DrawPlayerReadyState()
	{
		lobbyPlayerInfoPanel.playerNameText.text = "Player " + (index + 1);

		if (readyToBegin)
		{
			lobbyPlayerInfoPanel.readyText.text = "Ready";
		}
		else
		{
			lobbyPlayerInfoPanel.readyText.text = "Not Ready";
		}

		if((isServer && index > 0) || isServerOnly)
		{
			lobbyPlayerInfoPanel.removeButton.SetActive(true);
		}
	}

	public override void DrawPlayerReadyButton()
	{
		if(!lobbyPlayerInfoPanel) { return; }
		if(NetworkClient.active && isLocalPlayer)
		{
			if (readyToBegin)
			{
				canvasUI.readyButton.GetComponentInChildren<Text>().text = "Cancel";
			}
			else
			{
				canvasUI.readyButton.GetComponentInChildren<Text>().text = "Ready";
			}
		}
	}

	public void OnReadyButton()
	{
		if(NetworkClient.active && isLocalPlayer)
		{
			if (readyToBegin)
			{
				CmdChangeReadyState(false);
			}
			else
			{
				CmdChangeReadyState(true);
			}
		}
	}

	public void OnRemoveButton()
	{
		if ((isServer && index > 0) || isServerOnly)
		{
			GetComponent<NetworkIdentity>().connectionToClient.Disconnect();
		}
	}
}
