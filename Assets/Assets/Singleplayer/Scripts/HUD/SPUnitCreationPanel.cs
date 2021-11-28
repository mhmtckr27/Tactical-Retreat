using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SPUnitCreationPanel : MonoBehaviour
{
	[SerializeField] private Image unitIcon;
	[SerializeField] private Text unitName;
	[SerializeField] private GameObject unitPrefab;

	[SerializeField] private Text healthText;
	[SerializeField] private Text armorText;
	[SerializeField] private Text damageText;
	[SerializeField] private Text moveRangeText;
	[SerializeField] private Text attackRangeText;
	[SerializeField] private Text explorationRangeText;

	[SerializeField] private GameObject woodCostPanel;
	[SerializeField] private Text woodCostText;
	[SerializeField] private GameObject meatCostPanel;
	[SerializeField] private Text meatCostText;
	[SerializeField] private GameObject populationCostPanel;
	[SerializeField] private Text populationCostText;
	[SerializeField] private GameObject actionPointCostPanel;
	[SerializeField] private Text actionPointCostText;

	[SerializeField] private Button backButton;
	[SerializeField] private Button createButton;

	[SerializeField] private Color enoughResourceColor;
	[SerializeField] private Color notEnoughResourceColor;

	public SPTownCenter OwnerPlayer { get; set; }
	public UnitProperties UnitProperties { get; set; }
	private bool canCreate;

	public void Init(SPTownCenter ownerPlayer, UnitProperties unitProperties)
	{
		this.OwnerPlayer = ownerPlayer;
		this.unitPrefab = unitProperties.unitPrefab;
		this.UnitProperties = unitProperties;
		UpdateUI();
	}

	private void UpdateUI()
	{
		if (UnitProperties != null)
		{
			unitName.text = UnitProperties.unitName;
			unitIcon.sprite = UnitProperties.unitIcon;

			healthText.text = UnitProperties.health.ToString();
			armorText.text = UnitProperties.armor.ToString();
			damageText.text = UnitProperties.damage.ToString();
			moveRangeText.text = UnitProperties.moveRange.ToString();
			attackRangeText.text = UnitProperties.attackRange.ToString();
			explorationRangeText.text = UnitProperties.exploreRange.ToString();

			woodCostText.text = UnitProperties.woodCostToCreate.ToString();
			meatCostText.text = UnitProperties.meatCostToCreate.ToString();
			populationCostText.text = UnitProperties.populationCostToCreate.ToString();
			actionPointCostText.text = UnitProperties.actionPointCostToCreate.ToString();

			woodCostText.color = OwnerPlayer.woodCount < UnitProperties.woodCostToCreate ?
									notEnoughResourceColor : enoughResourceColor;
			meatCostText.color = OwnerPlayer.meatCount < UnitProperties.meatCostToCreate ?
									notEnoughResourceColor : enoughResourceColor;
			populationCostText.color = OwnerPlayer.maxPopulation - OwnerPlayer.currentPopulation < UnitProperties.populationCostToCreate ?
									notEnoughResourceColor : enoughResourceColor;
			actionPointCostText.color = OwnerPlayer.actionPoint < UnitProperties.actionPointCostToCreate ?
									notEnoughResourceColor : enoughResourceColor;


			canCreate = (OwnerPlayer.woodCount < UnitProperties.woodCostToCreate ||
						OwnerPlayer.meatCount < UnitProperties.meatCostToCreate ||
						(OwnerPlayer.maxPopulation - OwnerPlayer.currentPopulation) < UnitProperties.populationCostToCreate ||
						OwnerPlayer.actionPoint < UnitProperties.actionPointCostToCreate)
						== false;


			/*Debug.LogError(OwnerPlayer.woodCount < UnitProperties.woodCostToCreate);
			Debug.LogError(OwnerPlayer.meatCount < UnitProperties.meatCostToCreate);
			Debug.LogError((OwnerPlayer.maxPopulation - OwnerPlayer.currentPopulation) < UnitProperties.populationCostToCreate);
			Debug.LogError(OwnerPlayer.actionPoint < UnitProperties.actionPointCostToCreate);*/

			woodCostPanel.SetActive(UnitProperties.woodCostToCreate > 0);
			meatCostPanel.SetActive(UnitProperties.meatCostToCreate > 0);
			populationCostPanel.SetActive(UnitProperties.populationCostToCreate > 0);
			actionPointCostPanel.SetActive(UnitProperties.actionPointCostToCreate > 0);

			createButton.interactable = canCreate;
		}
	}

	public void OnCreateButton()
	{
		OwnerPlayer.CreateUnit(OwnerPlayer, unitPrefab.name);
		gameObject.SetActive(false);
	}

	public void OnBackButton()
	{
		gameObject.SetActive(false);
		OwnerPlayer.SelectBuilding(OwnerPlayer);
	}

	/*private void Update()
	{
		//TODO: coroutine kullan.
		if (Input.GetMouseButton(0) && gameObject.activeSelf &&
			!RectTransformUtility.RectangleContainsScreenPoint(gameObject.GetComponent<RectTransform>(), Input.mousePosition,null))
		{
			gameObject.SetActive(false);
		}
	}*/
}
