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

	public void OnQuitButton()
	{
		Application.Quit();
	}

	public void OnLoadScene(string sceneToLoad)
	{
		SceneManager.LoadScene(sceneToLoad);
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
