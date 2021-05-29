using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitCreateButtonUI : MonoBehaviour
{
	[SerializeField] private GameObject woodCost;
	[SerializeField] private Text woodCostText;
	[SerializeField] private GameObject meatCost;
	[SerializeField] private Text meatCostText;
	[SerializeField] private GameObject populationCost;
	[SerializeField] private Text populationCostText;
	[SerializeField] private GameObject actionPointCost;
	[SerializeField] private Text actionPointCostText;

	[SerializeField] private UnitProperties unitProperties;


	private void Start()
	{
		if(unitProperties != null)
		{
			woodCostText.text = unitProperties.woodCostToCreate.ToString();
			meatCostText.text = unitProperties.meatCostToCreate.ToString();
			populationCostText.text = unitProperties.populationCostToCreate.ToString();
			actionPointCostText.text = unitProperties.actionPointCostToCreate.ToString();

			woodCost.SetActive(unitProperties.woodCostToCreate > 0);
			meatCost.SetActive(unitProperties.meatCostToCreate > 0);
			populationCost.SetActive(unitProperties.populationCostToCreate > 0);
			actionPointCost.SetActive(unitProperties.actionPointCostToCreate > 0);
		}
	}
}
