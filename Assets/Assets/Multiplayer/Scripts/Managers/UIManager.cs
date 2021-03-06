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
	[SerializeField] public GameObject loadingScreen;

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
		SceneManager.sceneLoaded += SceneManager_sceneLoaded;
		/*if (NetworkRoomManagerWOT.singleton as NetworkRoomManagerWOT)
		{
			(NetworkRoomManagerWOT.singleton as NetworkRoomManagerWOT).OnMapGenerationFinish += OnLoadingMapFinish;
		}*/
	}

	private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
	{
		if((arg0.name == "Start"))
		{
			if (NetworkManagerHUDWOT.manager)
			{
				Destroy(NetworkManagerHUDWOT.manager.gameObject);
			}
		}
		else if((arg0.name == "WinScreen") || (arg0.name == "LoseScreen"))
		{
			NetworkServer.Shutdown();
			NetworkClient.Shutdown();
		}
	}

	private void OnDisable()
	{
		/*if (NetworkRoomManagerWOT.singleton as NetworkRoomManagerWOT)
		{
			(NetworkRoomManagerWOT.singleton as NetworkRoomManagerWOT).OnMapGenerationFinish -= OnLoadingMapFinish;
		}*/
		SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
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

	public void OnExitButton(string sceneToLoad)
	{
		// stop host if host mode
		if (NetworkServer.active && NetworkClient.isConnected)
		{
			//Debug.LogError("host");

			NetworkManagerHUDWOT.manager.StopHost();
			OnLoadScene(sceneToLoad);
			//OnlineGameManager.Instance.OnHostDisconnectedRpc();
		}
		// stop client if client-only
		else if (NetworkClient.isConnected)
		{
			//Debug.LogError("client");
			townCenterUI.townCenter.UnregisterPlayerCmd();
			NetworkManagerHUDWOT.manager.StopClient();
			OnLoadScene(sceneToLoad);
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
		if(sceneToLoad == "Room")
		{
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
