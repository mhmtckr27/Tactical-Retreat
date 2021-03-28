using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainMenuUI : MonoBehaviour
{
	[SerializeField] private StringGameObjectDictionary constructionButtons;
	[SerializeField] private GameObject parentForButtons;

	private TerrainHexagon currentTerrain;
	public TerrainHexagon CurrentTerrain
	{
		get => currentTerrain;
		set
		{
			currentTerrain = value;
			if(value != null)
			{
				UpdateUI();
			}
		}
	}

	public TownCenter townCenter;

	private void UpdateUI()
	{
		foreach(KeyValuePair<string, GameObject> keyValuePair in constructionButtons)
		{
			keyValuePair.Value.SetActive(false);
		}
		foreach(BuildingType buildingType in currentTerrain.buildablesOnThisTerrain)
		{
			if (constructionButtons.ContainsKey(buildingType.ToString()))
			{
				constructionButtons[buildingType.ToString()].SetActive(true);
			}
		}
	}

	public void OnBuildingButton(GameObject building)
	{
		townCenter.CreateBuildingCmd(building.name, currentTerrain);
		gameObject.SetActive(false);
	}
}
