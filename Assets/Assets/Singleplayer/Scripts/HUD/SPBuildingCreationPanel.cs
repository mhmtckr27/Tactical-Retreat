using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SPBuildingCreationPanel : MonoBehaviour
{
	[SerializeField] private Image buildingIcon;
	[SerializeField] private Text buildingName;

	[SerializeField] private Text descriptionText;

	[SerializeField] private GameObject woodCostPanel;
	[SerializeField] private Text woodCostText;
	[SerializeField] private GameObject meatCostPanel;
	[SerializeField] private Text meatCostText;
	[SerializeField] private GameObject populationCostPanel;
	[SerializeField] private Text populationCostText;
	[SerializeField] private GameObject actionPointCostPanel;
	[SerializeField] private Text actionPointCostText;

	[SerializeField] private Button backButton;
	[SerializeField] private Button buildButton;

	[SerializeField] private Color enoughResourceColor;
	[SerializeField] private Color notEnoughResourceColor;

	public SPTownCenter OwnerPlayer { get; set; }
	public BuildingProperties BuildingProperties { get; set; }
	private bool canBuild;

	public void Init(SPTownCenter ownerPlayer, BuildingProperties buildingProperties)
	{
		this.OwnerPlayer = ownerPlayer;
		this.BuildingProperties = buildingProperties;
		UpdateUI();
	}

	private void UpdateUI()
	{
		if (BuildingProperties != null)
		{
			buildingName.text = BuildingProperties.buildingName;
			buildingIcon.sprite = BuildingProperties.buildingIcon;

			descriptionText.text = BuildingProperties.buildingDescription.ToString();

			woodCostText.text = BuildingProperties.woodCostToCreate.ToString();
			meatCostText.text = BuildingProperties.meatCostToCreate.ToString();
			populationCostText.text = BuildingProperties.populationCostToCreate.ToString();
			actionPointCostText.text = BuildingProperties.actionPointCostToCreate.ToString();

			woodCostText.color = OwnerPlayer.woodCount < BuildingProperties.woodCostToCreate ?
									notEnoughResourceColor : enoughResourceColor;
			meatCostText.color = OwnerPlayer.meatCount < BuildingProperties.meatCostToCreate ?
									notEnoughResourceColor : enoughResourceColor;
			populationCostText.color = OwnerPlayer.maxPopulation - OwnerPlayer.currentPopulation < BuildingProperties.populationCostToCreate ?
									notEnoughResourceColor : enoughResourceColor;
			actionPointCostText.color = OwnerPlayer.actionPoint < BuildingProperties.actionPointCostToCreate ?
									notEnoughResourceColor : enoughResourceColor;


			canBuild = (OwnerPlayer.woodCount < BuildingProperties.woodCostToCreate ||
						OwnerPlayer.meatCount < BuildingProperties.meatCostToCreate ||
						(OwnerPlayer.maxPopulation - OwnerPlayer.currentPopulation) < BuildingProperties.populationCostToCreate ||
						OwnerPlayer.actionPoint < BuildingProperties.actionPointCostToCreate)
						== false;


			/*Debug.LogError(OwnerPlayer.woodCount < BuildingProperties.woodCostToCreate);
			Debug.LogError(OwnerPlayer.meatCount < BuildingProperties.meatCostToCreate);
			Debug.LogError((OwnerPlayer.maxPopulation - OwnerPlayer.currentPopulation) < BuildingProperties.populationCostToCreate);
			Debug.LogError(OwnerPlayer.actionPoint < BuildingProperties.actionPointCostToCreate);*/

			woodCostPanel.SetActive(BuildingProperties.woodCostToCreate > 0);
			meatCostPanel.SetActive(BuildingProperties.meatCostToCreate > 0);
			populationCostPanel.SetActive(BuildingProperties.populationCostToCreate > 0);
			actionPointCostPanel.SetActive(BuildingProperties.actionPointCostToCreate > 0);

			buildButton.interactable = canBuild;
		}
	}

	public void OnBuildButton()
	{
		OwnerPlayer.CreateBuilding(OwnerPlayer, BuildingProperties.buildingPrefab.name);
		gameObject.SetActive(false);
	}

	public void OnBackButton()
	{
		gameObject.SetActive(false);
		OwnerPlayer.SelectBuilding(OwnerPlayer);
	}
}
