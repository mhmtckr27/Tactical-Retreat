using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
	[SerializeField] private Button nextTurnButton;
	[SerializeField] private Text woodCountText;
	[SerializeField] private Text meatCountText;
	[SerializeField] private Text actionPointText;
	[SerializeField] private Text currentToMaxPopulationText;
	[SerializeField] private GameObject settingsMenu;

	[SerializeField] [Scene] private string singleplayerScene;
	[SerializeField] [Scene] private string multiplayerScene;
	[SerializeField] private InputField networkAddress;
	[SerializeField] private InputField mapWidth;

	public TownCenterUI townCenterUI;
	public TerrainHexagonUI terrainHexagonUI;
	public UnitCreationPanel unitCreationUI;
	public BuildingCreationPanel buildingCreationUI;

	private void OnEnable()
	{
		if (terrainHexagonUI)
		{
			terrainHexagonUI.uiManager = this;
		}
	}

	private void Start()
	{
		if (townCenterUI)
		{
			townCenterUI.townCenter.OnWoodCountChange += newWoodCount => woodCountText.text = newWoodCount.ToString();
			townCenterUI.townCenter.OnMeatCountChange += newMeatCount => meatCountText.text = newMeatCount.ToString();
			townCenterUI.townCenter.OnActionPointChange += newActionPoint => actionPointText.text = newActionPoint.ToString();
			townCenterUI.townCenter.OnCurrentToMaxPopulationChange += (newCurrentPopulation, newMaxPopulation) => currentToMaxPopulationText.text = newCurrentPopulation.ToString() + "/" + newMaxPopulation;
		}
	}

	public void OnNextTurnButton()
	{
		townCenterUI.townCenter.FinishTurn();
	}

	public void EnableNexTurnButton(bool enable)
	{
		nextTurnButton.interactable = enable;
	}

	public void OnSettingsButton()
	{
		settingsMenu.SetActive(true);
	}

	public void OnCloseSettingsButton()
	{
		settingsMenu.SetActive(false);
	}

	public void OnExitButton()
	{
		// stop host if host mode
		if (NetworkServer.active && NetworkClient.isConnected)
		{
			//Debug.LogError("host");
			List<TownCenter> playerListTemp = OnlineGameManager.Instance.playerList;
			int playerCount = playerListTemp.Count;
			for(int i = 0; i < playerCount; i++)
			{
				playerListTemp[0].UnregisterPlayer();
			}
			NetworkManagerHUDWOT.manager.StopHost();
			OnLoadScene("Start");
			//OnlineGameManager.Instance.OnHostDisconnectedRpc();
		}
		// stop client if client-only
		else if (NetworkClient.isConnected)
		{
			//Debug.LogError("client");
			townCenterUI.townCenter.UnregisterPlayerCmd();
			NetworkManagerHUDWOT.manager.StopClient();
			OnLoadScene("Start");
		}
		// stop server if server-only
		else if (NetworkServer.active)
		{
			//Debug.LogError("server");
			NetworkManagerHUDWOT.manager.StopServer();
		}
		//OnLoadScene("Start");
	}

	public void OnQuitButton()
	{
		Application.Quit();
	}

	public void OnLoadScene(string sceneToLoad)
	{
		SceneManager.LoadScene(sceneToLoad);
	}

	public void OnSingleplayerScene()
	{
		SceneManager.LoadScene(singleplayerScene);
	}

	public void OnMultiplayerButton()
	{
		SceneManager.LoadScene(multiplayerScene);
	}

	public void OnMainMenuButton()
	{
		SceneManager.LoadScene(0);
	}

	public void OnHostButton()
	{
		if (!NetworkClient.active)
		{
			// Server + Client
			if (Application.platform != RuntimePlatform.WebGLPlayer)
			{
				if (mapWidth.text == "")
				{
					(NetworkManagerHUDWOT.manager as NetworkRoomManagerWOT).mapWidth = 6;
				}
				else
				{
					(NetworkManagerHUDWOT.manager as NetworkRoomManagerWOT).mapWidth = int.Parse(mapWidth.text);
				}
				NetworkManagerHUDWOT.manager.StartHost();
			}
		}
	}

	public void OnClientButton()
	{
		if (!NetworkClient.active)
		{
			NetworkManagerHUDWOT.manager.StartClient();
			NetworkManagerHUDWOT.manager.networkAddress = networkAddress.text;
		}
	}

	public void OnServerButton()
	{
		if (!NetworkClient.active)
		{
			NetworkManagerHUDWOT.manager.StartServer();
		}
	}
}
