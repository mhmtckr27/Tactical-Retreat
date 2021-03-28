using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConstructButtonUI : MonoBehaviour
{
	[SerializeField] public BuildingBase buildingToConstruct;
	[SerializeField] public Text woodCostText;

	private void OnEnable()
	{
		woodCostText.text = buildingToConstruct.buildCostWood.ToString();
	}
}
