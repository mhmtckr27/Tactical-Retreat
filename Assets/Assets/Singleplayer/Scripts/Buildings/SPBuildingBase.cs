using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SPBuildingBase : MonoBehaviour
{
	//[SerializeField] public BuildingType buildingType;
	[SerializeField] public BuildingProperties buildingProperties;

	public SPTerrainHexagon OccupiedHex { get; set; }
	public uint PlayerID { get; set; }

	protected SPBuildingUI buildingMenuUI;
	public SPUIManager uiManager;
	protected bool menu_visible = false;

	private Color playerColor;
	public Color PlayerColor
	{
		get => playerColor;
		set
		{
			OnPlayerColorSet(playerColor, value);
			playerColor = value;
		}
	}

	public void OnPlayerColorSet(Color oldColor, Color newColor)
	{
		if (buildingProperties != null && buildingProperties.buildingType != BuildingType.House)
		{
			GetComponent<Renderer>().materials[1].color = newColor;
		}
	}
	protected virtual void Start()
	{
	}

	public virtual void ToggleBuildingMenu(bool enable)
	{
		buildingMenuUI.gameObject.SetActive(enable);
	}
}