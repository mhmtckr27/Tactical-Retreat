using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
	public string playerName;
	[SerializeField] private Button nextTurnButton;
	[SerializeField] private Text woodCountText;
	[SerializeField] private Text meatCountText;
	[SerializeField] private Text actionPointText;
	[SerializeField] private Text currentToMaxPopulationText;
	[SerializeField] private GameObject settingsMenu;

	[SerializeField] [Scene] private string singleplayerScene;
	[SerializeField] [Scene] private string roomScene;
	[SerializeField] private Toggle hostSwitchToggle;
	[SerializeField] private InputField networkAddress;
	[SerializeField] private InputField mapWidth;
	[SerializeField] private Text debugText;
	[SerializeField] private GameObject debugPanel;
	[SerializeField] private GameObject fieldsPanel;
	[SerializeField] public Button readyButton;
	[SerializeField] public GameObject disconnectButton;
	[SerializeField] public GameObject lobbyPlayerInfoPrefab;
	[SerializeField] public GameObject playersContent;
	[SerializeField] public InputField playerNameInputField;

	[HideInInspector] public Text readyText;
	[HideInInspector] public Text playerNameText;
	[HideInInspector] public GameObject removeButton;

	public TownCenterUI townCenterUI;
	public TerrainHexagonUI terrainHexagonUI;
	public UnitCreationPanel unitCreationUI;
	public BuildingCreationPanel buildingCreationUI;

	private static UIManager instance;
	public static UIManager Instance { get; }
	
	private void OnEnable()
	{
		if (terrainHexagonUI)
		{
			terrainHexagonUI.uiManager = this; 
		}
	}

	private void Awake()
	{
		if(instance == null)
		{
			instance = this;
		}
		else if(instance != this)
		{
			Destroy(gameObject);
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
		if(sceneToLoad == "ROOOOOOOOOOOMMMMM")
		{
			(NetworkManagerHUDWOT.manager as NetworkRoomManagerWOT).SetPlayerName(playerNameInputField.text);
			if (hostSwitchToggle.isOn)
			{
				OnHostButton();
			}
			else
			{
				OnClientButton();
			}
		}
		else
		{
			SceneManager.LoadScene(sceneToLoad);
		}
	}

	public void OnSingleplayerScene()
	{
		SceneManager.LoadScene(singleplayerScene);
	}

	public void OnRoomButton()
	{
		SceneManager.LoadScene(roomScene);
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
			if(networkAddress.text == "")
			{
				NetworkManagerHUDWOT.manager.networkAddress = "localhost";
			}
			else
			{
				NetworkManagerHUDWOT.manager.networkAddress = networkAddress.text;
			}
		}
	}

	public void CancelClientConnectionAttempt()
	{
		if (!NetworkClient.isConnected && !NetworkServer.active)
		{
			if (NetworkClient.active)
			{
				NetworkManagerHUDWOT.manager.StopClient();
				ShowDebugPanel(false);
				ShowConnectionPanel(true);
			}
		}
	}

	public void OnServerButton()
	{
		if (!NetworkClient.active)
		{
			NetworkManagerHUDWOT.manager.StartServer();
		}
	}

	public void OnHostSwitchToggleValueChange()
	{
		mapWidth.gameObject.SetActive(hostSwitchToggle.isOn);
		networkAddress.gameObject.SetActive(!hostSwitchToggle.isOn);
	}

	public void OnMapWidthFieldChange()
	{
		if (mapWidth.text == "") { return; }
		int tempMapWidth = int.Parse(mapWidth.text);
		if (tempMapWidth < 5)
		{
			mapWidth.text = "5";
		}
		else if (tempMapWidth > 20)
		{
			mapWidth.text = "20";
		}
	}


	private void OnGUI()
	{
		if(!NetworkClient.isConnected && !NetworkServer.active)
		{
			ShowPanels();
		}

		ShowDisconnectButton((NetworkServer.active && NetworkClient.isConnected) ||
							(NetworkClient.isConnected) ||
							(NetworkServer.active));
		
	}

	private void ShowPanels()
	{
		if(debugPanel == null) { return; }
		if (!NetworkClient.active)
		{
			ShowDebugPanel(false);
			ShowConnectionPanel(true);
		}
		else
		{
			ShowConnectionPanel(false);
			ShowDebugPanel(true);
		}
	}

	void ShowDisconnectButton(bool show)
	{
		// stop host if host mode
		if (disconnectButton)
		{
			disconnectButton.SetActive(show);
		}
	}

	private void ShowConnectionPanel(bool show)
	{
		if (fieldsPanel)
		{
			fieldsPanel.SetActive(show);
		}
	}

	private void ShowDebugPanel(bool show)
	{
		if (debugPanel)
		{
			debugText.text = "Trying to connect to " + NetworkManagerHUDWOT.manager.networkAddress + "...";
			debugPanel.gameObject.SetActive(show);
		}
	}

	public void OnDisconnectButton()
	{
		if (NetworkServer.active && NetworkClient.isConnected)
		{
			NetworkManagerHUDWOT.manager.StopHost();
		}
		// stop client if client-only
		else if (NetworkClient.isConnected)
		{
			NetworkManagerHUDWOT.manager.StopClient();
		}
		// stop server if server-only
		else if (NetworkServer.active)
		{
			NetworkManagerHUDWOT.manager.StopServer();
		}
	}
}
