using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(EventTrigger))]
public class SPUIManager : MonoBehaviour
{
	[SerializeField] private Button nextTurnButton;
	[SerializeField] private Text woodCountText;
	[SerializeField] private Text meatCountText;
	[SerializeField] private Text actionPointText;
	[SerializeField] private Text currentToMaxPopulationText;
	[SerializeField] private GameObject settingsMenu;

	[SerializeField] private string singleplayerScene;
	[SerializeField] private InputField mapWidth;
	[SerializeField] private InputField aiPlayerCount;

	
	public SPTownCenterUI townCenterUI;
	public SPTerrainHexagonUI terrainHexagonUI;
	public SPUnitCreationPanel unitCreationUI;
	public SPBuildingCreationPanel buildingCreationUI;


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
		OnLoadScene("Start");
	}

	public void OnQuitButton()
	{
		Application.Quit();
	}

	public void OnMapWidthFieldChange()
	{
		if(mapWidth.text == "") { return; }
		int tempMapWidth = int.Parse(mapWidth.text);
		if(tempMapWidth < 5)
		{
			mapWidth.text = "5";
		}
		else if(tempMapWidth > 20)
		{
			mapWidth.text = "20";
		}
	}

	public void OnAICountFieldChange()
	{
		if (aiPlayerCount.text == "") { return; }
		int tempAICount = int.Parse(aiPlayerCount.text);
		if (tempAICount < 1)
		{
			aiPlayerCount.text = "1";
		}
		else if (tempAICount > 6)
		{
			aiPlayerCount.text = "6";
		}
	}

	public void OnSPStartButton()
	{
		if (aiPlayerCount.text == "")
		{
			SPGameManager.Instance.aiPlayerCount = 1;
		}
		else
		{
			SPGameManager.Instance.aiPlayerCount = int.Parse(aiPlayerCount.text);
		}

		if (mapWidth.text == "")
		{
			SPGameManager.Instance.mapWidth = 6;
		}
		else
		{
			SPGameManager.Instance.mapWidth = int.Parse(mapWidth.text);
		}

		OnLoadScene("Singleplayer");
	}

	public void OnLoadScene(string sceneToLoad)
	{
		SceneManager.LoadScene(sceneToLoad);
	}
}